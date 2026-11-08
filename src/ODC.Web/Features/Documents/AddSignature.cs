using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Attaches a qualified electronic signature created in the signer's browser (the private key never leaves it). In a
/// cumulative format the file must be the newest signed version plus exactly one signature.
/// </summary>
public static class AddSignature
{
    /// <summary>
    /// Containers up to this size are parsed on the server to extract the signer certificate.
    /// </summary>
    private const long InspectableSignatureSize = 10L * 1024 * 1024;

    /// <summary>
    /// Allowed clock skew between the signer's browser and the server.
    /// </summary>
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Checks the file and the chain, stores the signature and tells the next signers or the author.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    /// <param name="signatureInspector">The signature container inspector.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        DocumentDesk desk,
        IFileStorage fileStorage,
        ISignatureInspector signatureInspector,
        IAuditLogger audit,
        TimeProvider timeProvider) : IRequestHandler<AddSignatureCommand, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(AddSignatureCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            if (!currentUser.Has(Permissions.Documents.Sign))
            {
                return DocumentDesk.CannotSign();
            }

            if (command.Signature.Length is <= 0 or > FileRules.MaxSignatureSize)
            {
                return Error.Validation(Texts.T("Некоректний розмір файлу підпису."));
            }

            var document = await db.Documents
                .Include(static d => d.Signers)
                .Include(static d => d.Signatures)
                .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
            if (document is null || !document.IsAwaitingSignatureFrom(me))
            {
                return document is null ? DocumentDesk.NotFound() : Error.Conflict(Texts.T("Документ зараз не очікує Вашого підпису."));
            }

            var now = timeProvider.GetUtcNow();
            var certificate = command.ReportedCertificate;
            Stream content = command.Signature.Content;
            MemoryStream? buffer = null;
            var appended = command.BaseSignatureId is not null && SignatureChain.IsCumulative(command.Format);
            try
            {
                if (appended || (command.Format == SignatureFormat.CAdES && command.Signature.Length <= InspectableSignatureSize))
                {
                    buffer = new MemoryStream((int)command.Signature.Length);
                    await command.Signature.Content.CopyToAsync(buffer, cancellationToken);
                    buffer.Position = 0;
                    content = buffer;
                }

                if (appended)
                {
                    // The new file must be the previous version plus exactly one signature (the chain rules themselves
                    // are enforced by Document.AddSignature, again after any reload).
                    var previous = document.Signatures.FirstOrDefault(s => s.Id == command.BaseSignatureId);
                    if (previous is null)
                    {
                        return Error.Conflict(Texts.T("Поки Ви підписували, документ підписав інший підписант. Підпишіть останню версію ще раз."));
                    }

                    var check = await desk.CheckAppendedAsync(previous, buffer!.GetBuffer().AsMemory(0, (int)buffer.Length), command.Format, cancellationToken);
                    if (!check.IsSuccess)
                    {
                        return check.Error;
                    }

                    certificate = check.Value ?? certificate;
                }
                else if (buffer is not null)
                {
                    certificate = signatureInspector.TryReadSigner(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), command.Format) ?? certificate;
                }

                if (string.IsNullOrWhiteSpace(certificate.SubjectFullName) || string.IsNullOrWhiteSpace(certificate.SerialNumber))
                {
                    return Error.Validation(Texts.T("Не вдалося визначити сертифікат підписувача."));
                }

                var signedAt = command.SignedAt is { } reported && reported <= now + AllowedClockSkew && reported >= (document.SubmittedAt ?? now) - AllowedClockSkew
                    ? reported
                    : now;

                var blob = await fileStorage.SaveAsync(content, "signatures", FileRules.MaxSignatureSize, cancellationToken);
                var contentType = DocumentDesk.SignatureContentType(command.Format);

                var result = await DomainErrors.GuardAsync<Guid>(async () =>
                {
                    for (var attempt = 1; ; attempt++)
                    {
                        try
                        {
                            var file = new StoredFile(SignedFileName.For(document.File.FileName, command.Format), contentType, blob.Size, blob.Sha256, blob.StorageKey);
                            var signature = document.AddSignature(me, command.Format, command.Packaging, command.Level, command.KeyMedia, file, certificate, signedAt, now, command.BaseSignatureId);
                            audit.Add(AuditActions.DocumentSigned, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber}: {command.Format}, {certificate.IssuerName}, № {certificate.SerialNumber}");
                            await db.SaveChangesAsync(cancellationToken);
                            return signature.Id;
                        }
                        catch (DbUpdateConcurrencyException) when (attempt < DocumentDesk.MaxSaveAttempts)
                        {
                            // Another signer of a parallel route (or the author) saved the document first. An independent
                            // (XAdES) signature is still valid and is attached again; a cumulative one was made on a
                            // version that is no longer the newest, and AddSignature asks to sign the newest one instead.
                            db.DiscardChanges();
                            document = await db.Documents
                                .Include(static d => d.Signers)
                                .Include(static d => d.Signatures)
                                .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
                            if (document is null)
                            {
                                return DocumentDesk.NotFound();
                            }
                        }
                    }
                });

                if (!result.IsSuccess)
                {
                    await fileStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
                }

                return result;
            }
            finally
            {
                if (buffer is not null)
                {
                    await buffer.DisposeAsync();
                }
            }
        }
    }

    /// <summary>
    /// <c>POST /api/v1/documents/{id}/signatures</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => DocumentDesk.MapDocuments(api)
                .MapPost("{id:guid}/signatures", HandleAsync)
                .RequirePermission(Permissions.Documents.Sign)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxSignatureSize + (1024 * 1024)));

        /// <summary>
        /// Attaches a qualified electronic signature created in the signer's browser.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="form">The signature parameters and the certificate data reported by the browser.</param>
        /// <param name="signature">The signature file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            Guid id,
            [FromForm] SignatureUploadForm form,
            IFormFile signature,
            IRequestHandler<AddSignatureCommand, Result<Guid>> handler,
            CancellationToken cancellationToken)
        {
            await using var content = signature.OpenReadStream();
            var result = await handler.HandleAsync(
                new AddSignatureCommand(
                    id,
                    form.Format,
                    form.Packaging,
                    form.Level,
                    form.KeyMedia,
                    form.SignedAt,
                    new SignerCertificate(form.SubjectFullName, form.SubjectOrganization, form.SubjectTaxId, form.IssuerName, form.SerialNumber, form.ValidFrom, form.ValidTo),
                    new FileUpload(content, signature.FileName, signature.ContentType, signature.Length),
                    form.BaseSignatureId),
                cancellationToken);
            return result.ToCreated(signatureId => $"{ApiEndpoints.Base}/documents/{id}/signatures/{signatureId}/file");
        }
    }
}
