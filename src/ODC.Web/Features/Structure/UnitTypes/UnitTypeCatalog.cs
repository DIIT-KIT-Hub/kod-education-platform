using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Structure.UnitTypes;

/// <summary>
/// Rules and queries shared by the unit type slices. The catalog is shared by every organization of the cabinet.
/// </summary>
internal static class UnitTypeCatalog
{
    /// <summary>
    /// The route group of the unit type endpoints (structure managers only).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group <c>/api/v1/unit-types</c>.</returns>
    public static RouteGroupBuilder MapUnitTypes(this IEndpointRouteBuilder api)
        => api.MapGroup("unit-types").RequirePermission(Permissions.Structure.Manage).WithTags("UnitTypes");

    /// <summary>
    /// The error of a missing type.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Тип підрозділу не знайдено."));

    /// <summary>
    /// Checks that the current user may change the catalog: besides managing the structure, it takes the
    /// administration of all organizations.
    /// </summary>
    /// <param name="currentUser">The current user.</param>
    /// <returns>The error, or <see langword="null"/> when allowed.</returns>
    public static Error? CheckEditor(ICurrentUser currentUser)
        => currentUser.Has(Permissions.Structure.Manage) && currentUser.Has(Permissions.Administration.AllOrganizations)
            ? null
            : Error.Forbidden(Texts.T("Каталог типів підрозділів спільний для всіх організацій кабінету, тому змінювати його може лише адміністратор усіх організацій."));

    /// <summary>
    /// Projects the types with their usage in display order. The id filter goes before the projection: EF Core cannot
    /// translate a filter on a property of the projected record.
    /// </summary>
    /// <param name="db">The master database (the catalog).</param>
    /// <param name="organization">The organization's database (the units of each type).</param>
    /// <param name="id">The type to load, or <see langword="null"/> for all of them.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The types.</returns>
    public static async Task<List<UnitTypeItem>> ItemsAsync(MasterDbContext db, OrganizationDbContext organization, Guid? id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(organization);
        var types = await db.UnitTypes
            .AsNoTracking()
            .Where(t => id == null || t.Id == id)
            .OrderBy(static t => t.SortOrder)
            .ThenBy(static t => t.Name)
            .Select(static t => new UnitTypeItem(t.Id, t.Code, t.Name, t.Description, t.Icon, t.IsOrganization, t.SortOrder, 0))
            .ToListAsync(cancellationToken);
        var ids = types.Select(static t => t.Id).ToList();
        var usage = await organization.OrganizationUnits
            .Where(u => ids.Contains(u.TypeId))
            .GroupBy(static u => u.TypeId)
            .Select(static g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(static g => g.Key, static g => g.Count, cancellationToken);
        return [.. types.Select(t => t with { UnitCount = usage.GetValueOrDefault(t.Id) })];
    }

    /// <summary>
    /// Validates the uniqueness of the code and the name.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="id">The type being updated, <see langword="null"/> when creating.</param>
    /// <param name="type">The new data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    public static async Task<Error?> ValidateAsync(MasterDbContext db, Guid? id, SaveUnitTypeCommand type, CancellationToken cancellationToken)
    {
        var code = type.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.UnitTypes.AnyAsync(t => t.Code == code && t.Id != id, cancellationToken))
        {
            return Error.Conflict(Texts.T("Тип із кодом «{0}» вже існує.", code));
        }

        var name = type.Name?.Trim() ?? string.Empty;
        return await db.UnitTypes.AnyAsync(t => t.Name == name && t.Id != id, cancellationToken)
            ? Error.Conflict(Texts.T("Тип «{0}» вже існує.", name))
            : null;
    }
}
