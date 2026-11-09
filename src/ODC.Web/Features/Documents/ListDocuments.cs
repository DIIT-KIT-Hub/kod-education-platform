using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Documents;

/// <summary>
/// The document registry: everything the current user sees, or one of the personal lists (awaiting my signature,
/// authored, involved, signed by me).
/// </summary>
public static class ListDocuments
{
    /// <summary>
    /// Filters, sorts and pages the documents.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="accessScope">The data-level access rules.</param>
    internal sealed class Handler(OrganizationDbContext db, ICurrentUser currentUser, IAccessScope accessScope) : IRequestHandler<DocumentQuery, Result<PagedResult<DocumentListItem>>>
    {
        /// <inheritdoc />
        public async Task<Result<PagedResult<DocumentListItem>>> HandleAsync(DocumentQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var documents = db.Documents.AsNoTracking();
            documents = request.Scope switch
            {
                DocumentScope.AwaitingMe => documents.Where(d =>
                    d.Status == DocumentStatus.InSigning
                    && d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Pending
                        && (d.SigningMode == SigningMode.Parallel
                            || !d.Signers.Any(o => o.Status == SignerStatus.Pending && o.Order < s.Order)))),
                DocumentScope.Authored => documents.Where(d => d.AuthorId == me),
                DocumentScope.Involved => documents.Where(d => d.Signers.Any(s => s.UserId == me)),
                DocumentScope.SignedByMe => documents.Where(d => d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Signed)),
                _ => await accessScope.VisibleDocumentsAsync(documents, cancellationToken),
            };

            if (request.Status is { } status)
            {
                documents = documents.Where(d => d.Status == status);
            }

            if (request.Kind is { } kind)
            {
                documents = documents.Where(d => d.Kind == kind);
            }

            if (request.Search is { } search)
            {
                var term = search;
                documents = documents.Where(d => d.Title.Contains(term) || d.RegistrationNumber.Contains(term));
            }

            documents = request.SortBy switch
            {
                "number" => documents.OrderByDirection(static d => d.RegistrationNumber, request.IsDescending),
                "title" => documents.OrderByDirection(static d => d.Title, request.IsDescending),
                "status" => documents.OrderByDirection(static d => d.Status, request.IsDescending),
                "created" => documents.OrderByDirection(static d => d.CreatedAt, request.IsDescending),
                _ => documents.OrderByDescending(static d => d.CreatedAt),
            };

            return await documents
                .Select(d => new DocumentListItem(
                    d.Id,
                    d.RegistrationNumber,
                    d.Title,
                    d.Kind,
                    d.Status,
                    d.Author!.LastName + " " + d.Author.FirstName,
                    d.OrganizationUnit!.Name,
                    d.CreatedAt,
                    d.Signers.Count(static s => s.Status == SignerStatus.Signed),
                    d.Signers.Count,
                    d.Status == DocumentStatus.InSigning
                        && d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Pending
                            && (d.SigningMode == SigningMode.Parallel
                                || !d.Signers.Any(o => o.Status == SignerStatus.Pending && o.Order < s.Order))),
                    d.Author.PhotoId))
                .ToPagedResultAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/documents</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => DocumentDesk.MapDocuments(api).MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Lists documents visible to the current user.
        /// </summary>
        /// <param name="query">The scope, filters, sorting and paging.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The page.</returns>
        private static async Task<Results<Ok<PagedResult<DocumentListItem>>, ProblemHttpResult>> HandleAsync(
            [AsParameters] DocumentQuery query,
            IRequestHandler<DocumentQuery, Result<PagedResult<DocumentListItem>>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(query, cancellationToken)).ToOk();
    }
}
