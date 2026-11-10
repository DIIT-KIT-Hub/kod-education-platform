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
/// Changes a draft or rejected document (author only). A new file removes every signature; removing a signer from the
/// route removes that signer's signature (and, in a cumulative format, the whole chain).
/// </summary>
public static class UpdateDocument
{
    /// <summary>
    /// Validates the change, saves it and deletes the files nobody references any more — only after the database agreed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        OrganizationDbContext db,
        ICurrentUser currentUser,
        DocumentDesk desk,
        IFileStorage fileStorage,
        IAuditLogger audit,
        TimeProvider timeProvider) : IRequestHandler<UpdateDocumentCommand, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(UpdateDocumentCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents
                .Include(static d => d.Signers)
                .Include(static d => d.Signatures)
                .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
            if (document is null || !await desk.IsVisibleAsync(command.Id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            if (document.AuthorId != me)
            {
                return Error.Forbidden(Texts.T("Змінювати документ може лише його автор."));
            }

            // The current unit stays allowed even if the author's positions changed since the document was created.
            if (command.OrganizationUnitId != document.OrganizationUnitId)
            {
                var allowedUnits = await desk.GetAuthorUnitsAsync(cancellationToken);
                if (!allowedUnits.Any(u => u.Id == command.OrganizationUnitId))
                {
                    return Error.Validation(Texts.T("Оберіть підрозділ, до якого Ви належите."));
                }
            }

            var signerIds = command.SignerIds.Distinct().ToList();
            if (await desk.CheckSignersAsync(signerIds, cancellationToken) is { } invalidSigners)
            {
                return invalidSigners;
            }

            if (command.Submit && signerIds.Count == 0)
            {
                return Error.Validation(Texts.T("Додайте щонайменше одного підписанта."));
            }

            StoredFile? newFile = null;
            if (command.File is { } upload)
            {
                var contentType = FileRules.ResolveDocumentType(upload.FileName);
                if (contentType is null)
                {
                    return Error.Validation(Texts.T("Цей тип файлу не підтримується."));
                }

                if (upload.Length > FileRules.MaxDocumentSize)
                {
                    return Error.Validation(Texts.T("Розмір файлу не може перевищувати {0} МБ.", FileRules.MaxDocumentSize / 1024 / 1024));
                }

                var blob = await fileStorage.SaveAsync(upload.Content, "documents", FileRules.MaxDocumentSize, cancellationToken);
                newFile = new StoredFile(upload.FileName, contentType, blob.Size, blob.Sha256, blob.StorageKey);
            }

            var previousFile = document.File;
            var removed = new List<DocumentSignature>();
            var result = await DomainErrors.GuardAsync(async () =>
            {
                document.UpdateDetails(command.Title, command.Description, command.Kind, command.OrganizationUnitId);
                removed.AddRange(document.SetRoute(signerIds, command.SigningMode));
                if (newFile is not null)
                {
                    removed.AddRange(document.ReplaceFile(newFile));
                }

                var details = newFile is null
                    ? $"{document.RegistrationNumber} «{document.Title}»"
                    : $"{document.RegistrationNumber} «{document.Title}»: новий файл {newFile.FileName}";
                if (removed.Count > 0)
                {
                    details += $", анульовано підписів: {removed.Count}";
                }

                audit.Add(AuditActions.DocumentUpdated, nameof(Document), document.Id.ToString(), details);
                if (command.Submit)
                {
                    document.Submit(timeProvider.GetUtcNow());
                    audit.Add(AuditActions.DocumentSubmitted, nameof(Document), document.Id.ToString(), document.RegistrationNumber);
                }

                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });

            // Only files that are no longer referenced are deleted, and only after the database agreed.
            if (result.IsSuccess)
            {
                if (newFile is not null)
                {
                    await fileStorage.DeleteAsync(previousFile.StorageKey, CancellationToken.None);
                }

                foreach (var signature in removed)
                {
                    await fileStorage.DeleteAsync(signature.File.StorageKey, CancellationToken.None);
                }
            }
            else if (newFile is not null)
            {
                await fileStorage.DeleteAsync(newFile.StorageKey, CancellationToken.None);
            }

            return result;
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/documents/{id}</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => DocumentDesk.MapDocuments(api)
                .MapPut("{id:guid}", HandleAsync)
                .RequirePermission(Permissions.Documents.Create)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxDocumentSize + (1024 * 1024)));

        /// <summary>
        /// Changes a draft or rejected document (author only). Sending a new file removes every signature; removing a
        /// signer from the route removes that signer's signature.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="form">The document fields.</param>
        /// <param name="file">A new file (optional).</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            [FromForm] UpdateDocumentForm form,
            IFormFile? file,
            IRequestHandler<UpdateDocumentCommand, Result> handler,
            CancellationToken cancellationToken)
        {
            await using var content = file?.OpenReadStream();
            var upload = file is null ? null : new FileUpload(content!, file.FileName, file.ContentType, file.Length);
            return (await handler.HandleAsync(
                new UpdateDocumentCommand(id, form.Title, form.Description, form.Kind, form.OrganizationUnitId, form.SigningMode, form.SignerIds, form.Submit, upload),
                cancellationToken)).ToNoContent();
        }
    }
}
