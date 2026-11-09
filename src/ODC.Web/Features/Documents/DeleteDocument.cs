using Microsoft.AspNetCore.Http.HttpResults;
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

namespace ODC.Web.Features.Documents;

/// <summary>
/// Deletes a document without signatures (its author or a document manager).
/// </summary>
public static class DeleteDocument
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Deletes the document and, after the save, its file.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, DocumentDesk desk, IFileStorage fileStorage, IAuditLogger audit)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents
                .Include(static d => d.Signers)
                .Include(static d => d.Signatures)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null || !await desk.IsVisibleAsync(id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            if (document.AuthorId != me && !currentUser.Has(Permissions.Documents.Manage))
            {
                return Error.Forbidden();
            }

            var result = await DomainErrors.GuardAsync(async () =>
            {
                document.EnsureCanBeDeleted();
                db.Documents.Remove(document);
                audit.Add(AuditActions.DocumentDeleted, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber} «{document.Title}»");
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });

            if (result.IsSuccess)
            {
                await fileStorage.DeleteAsync(document.File.StorageKey, CancellationToken.None);
            }

            return result;
        }
    }

    /// <summary>
    /// <c>DELETE /api/v1/documents/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DocumentDesk.MapDocuments(api).MapDelete("{id:guid}", HandleAsync);

        /// <summary>
        /// Deletes a document without signatures.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
