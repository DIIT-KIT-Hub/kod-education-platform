using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Documents;

/// <summary>
/// The signing turn (<see cref="SignatureChain.TurnDuration"/>): the browser takes it when the signing window opens,
/// renews it right before signing and gives it up on close, so two signers never add to the same version.
/// </summary>
public static class SigningTurn
{
    /// <summary>
    /// The error code of "another signer holds the signing turn" (the browser waits and asks again).
    /// </summary>
    private const string SigningTurnErrorCode = "signing_turn";

    /// <summary>
    /// Takes the turn.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record BeginCommand(Guid Id) : ICommand<Result<SigningSession>>;

    /// <summary>
    /// Gives up the turn.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record EndCommand(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Takes the turn and returns the version to add the signature to.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class BeginHandler(ApplicationDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<BeginCommand, Result<SigningSession>>
    {
        /// <inheritdoc />
        public async Task<Result<SigningSession>> HandleAsync(BeginCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            if (!currentUser.Has(Permissions.Documents.Sign))
            {
                return DocumentDesk.CannotSign();
            }

            for (var attempt = 1; ; attempt++)
            {
                var document = await db.Documents
                    .Include(static d => d.Signers)
                    .Include(static d => d.Signatures)
                    .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
                if (document is null)
                {
                    return DocumentDesk.NotFound();
                }

                if (!document.IsAwaitingSignatureFrom(me))
                {
                    return Error.Conflict(Texts.T("Документ зараз не очікує Вашого підпису."));
                }

                var now = timeProvider.GetUtcNow();
                if (document.IsSigningTurnHeldByOther(me, now))
                {
                    var holder = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == document.SigningTurnUserId, cancellationToken);
                    return new Error(
                        SigningTurnErrorCode,
                        Texts.T("Документ зараз підписує {0}. Додати свій підпис можна буде, щойно він завершить.", holder?.FullName ?? Texts.T("інший підписант")),
                        ErrorType.Conflict);
                }

                try
                {
                    var expiresAt = document.BeginSigning(me, now);
                    await db.SaveChangesAsync(cancellationToken);

                    var first = document.FirstSignature;
                    var latest = first is not null && SignatureChain.IsCumulative(first.Format) ? document.LatestSignature : null;
                    return new SigningSession(latest?.Id, latest?.File.Sha256, first?.Format, first?.Packaging, latest is null ? 0 : document.Signatures.Count, expiresAt);
                }
                catch (DbUpdateConcurrencyException) when (attempt < DocumentDesk.MaxSaveAttempts)
                {
                    // Someone signed or took the turn at the same moment: look again.
                    db.DiscardChanges();
                }
            }
        }
    }

    /// <summary>
    /// Gives up the turn when the current user holds it.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    internal sealed class EndHandler(ApplicationDbContext db, ICurrentUser currentUser) : IRequestHandler<EndCommand, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(EndCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
            if (document is null || document.SigningTurnUserId != me)
            {
                return Result.Success();
            }

            document.EndSigning(me);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The turn ends on its own when it expires; nothing else depends on giving it up early.
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST</c> and <c>DELETE /api/v1/documents/{id}/signing-session</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var documents = DocumentDesk.MapDocuments(api);
            documents.MapPost("{id:guid}/signing-session", BeginAsync).RequirePermission(Permissions.Documents.Sign);
            documents.MapDelete("{id:guid}/signing-session", EndAsync).RequirePermission(Permissions.Documents.Sign);
        }

        /// <summary>
        /// Takes the turn to sign the document and returns the version to add the signature to (409 while another signer
        /// holds the turn).
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The signing session.</returns>
        private static async Task<Results<Ok<SigningSession>, ProblemHttpResult>> BeginAsync(Guid id, IRequestHandler<BeginCommand, Result<SigningSession>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new BeginCommand(id), cancellationToken)).ToOk();

        /// <summary>
        /// Gives up the turn to sign the document (the signing window was closed).
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> EndAsync(Guid id, IRequestHandler<EndCommand, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new EndCommand(id), cancellationToken)).ToNoContent();
    }
}
