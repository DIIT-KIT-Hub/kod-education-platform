using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Default <see cref="IOrganizationService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="audit">The audit logger.</param>
internal sealed class OrganizationService(IApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit) : IOrganizationService
{
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
            return Error.NotFound("Підрозділ не знайдено.");
        }

        var byId = units.ToDictionary(static u => u.Id);
        var path = OrganizationTree.PathTo(units.ToDictionary(static u => u.Id, static u => u.ParentId), id)
            .Select(pid => new UnitRef(pid, byId[pid].Name))
            .ToList();

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
            BuildTree(units, id, includeInactive: true),
            unit.StaffCount);
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
            .OrderByDescending(static p => p.Role!.Code == SystemRoles.HeadOfDepartment || p.Role.Code == SystemRoles.Dean || p.Role.Code == SystemRoles.InstituteDirector || p.Role.Code == SystemRoles.Rector)
            .ThenBy(static p => p.User!.LastName)
            .ThenBy(static p => p.User!.FirstName)
            .Select(static p => new UnitStaffItem(
                p.UserId,
                p.User!.LastName + " " + p.User.FirstName + (p.User.MiddleName != null ? " " + p.User.MiddleName : string.Empty),
                p.Title ?? p.Role!.Name,
                p.User.Email!,
                p.User.PhoneNumber,
                p.IsPrimary))
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

        var units = db.OrganizationUnits.AsNoTracking();
        if (query.Type is { } type)
        {
            units = units.Where(u => u.Type == type);
        }

        if (query.Search is { } search)
        {
            var term = search;
            units = units.Where(u => u.Name.Contains(term) || u.Code.Contains(term));
        }

        units = query.SortBy switch
        {
            "code" => units.OrderByDirection(static u => u.Code, query.Descending),
            "type" => units.OrderByDirection(static u => u.Type, query.Descending),
            _ => units.OrderByDirection(static u => u.Name, query.Descending),
        };

        return await units
            .Select(u => new UnitListItem(
                u.Id,
                u.Code,
                u.Name,
                u.Type,
                u.Parent != null ? u.Parent.Name : null,
                u.IsActive,
                db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id)))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitOption>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var units = await LoadUnitsAsync(cancellationToken);
        var result = new List<UnitOption>(units.Count);
        void Walk(IReadOnlyList<UnitTreeNode> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                result.Add(new UnitOption(node.Id, node.Name, node.Code, node.Type, depth));
                Walk(node.Children, depth + 1);
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

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var unit = new OrganizationUnit(command.Code, command.Name, command.Type, command.ParentId);
            unit.Update(command.Code, command.Name, command.ShortName, command.Type, command.SortOrder);
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
            return Error.NotFound("Підрозділ не знайдено.");
        }

        var validation = await ValidateAsync(id, command, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var links = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.ParentId }).ToListAsync(cancellationToken);
        var descendants = OrganizationTree.WithDescendants(links.Select(static l => (l.Id, l.ParentId)), [id]);
        descendants.Remove(id);

        return await DomainErrors.GuardAsync(async () =>
        {
            unit.Update(command.Code, command.Name, command.ShortName, command.Type, command.SortOrder);
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
    /// <param name="parentId">The parent, <see langword="null"/> for roots.</param>
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
                u.Type,
                u.IsActive,
                u.SortOrder,
                db.StaffPositions.Count(p => p.OrganizationUnitId == u.Id)))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Validates code uniqueness and the parent reference.
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
            return Error.Conflict($"Підрозділ із кодом «{code}» вже існує.");
        }

        if (command.ParentId is { } parentId && !await db.OrganizationUnits.AnyAsync(u => u.Id == parentId, cancellationToken))
        {
            return Error.Validation("Батьківський підрозділ не знайдено.");
        }

        if (command.ParentId is null && command.Type != OrganizationUnitType.University)
        {
            return Error.Validation("Кореневим може бути лише заклад освіти.");
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
        OrganizationUnitType Type,
        bool IsActive,
        int SortOrder,
        int StaffCount);
}
