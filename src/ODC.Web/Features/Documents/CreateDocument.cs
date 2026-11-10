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
/// Registers a document with its file and signing route; it can go for signing at once.
/// </summary>
public static class CreateDocument
{
    /// <summary>
    /// Validates the file, the unit and the signers, stores the file and saves the document. A stored file is deleted
    /// again when the document could not be saved.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    /// <param name="numbers">The registration number generator.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        OrganizationDbContext db,
        ICurrentUser currentUser,
        DocumentDesk desk,
        IFileStorage fileStorage,
        IDocumentNumberGenerator numbers,
        IAuditLogger audit,
        TimeProvider timeProvider) : IRequestHandler<CreateDocumentCommand, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(CreateDocumentCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var contentType = FileRules.ResolveDocumentType(command.File.FileName);
            if (contentType is null)
            {
                return Error.Validation(Texts.T("Цей тип файлу не підтримується."));
            }

            if (command.File.Length > FileRules.MaxDocumentSize)
            {
                return Error.Validation(Texts.T("Розмір файлу не може перевищувати {0} МБ.", FileRules.MaxDocumentSize / 1024 / 1024));
            }

            var allowedUnits = await desk.GetAuthorUnitsAsync(cancellationToken);
            if (!allowedUnits.Any(u => u.Id == command.OrganizationUnitId))
            {
                return Error.Validation(Texts.T("Оберіть підрозділ, до якого Ви належите."));
            }

            var signerIds = command.SignerIds.Distinct().ToList();
            if (await desk.CheckSignersAsync(signerIds, cancellationToken) is { } invalidSigners)
            {
                return invalidSigners;
            }

            var blob = await fileStorage.SaveAsync(command.File.Content, "documents", FileRules.MaxDocumentSize, cancellationToken);
            var saved = false;
            try
            {
                var result = await DomainErrors.GuardAsync<Guid>(async () =>
                {
                    var number = await numbers.NextAsync(cancellationToken);
                    var file = new StoredFile(command.File.FileName, contentType, blob.Size, blob.Sha256, blob.StorageKey);
                    var document = new Document(number, command.Title, command.Description, command.Kind, me, command.OrganizationUnitId, command.SigningMode, file);
                    document.SetRoute(signerIds, command.SigningMode);
                    if (command.Submit)
                    {
                        document.Submit(timeProvider.GetUtcNow());
                    }

                    db.Documents.Add(document);
                    audit.Add(AuditActions.DocumentCreated, nameof(Document), document.Id.ToString(), $"{number} «{document.Title}»");
                    await db.SaveChangesAsync(cancellationToken);
                    return document.Id;
                });

                saved = result.IsSuccess;

                return result;
            }
            finally
            {
                if (!saved)
                {
                    await fileStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
                }
            }
        }
    }

    /// <summary>
    /// <c>POST /api/v1/documents</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => DocumentDesk.MapDocuments(api)
                .MapPost(string.Empty, HandleAsync)
                .RequirePermission(Permissions.Documents.Create)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxDocumentSize + (1024 * 1024)));

        /// <summary>
        /// Creates a document (multipart form with the file).
        /// </summary>
        /// <param name="form">The document fields.</param>
        /// <param name="file">The document file.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            [FromForm] CreateDocumentForm form,
            IFormFile file,
            IRequestHandler<CreateDocumentCommand, Result<Guid>> handler,
            CancellationToken cancellationToken)
        {
            await using var content = file.OpenReadStream();
            var result = await handler.HandleAsync(
                new CreateDocumentCommand(
                    form.Title,
                    form.Description,
                    form.Kind,
                    form.OrganizationUnitId,
                    form.SigningMode,
                    form.SignerIds,
                    form.Submit,
                    new FileUpload(content, file.FileName, file.ContentType, file.Length)),
                cancellationToken);
            return result.ToCreated(static id => $"{ApiEndpoints.Base}/documents/{id}");
        }
    }
}
