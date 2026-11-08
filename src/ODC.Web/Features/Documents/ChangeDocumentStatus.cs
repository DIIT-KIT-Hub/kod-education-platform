using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Changes the life-cycle state of a document: submit for signing or withdraw to drafts (its author), archive a signed
/// or rejected one (its author or a document manager).
/// </summary>
public static class ChangeDocumentStatus
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The document.</param>
    /// <param name="Status">The target state: in signing, draft or archived.</param>
    public sealed record Command(Guid Id, DocumentStatus Status) : ICommand<Result>;

    /// <summary>
    /// Applies the transition.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, DocumentDesk desk, IAuditLogger audit, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return request.Status switch
            {
                DocumentStatus.InSigning => await SubmitAsync(request.Id, cancellationToken),
                DocumentStatus.Draft => await desk.MutateAsAuthorAsync(request.Id, static document => document.Withdraw(), AuditActions.DocumentWithdrawn, cancellationToken),
                DocumentStatus.Archived => await ArchiveAsync(request.Id, cancellationToken),
                _ => Result.Failure(Error.Validation(Texts.T("Недопустимий перехід стану."))),
            };
        }

        /// <summary>
        /// Sends a document for signing and tells the signers whose turn came.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        private Task<Result> SubmitAsync(Guid id, CancellationToken cancellationToken)
            => desk.MutateAsAuthorAsync(id, document => document.Submit(timeProvider.GetUtcNow()), AuditActions.DocumentSubmitted, cancellationToken);

        /// <summary>
        /// Archives a signed or rejected document.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result.</returns>
        private async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken)
        {
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null || !await desk.IsVisibleAsync(id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            if (document.AuthorId != me && !currentUser.Has(Permissions.Documents.Manage))
            {
                return Error.Forbidden();
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                document.Archive();
                audit.Add(AuditActions.DocumentArchived, nameof(Document), document.Id.ToString(), document.RegistrationNumber);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/documents/{id}/status</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DocumentDesk.MapDocuments(api).MapPut("{id:guid}/status", HandleAsync);

        /// <summary>
        /// Changes the life-cycle state: submit for signing, withdraw to drafts or archive.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="status">The target state.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            DocumentStatusRequest status,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, status.Status), cancellationToken)).ToNoContent();
    }
}
