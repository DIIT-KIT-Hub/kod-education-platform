using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Rules shared by the role slices.
/// </summary>
internal static class RoleRules
{
    /// <summary>
    /// The route group of the role endpoints (role managers only; the permission is cabinet-wide).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group <c>/api/v1/roles</c>.</returns>
    public static RouteGroupBuilder MapRoles(this IEndpointRouteBuilder api)
        => api.MapGroup("roles").RequirePermission(Permissions.Roles.Manage).WithTags("Roles");

    /// <summary>
    /// The error of a missing role.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Роль не знайдено."));

    /// <summary>
    /// The error of a name another role has.
    /// </summary>
    /// <param name="name">The requested name.</param>
    /// <returns>The error.</returns>
    public static Error NameTaken(string name) => Error.Conflict(Texts.T("Роль із назвою «{0}» вже існує.", name.Trim()));

    /// <summary>
    /// Determines whether another role already has the name (names are unique, like Identity role names).
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="name">The requested name.</param>
    /// <param name="exceptId">The role being edited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    public static async Task<bool> NameTakenAsync(ApplicationDbContext db, string? name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Role.NormalizeName(name);
        return await db.Roles.AnyAsync(r => r.NormalizedName == normalized && r.Id != exceptId, cancellationToken);
    }
}
