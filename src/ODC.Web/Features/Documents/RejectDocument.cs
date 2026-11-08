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

namespace ODC.Web.Features.Documents;

/// <summary>
/// Declines to sign a document; the author hears about it with the reason.
/// </summary>
public static class RejectDocument
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The document.</param>
    /// <param name="Reason">Why the signer declines.</param>
    public sealed record Command(Guid Id, string Reason) : ICommand<Result>;

    /// <summary>
    /// Records the decision (the domain checks the turn) and tells the author.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents.Include(static d => d.Signers).FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
            if (document is null)
            {
                return DocumentDesk.NotFound();
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                document.Reject(me, request.Reason, timeProvider.GetUtcNow());
                audit.Add(AuditActions.DocumentRejected, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber}: {request.Reason}");
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/documents/{id}/rejections</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DocumentDesk.MapDocuments(api).MapPost("{id:guid}/rejections", HandleAsync).RequirePermission(Permissions.Documents.Sign);

        /// <summary>
        /// Declines to sign the document.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="rejection">The reason.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 on success.</returns>
        private static async Task<Results<Created, ProblemHttpResult>> HandleAsync(
            Guid id,
            RejectionRequest rejection,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(id, rejection.Reason), cancellationToken);
            return result.IsSuccess ? TypedResults.Created($"{ApiEndpoints.Base}/documents/{id}") : result.Error.ToProblem();
        }
    }
}
