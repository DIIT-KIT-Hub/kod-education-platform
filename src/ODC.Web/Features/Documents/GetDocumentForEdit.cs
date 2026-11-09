using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Documents;

/// <summary>
/// The document editor's data, the units the author may file under and the people to add to a route.
/// </summary>
public static class GetDocumentForEdit
{
    /// <summary>
    /// A document as the editor shows it.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record Query(Guid Id) : IQuery<Result<DocumentEditModel>>;

    /// <summary>
    /// The units the current user may register documents in.
    /// </summary>
    public sealed record AuthorUnitsQuery : IQuery<IReadOnlyList<UnitOption>>;

    /// <summary>
    /// Active users to add to a signing route (at least two letters of a name or e-mail).
    /// </summary>
    /// <param name="Term">The search text.</param>
    public sealed record SignersQuery(string Term) : IQuery<IReadOnlyList<UserLookupItem>>;

    /// <summary>
    /// Reads a draft or rejected document of its author.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="catalog">The shared catalogs (which unit types are organizations).</param>
    /// <param name="directory">The positions of people.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared document rules.</param>
    internal sealed class Handler(OrganizationDbContext db, Catalog catalog, PositionDirectory directory, ICurrentUser currentUser, DocumentDesk desk) : IRequestHandler<Query, Result<DocumentEditModel>>
    {
        /// <inheritdoc />
        public async Task<Result<DocumentEditModel>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var id = request.Id;
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
            }

            var document = await db.Documents
                .AsNoTracking()
                .Include(static d => d.Signers)
                .Include(static d => d.Signatures)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null || !await desk.IsVisibleAsync(id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            if (document.AuthorId != me || !currentUser.Has(Permissions.Documents.Create))
            {
                return Error.Forbidden(Texts.T("Змінювати документ може лише його автор."));
            }

            if (!document.IsEditable)
            {
                return Error.Conflict(Texts.T("Змінювати можна лише чернетку або відхилений документ."));
            }

            var signerIds = document.Signers.OrderBy(static s => s.Order).Select(static s => s.UserId).ToList();
            var people = (await directory.LookupAsync(db.Members.Where(u => signerIds.Contains(u.Id)), cancellationToken)).ToDictionary(static u => u.Id);
            var organizationTypes = await catalog.OrganizationTypeIdsAsync(cancellationToken);
            var unit = await db.OrganizationUnits
                .AsNoTracking()
                .Where(u => u.Id == document.OrganizationUnitId)
                .Select(u => new UnitOption(u.Id, u.Name, u.Code, organizationTypes.Contains(u.TypeId), 0))
                .FirstAsync(cancellationToken);

            return new DocumentEditModel(
                document.Id,
                document.RegistrationNumber,
                document.Title,
                document.Description,
                document.Kind,
                document.Status,
                unit,
                document.SigningMode,
                [.. signerIds.Where(people.ContainsKey).Select(signerId => people[signerId])],
                new DocumentFileInfo(document.File.FileName, document.File.ContentType, document.File.Size, document.File.Sha256, document.File.IsPdf),
                [.. document.Signatures.Select(static s => s.UserId).Distinct()]);
        }
    }

    /// <summary>
    /// Lists the author's units.
    /// </summary>
    /// <param name="desk">The shared document rules.</param>
    internal sealed class AuthorUnitsHandler(DocumentDesk desk) : IRequestHandler<AuthorUnitsQuery, IReadOnlyList<UnitOption>>
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<UnitOption>> HandleAsync(AuthorUnitsQuery request, CancellationToken cancellationToken) => desk.GetAuthorUnitsAsync(cancellationToken);
    }

    /// <summary>
    /// Finds the people.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="directory">The positions of people.</param>
    /// <param name="currentUser">The current user.</param>
    internal sealed class SignersHandler(OrganizationDbContext db, PositionDirectory directory, ICurrentUser currentUser) : IRequestHandler<SignersQuery, IReadOnlyList<UserLookupItem>>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<UserLookupItem>> HandleAsync(SignersQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var term = request.Term;
            if (!currentUser.Has(Permissions.Documents.Create) || string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            {
                return [];
            }

            var lowered = term.Trim();
            return await directory.LookupAsync(
                db.Members
                    .Where(u => u.IsActive && (u.LastName.Contains(lowered) || u.FirstName.Contains(lowered) || u.Email.Contains(lowered)))
                    .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
                    .Take(10),
                cancellationToken);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/documents/{id}/edit</c>, <c>…/author-units</c> and <c>…/signers</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var documents = DocumentDesk.MapDocuments(api);
            documents.MapGet("{id:guid}/edit", HandleAsync).RequirePermission(Permissions.Documents.Create);
            documents.MapGet("author-units", AuthorUnitsAsync).RequirePermission(Permissions.Documents.Create);
            documents.MapGet("signers", SignersAsync).RequirePermission(Permissions.Documents.Create);
        }

        /// <summary>
        /// Gets a document as the editor shows it: details, route and the signers whose signatures are on the current file.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The editor's data.</returns>
        private static async Task<Results<Ok<DocumentEditModel>, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Query, Result<DocumentEditModel>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(id), cancellationToken)).ToOk();

        /// <summary>
        /// Gets the units the user may register documents in.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The units.</returns>
        private static async Task<Ok<IReadOnlyList<UnitOption>>> AuthorUnitsAsync(IRequestHandler<AuthorUnitsQuery, IReadOnlyList<UnitOption>> handler, CancellationToken cancellationToken)
            => TypedResults.Ok(await handler.HandleAsync(new AuthorUnitsQuery(), cancellationToken));

        /// <summary>
        /// Finds active users to add to a signing route.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="term">The search text.</param>
        /// <returns>The people.</returns>
        private static async Task<Ok<IReadOnlyList<UserLookupItem>>> SignersAsync(
            IRequestHandler<SignersQuery, IReadOnlyList<UserLookupItem>> handler,
            CancellationToken cancellationToken,
            string? term = null)
            => TypedResults.Ok(await handler.HandleAsync(new SignersQuery(term ?? string.Empty), cancellationToken));
    }
}
