using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Default <see cref="IOrganizationService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data access rules of the current user.</param>
/// <param name="audit">The audit logger.</param>
internal sealed class OrganizationService(IApplicationDbContext db, ICurrentUser currentUser, IAccessScope accessScope, IAuditLogger audit) : IOrganizationService
{
    /// <summary>
    /// The error shown when a unit is outside the organizations the current user manages.
    /// </summary>
    private const string OutsideScopeMessage = "Підрозділ належить організації, структурою якої ви не керуєте.";

    /// <summary>
    /// The error shown when an organization administrator tries to create or move a top-level organization.
    /// </summary>
    private const string TopLevelMessage = "Організації верхнього рівня створює й переміщує лише адміністратор усіх організацій. Оберіть батьківський підрозділ у своїй організації.";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnitTreeNode>>> GetTreeAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Structure.View))
        {
            return Error.Forbidden();
        }

        var units = await LoadUnitsAsync(cancellationToken);
        return Result<IReadOnlyList<UnitTreeNode>>.Success(BuildTree(units, null, includeInactive));
    }

    /// <inheritdoc />
    public async Task<Result<UnitDetails>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Structure.View))
        {
            return Error.Forbidden();
        }

        var units = await LoadUnitsAsync(cancellationToken);
        var unit = units.Find(u => u.Id == id);
        if (unit is null)
        {
            return Error.NotFound(Texts.T("Підрозділ не знайдено."));
        }

        var byId = units.ToDictionary(static u => u.Id);
        var parents = units.ToDictionary(static u => u.Id, static u => u.ParentId);
        var organizations = units.Where(static u => u.Type.IsOrganization).Select(static u => u.Id).ToHashSet();
        var path = OrganizationTree.PathTo(parents, id)
            .Select(pid => new UnitRef(pid, byId[pid].Name))
            .ToList();
        var organization = byId[OrganizationTree.OrganizationOf(parents, organizations, id)];
        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);

        return new UnitDetails(
            unit.Id,
            unit.Code,
            unit.Name,
            unit.ShortName,
            unit.Type,
            unit.IsActive,
            unit.ParentId,
            unit.SortOrder,
            path,
            new UnitRef(organization.Id, organization.Name),
            BuildTree(units, id, includeInactive: true),
            unit.StaffCount,
            currentUser.Has(Permissions.Structure.Manage) && scope.Contains(id));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<UnitStaffItem>>> GetStaffAsync(Guid id, PageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!currentUser.Has(Permissions.Structure.View))
        {
            return Error.Forbidden();
        }

        var positions = db.StaffPositions.AsNoTracking().Where(p => p.OrganizationUnitId == id && p.User!.IsActive);
        if (request.Search is { } search)
        {
            var term = search;
            positions = positions.Where(p => p.User!.LastName.Contains(term) || p.User.FirstName.Contains(term));
        }

        return await positions
            .OrderByDescending(static p => p.Role!.IsLeadership)
            .ThenBy(static p => p.User!.LastName)
            .ThenBy(static p => p.User!.FirstName)
            .Select(static p => new UnitStaffItem(
                p.UserId,
                p.User!.LastName + " " + p.User.FirstName + (p.User.MiddleName != null ? " " + p.User.MiddleName : string.Empty),
                p.Title ?? StoredText.Display(p.Role!.Name),
                p.User.Email!,
                p.User.PhoneNumber,
                p.IsPrimary,
                p.Role!.IsLeadership))
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<UnitListItem>>> ListAsync(UnitQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
        var units = db.OrganizationUnits.AsNoTracking();
        if (scope.Units is { } scoped)
        {
            var managed = scoped.ToList();
            units = units.Where(u => managed.Contains(u.Id));
        }

        if (query.TypeId is { } typeId)
        {
            units = units.Where(u => u.TypeId == typeId);
        }

        if (query.OrganizationId is { } organizationId)
        {
            var links = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.ParentId }).ToListAsync(cancellationToken);
            var subtree = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), [organizationId]).ToList();
            units = units.Where(u => subtree.Contains(u.Id));
        }

        if (query.Search is { } search)
        {
            var term = search;
            units = units.Where(u => u.Name.Contains(term) || u.Code.Contains(term));
        }

        units = query.SortBy switch
        {
            "code" => units.OrderByDirection(static u => u.Code, query.Descending),
            "type" => units.OrderByDirection(static u => u.Type!.Name, query.Descending),
            _ => units.OrderByDirection(static u => u.Name, query.Descending),
        };

        return await units
            .Select(u => new UnitListItem(
                u.Id,
                u.Code,
                u.Name,
                new UnitTypeRef(u.TypeId, StoredText.Display(u.Type!.Name), u.Type.Icon, u.Type.IsOrganization),
                u.Parent != null ? u.Parent.Name : null,
                u.IsActive,
                db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id)))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitOption>> GetOptionsAsync(string permission, CancellationToken cancellationToken)
    {
        var scope = await accessScope.GetAdministrationScopeAsync(permission, cancellationToken);
        if (scope.Units is { Count: 0 })
        {
            return [];
        }

        var units = await LoadUnitsAsync(cancellationToken);
        var result = new List<UnitOption>(units.Count);
        void Walk(IReadOnlyList<UnitTreeNode> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                // Units outside the scope are skipped without indenting their descendants, so the scope's topmost units have depth 0.
                var included = scope.Contains(node.Id);
                if (included)
                {
                    result.Add(new UnitOption(node.Id, node.Name, node.Code, node.Type.IsOrganization, depth));
                }

                Walk(node.Children, included ? depth + 1 : depth);
            }
        }

        Walk(BuildTree(units, null, includeInactive: false), 0);
        return result;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(SaveUnitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var validation = await ValidateAsync(null, command, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
        if (command.ParentId is not { } parentId)
        {
            if (!scope.IsCabinetWide)
            {
                return Error.Forbidden(Texts.T(TopLevelMessage));
            }
        }
        else if (!scope.Contains(parentId))
        {
            return Error.Forbidden(Texts.T(OutsideScopeMessage));
        }

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var unit = new OrganizationUnit(command.Code, command.Name, command.TypeId, command.ParentId);
            unit.Update(command.Code, command.Name, command.ShortName, command.TypeId, command.SortOrder);
            unit.SetActive(command.IsActive);
            db.OrganizationUnits.Add(unit);
            audit.Add(AuditActions.UnitChanged, nameof(OrganizationUnit), unit.Id.ToString(), $"Створено {unit.Code} «{unit.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return unit.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveUnitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var unit = await db.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (unit is null)
        {
            return Error.NotFound(Texts.T("Підрозділ не знайдено."));
        }

        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Structure.Manage, cancellationToken);
        if (!scope.Contains(id))
        {
            return Error.Forbidden(Texts.T(OutsideScopeMessage));
        }

        var validation = await ValidateAsync(id, command, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        if (command.ParentId != unit.ParentId)
        {
            if (command.ParentId is not { } parentId)
            {
                if (!scope.IsCabinetWide)
                {
                    return Error.Forbidden(Texts.T(TopLevelMessage));
                }
            }
            else if (!scope.Contains(parentId))
            {
                return Error.Forbidden(Texts.T(OutsideScopeMessage));
            }
        }

        // Turning an organization into an ordinary unit merges it into the organization above and widens the scope of its staff.
        if (!scope.IsCabinetWide
            && await db.UnitTypes.AnyAsync(t => t.Id == unit.TypeId && t.IsOrganization, cancellationToken)
            && !await db.UnitTypes.AnyAsync(t => t.Id == command.TypeId && t.IsOrganization, cancellationToken))
        {
            return Error.Forbidden(Texts.T("Перетворити організацію на звичайний підрозділ може лише адміністратор усіх організацій: це змінює межі організацій і права їхніх працівників."));
        }

        var links = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.ParentId }).ToListAsync(cancellationToken);
        var descendants = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), [id]);
        descendants.Remove(id);

        return await DomainErrors.GuardAsync(async () =>
        {
            unit.Update(command.Code, command.Name, command.ShortName, command.TypeId, command.SortOrder);
            unit.MoveTo(command.ParentId, descendants);
            unit.SetActive(command.IsActive);
            audit.Add(AuditActions.UnitChanged, nameof(OrganizationUnit), unit.Id.ToString(), $"Змінено {unit.Code} «{unit.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <summary>
    /// Builds the subtree under <paramref name="parentId"/>.
    /// </summary>
    /// <param name="units">All units.</param>
    /// <param name="parentId">The parent, <see langword="null"/> for the top-level organizations.</param>
    /// <param name="includeInactive">Whether inactive units are included.</param>
    /// <returns>The child nodes.</returns>
    private static List<UnitTreeNode> BuildTree(List<UnitRow> units, Guid? parentId, bool includeInactive)
    {
        var children = units.ToLookup(static u => u.ParentId);
        List<UnitTreeNode> Build(Guid? id) =>
        [
            .. children[id]
                .Where(u => includeInactive || u.IsActive)
                .OrderBy(static u => u.SortOrder)
                .ThenBy(static u => u.Name, StringComparer.CurrentCulture)
                .Select(u => new UnitTreeNode(u.Id, u.Code, u.Name, u.ShortName, u.Type, u.IsActive, u.StaffCount, Build(u.Id))),
        ];

        return Build(parentId);
    }

    /// <summary>
    /// Loads all units with their staff counts.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units.</returns>
    private Task<List<UnitRow>> LoadUnitsAsync(CancellationToken cancellationToken)
        => db.OrganizationUnits
            .AsNoTracking()
            .Select(u => new UnitRow(
                u.Id,
                u.ParentId,
                u.Code,
                u.Name,
                u.ShortName,
                new UnitTypeRef(u.TypeId, StoredText.Display(u.Type!.Name), u.Type.Icon, u.Type.IsOrganization),
                u.IsActive,
                u.SortOrder,
                db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id)))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Validates code uniqueness, the type and the parent reference.
    /// </summary>
    /// <param name="id">The unit being updated, <see langword="null"/> when creating.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    private async Task<Error?> ValidateAsync(Guid? id, SaveUnitCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.OrganizationUnits.AnyAsync(u => u.Code == code && u.Id != id, cancellationToken))
        {
            return Error.Conflict(Texts.T("Підрозділ із кодом «{0}» вже існує.", code));
        }

        var type = await db.UnitTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TypeId, cancellationToken);
        if (type is null)
        {
            return Error.Validation(Texts.T("Оберіть тип підрозділу."));
        }

        if (command.ParentId is { } parentId && !await db.OrganizationUnits.AnyAsync(u => u.Id == parentId, cancellationToken))
        {
            return Error.Validation(Texts.T("Батьківський підрозділ не знайдено."));
        }

        if (command.ParentId is null && !type.IsOrganization)
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
    private sealed record UnitRow(
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
