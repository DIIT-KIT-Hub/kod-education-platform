using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Structure.Units;

/// <summary>
/// Rules and queries shared by the unit slices: the whole structure is small enough to load at once and build trees in
/// memory.
/// </summary>
internal static class UnitTree
{
    /// <summary>
    /// The error shown when a unit is outside the organizations the current user manages.
    /// </summary>
    public const string OutsideScopeMessage = "Підрозділ належить організації, структурою якої Ви не керуєте.";

    /// <summary>
    /// The error shown when an organization administrator tries to create or move a top-level organization.
    /// </summary>
    public const string TopLevelMessage = "Організації верхнього рівня створює й переміщує лише адміністратор усіх організацій. Оберіть батьківський підрозділ у своїй організації.";

    /// <summary>
    /// The route group of the unit endpoints (people who see the structure).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group <c>/api/v1/organization-units</c>.</returns>
    public static RouteGroupBuilder MapUnits(this IEndpointRouteBuilder api)
        => api.MapGroup("organization-units").RequirePermission(Permissions.Structure.View).WithTags("OrganizationUnits");

    /// <summary>
    /// The error of a missing unit.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Підрозділ не знайдено."));

    /// <summary>
    /// Builds the subtree under <paramref name="parentId"/>.
    /// </summary>
    /// <param name="units">All units.</param>
    /// <param name="parentId">The parent, <see langword="null"/> for the top-level organizations.</param>
    /// <param name="includeInactive">Whether inactive units are included.</param>
    /// <returns>The child nodes.</returns>
    public static List<UnitTreeNode> Build(List<UnitRow> units, Guid? parentId, bool includeInactive)
    {
        var children = units.ToLookup(static u => u.ParentId);
        List<UnitTreeNode> Nodes(Guid? id) =>
        [
            .. children[id]
                .Where(u => includeInactive || u.IsActive)
                .OrderBy(static u => u.SortOrder)
                .ThenBy(static u => u.Name, StringComparer.CurrentCulture)
                .Select(u => new UnitTreeNode(u.Id, u.Code, u.Name, u.ShortName, u.Type, u.IsActive, u.StaffCount, Nodes(u.Id))),
        ];

        return Nodes(parentId);
    }

    /// <summary>
    /// Loads all units with their staff counts.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="catalog">The shared catalogs (unit types).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units.</returns>
    public static async Task<List<UnitRow>> LoadAsync(OrganizationDbContext db, Catalog catalog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var types = await catalog.UnitTypesAsync(cancellationToken);
        var units = await db.OrganizationUnits
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.ParentId,
                u.Code,
                u.Name,
                u.ShortName,
                u.TypeId,
                u.IsActive,
                u.SortOrder,
                StaffCount = db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id),
            })
            .ToListAsync(cancellationToken);
        return [.. units.Select(u => new UnitRow(u.Id, u.ParentId, u.Code, u.Name, u.ShortName, CatalogUnitType.RefOf(types, u.TypeId), u.IsActive, u.SortOrder, u.StaffCount))];
    }

    /// <summary>
    /// Validates code uniqueness, the type and the parent reference.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="catalog">The shared catalogs (unit types).</param>
    /// <param name="id">The unit being updated, <see langword="null"/> when creating.</param>
    /// <param name="unit">The new data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    public static async Task<Error?> ValidateAsync(OrganizationDbContext db, Catalog catalog, Guid? id, SaveUnitCommand unit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var code = unit.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.OrganizationUnits.AnyAsync(u => u.Code == code && u.Id != id, cancellationToken))
        {
            return Error.Conflict(Texts.T("Підрозділ із кодом «{0}» вже існує.", code));
        }

        var type = (await catalog.UnitTypesAsync(cancellationToken)).GetValueOrDefault(unit.TypeId);
        if (type is null)
        {
            return Error.Validation(Texts.T("Оберіть тип підрозділу."));
        }

        if (unit.ParentId is { } parentId && !await db.OrganizationUnits.AnyAsync(u => u.Id == parentId, cancellationToken))
        {
            return Error.Validation(Texts.T("Батьківський підрозділ не знайдено."));
        }

        if (unit.ParentId is null && !type.IsOrganization)
        {
            return Error.Validation(Texts.T("Верхній рівень структури займають лише організації. Оберіть батьківський підрозділ або тип, позначений як організація (тип «{0}» ним не є).", Texts.T(type.Name)));
        }

        return null;
    }

    /// <summary>
    /// A flat unit row used to build trees in memory.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="ParentId">The parent identifier.</param>
    /// <param name="Code">The code.</param>
    /// <param name="Name">The name.</param>
    /// <param name="ShortName">The abbreviation.</param>
    /// <param name="Type">The type.</param>
    /// <param name="IsActive">Whether the unit is active.</param>
    /// <param name="SortOrder">The display order.</param>
    /// <param name="StaffCount">The number of positions.</param>
    public sealed record UnitRow(
        Guid Id,
        Guid? ParentId,
        string Code,
        string Name,
        string? ShortName,
        UnitTypeRef Type,
        bool IsActive,
        int SortOrder,
        int StaffCount);
}
