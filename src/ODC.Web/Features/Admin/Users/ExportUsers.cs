using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Exports the accounts matching the filters, one row per position, in the import format. Only the positions in the
/// exporter's organizations are exported, so the file can be imported back by the same administrator.
/// </summary>
public static class ExportUsers
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Filter">The grid filters.</param>
    /// <param name="Format">The file format.</param>
    [RequirePermission(Permissions.Users.Export)]
    public sealed record Query(UserQuery Filter, SpreadsheetFormat Format) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// Builds the file.
    /// </summary>
    /// <param name="db">The organization's database (members and positions).</param>
    /// <param name="catalog">The shared catalogs (role codes).</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="spreadsheet">Writes the file.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        OrganizationDbContext db,
        Catalog catalog,
        IAccessScope accessScope,
        IUserSpreadsheet spreadsheet,
        UserDirectory directory,
        IAuditLogger audit,
        TimeProvider timeProvider) : IRequestHandler<Query, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public async Task<Result<DownloadableFile>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Export, cancellationToken);
            var all = scope.IsCabinetWide;
            var units = UserDirectory.ScopeUnits(scope);
            var filtered = await directory.FilterAsync(UserDirectory.InScope(db.Members.AsNoTracking(), scope), request.Filter, cancellationToken);
            var roles = await catalog.RolesAsync(cancellationToken);
            var users = await filtered
                .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
                .Select(u => new
                {
                    u.Email,
                    u.LastName,
                    u.FirstName,
                    u.MiddleName,
                    u.PhoneNumber,
                    u.IsActive,
                    Positions = u.Positions
                        .Where(p => all || units.Contains(p.OrganizationUnitId))
                        .OrderByDescending(static p => p.IsPrimary)
                        .Select(static p => new { UnitCode = p.OrganizationUnit!.Code, p.RoleId, p.Title })
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

            var rows = users.SelectMany(u =>
            {
                var status = u.IsActive ? "активний" : "заблокований";
                return u.Positions.Count == 0
                    ? [new UserSheetRow(0, u.Email, u.LastName, u.FirstName, u.MiddleName, u.PhoneNumber, null, null, null, status)]
                    : u.Positions.Select(p => new UserSheetRow(0, u.Email, u.LastName, u.FirstName, u.MiddleName, u.PhoneNumber, p.UnitCode, roles.GetValueOrDefault(p.RoleId)?.Code, p.Title, status));
            });

            var content = spreadsheet.Write(rows, request.Format);
            audit.Add(AuditActions.UsersExported, details: $"Експортовано {users.Count} користувачів ({request.Format})");
            await db.SaveChangesAsync(cancellationToken);
            return UserDirectory.ToFile(content, $"users-{timeProvider.GetUtcNow():yyyyMMdd-HHmm}", request.Format);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/user-exports</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => api.MapGroup("user-exports").RequirePermission(Permissions.Users.Export).WithTags("UserExports").MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Exports users matching the filters (one row per position; the format is compatible with imports).
        /// </summary>
        /// <param name="filter">The grid filters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="format">The file format.</param>
        /// <returns>The file.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> HandleAsync(
            [AsParameters] UserQuery filter,
            IRequestHandler<Query, Result<DownloadableFile>> handler,
            CancellationToken cancellationToken,
            SpreadsheetFormat format = SpreadsheetFormat.Xlsx)
            => (await handler.HandleAsync(new Query(filter, format), cancellationToken)).ToFile();
    }
}
