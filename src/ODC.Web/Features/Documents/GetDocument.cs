using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Gets a document with its signing route, signatures and the actions the current user may take; opening it answers the
/// notifications about it.
/// </summary>
public static class GetDocument
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record Query(Guid Id) : IQuery<Result<DocumentDetails>>;

    /// <summary>
    /// Reads the document when visible.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="domainEvents">Raises what the user has seen (their notifications about it are done).</param>
    internal sealed class Handler(ApplicationDbContext db, ICurrentUser currentUser, DocumentDesk desk, DomainEvents domainEvents) : IRequestHandler<Query, Result<DocumentDetails>>
    {
        /// <inheritdoc />
        public async Task<Result<DocumentDetails>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            if (!await desk.IsVisibleAsync(id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            var document = await db.Documents
                .AsNoTracking()
                .Include(static d => d.Author)
                .Include(static d => d.OrganizationUnit)
                .Include(static d => d.Signers).ThenInclude(static s => s.User)
                .Include(static d => d.Signatures).ThenInclude(static s => s.User)
                .FirstAsync(d => d.Id == id, cancellationToken);

            var signerIds = document.Signers.Select(static s => s.UserId).ToList();
            var positions = await db.StaffPositions
                .AsNoTracking()
                .Where(p => signerIds.Contains(p.UserId) && p.IsPrimary)
                .Select(static p => new { p.UserId, p.Title, Role = p.Role!.Name, Unit = p.OrganizationUnit!.Name })
                .ToDictionaryAsync(static p => p.UserId, static p => StoredText.PositionInUnit(p.Title, p.Role, p.Unit)!, cancellationToken);

            // Opening the document answers the notifications about it.
            domainEvents.Raise(new ItemSeen(me, id));

            var isAuthor = document.AuthorId == me;
            var canManage = currentUser.Has(Permissions.Documents.Manage);
            var awaitingMe = document.IsAwaitingSignatureFrom(me) && currentUser.Has(Permissions.Documents.Sign);
            var hasSignatures = document.Signatures.Count > 0;
            var actions = new DocumentActions(
                CanEdit: isAuthor && document.IsEditable && currentUser.Has(Permissions.Documents.Create),
                CanSubmit: isAuthor && document.IsEditable && document.Signers.Count > 0,
                CanWithdraw: isAuthor && document.Status == DocumentStatus.InSigning && !hasSignatures,
                CanSign: awaitingMe,
                CanReject: awaitingMe,
                CanArchive: (isAuthor || canManage) && document.Status is DocumentStatus.Signed or DocumentStatus.Rejected,
                CanDelete: (isAuthor || canManage) && !hasSignatures && document.Status != DocumentStatus.Archived);

            return new DocumentDetails(
                document.Id,
                document.RegistrationNumber,
                document.Title,
                document.Description,
                document.Kind,
                document.Status,
                document.SigningMode,
                new UserRef(document.AuthorId, document.Author!.FullName, document.Author.PhotoId),
                new UnitRef(document.OrganizationUnitId, document.OrganizationUnit!.Name),
                new DocumentFileInfo(document.File.FileName, document.File.ContentType, document.File.Size, document.File.Sha256, document.File.IsPdf),
                document.CreatedAt,
                document.SubmittedAt,
                document.CompletedAt,
                document.Signers
                    .OrderBy(static s => s.Order)
                    .Select(s => new DocumentSignerItem(s.UserId, s.User!.FullName, positions.GetValueOrDefault(s.UserId), s.Order, s.Status, s.DecidedAt, s.Comment, s.User.PhotoId))
                    .ToList(),
                document.Signatures
                    .OrderBy(static s => s.SignedAt)
                    .Select(s => new DocumentSignatureItem(s.Id, s.UserId, s.User!.FullName, s.Format, s.Packaging, s.Level, s.KeyMedia, s.Certificate, s.SignedAt, SignedFileName.For(document.File.FileName, s.Format), s.File.Size))
                    .ToList(),
                actions,
                document.FirstSignature is { } first && SignatureChain.IsCumulative(first.Format) ? document.LatestSignature?.Id : null);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/documents/{id}</c> and <c>GET /api/v1/documents/{id}/signatures</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var documents = DocumentDesk.MapDocuments(api);
            documents.MapGet("{id:guid}", HandleAsync);
            documents.MapGet("{id:guid}/signatures", SignaturesAsync);
        }

        /// <summary>
        /// Gets a document with its signing route and signatures.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The document or the problem.</returns>
        private static async Task<Results<Ok<DocumentDetails>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<DocumentDetails>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();

        /// <summary>
        /// Lists the signatures of a document.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The signatures.</returns>
        private static async Task<Results<Ok<IReadOnlyList<DocumentSignatureItem>>, ProblemHttpResult>> SignaturesAsync(
            Guid id,
            IRequestHandler<Query, Result<DocumentDetails>> handler,
            CancellationToken cancellationToken)
        {
            var document = await handler.HandleAsync(new Query(id), cancellationToken);
            return document.IsSuccess ? TypedResults.Ok(document.Value.Signatures) : document.Error.ToProblem();
        }
    }
}
