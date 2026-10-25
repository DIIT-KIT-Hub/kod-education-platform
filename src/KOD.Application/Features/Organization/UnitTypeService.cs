using KOD.Application.Abstractions;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Organization;

/// <summary>
/// Default <see cref="IUnitTypeService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="audit">The audit logger.</param>
internal sealed class UnitTypeService(IApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit) : IUnitTypeService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnitTypeItem>>> ListAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        return Result<IReadOnlyList<UnitTypeItem>>.Success(await Items().ToListAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitTypeRef>> GetOptionsAsync(CancellationToken cancellationToken)
        => await db.UnitTypes
            .AsNoTracking()
            .OrderBy(static t => t.SortOrder)
            .ThenBy(static t => t.Name)
            .Select(static t => new UnitTypeRef(t.Id, t.Name, t.Icon, t.IsOrganization))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Result<UnitTypeItem>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var type = await Items().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return type is null ? Error.NotFound("Тип підрозділу не знайдено.") : type;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(SaveUnitTypeCommand command, CancellationToken cancellationToken)
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
            var type = new UnitType(command.Code, command.Name, command.IsOrganization);
            type.Update(command.Code, command.Name, command.Description, command.Icon, command.IsOrganization, command.SortOrder);
            db.UnitTypes.Add(type);
            audit.Add(AuditActions.UnitTypeChanged, nameof(UnitType), type.Id.ToString(), $"Створено тип {type.Code} «{type.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return type.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveUnitTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var type = await db.UnitTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (type is null)
        {
            return Error.NotFound("Тип підрозділу не знайдено.");
        }

        var validation = await ValidateAsync(id, command, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        if (type.IsOrganization && !command.IsOrganization
            && await db.OrganizationUnits.AnyAsync(u => u.TypeId == id && u.ParentId == null, cancellationToken))
        {
            return Error.Conflict("Цей тип мають організації верхнього рівня, тому він має лишатися організацією. Спершу змініть їхній тип або вкладіть їх в іншу організацію.");
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            type.Update(command.Code, command.Name, command.Description, command.Icon, command.IsOrganization, command.SortOrder);
            audit.Add(AuditActions.UnitTypeChanged, nameof(UnitType), type.Id.ToString(), $"Змінено тип {type.Code} «{type.Name}»");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Structure.Manage))
        {
            return Error.Forbidden();
        }

        var type = await db.UnitTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (type is null)
        {
            return Error.NotFound("Тип підрозділу не знайдено.");
        }

        var used = await db.OrganizationUnits.CountAsync(u => u.TypeId == id, cancellationToken);
        if (used > 0)
        {
            return Error.Conflict($"Тип «{type.Name}» мають підрозділи ({used}). Спершу змініть їхній тип.");
        }

        db.UnitTypes.Remove(type);
        audit.Add(AuditActions.UnitTypeDeleted, nameof(UnitType), type.Id.ToString(), $"Видалено тип {type.Code} «{type.Name}»");
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Projects the types with their usage in display order.
    /// </summary>
    /// <returns>The query.</returns>
    private IQueryable<UnitTypeItem> Items()
        => db.UnitTypes
            .AsNoTracking()
            .OrderBy(static t => t.SortOrder)
            .ThenBy(static t => t.Name)
            .Select(t => new UnitTypeItem(
                t.Id,
                t.Code,
                t.Name,
                t.Description,
                t.Icon,
                t.IsOrganization,
                t.SortOrder,
                db.OrganizationUnits.Count(u => u.TypeId == t.Id)));

    /// <summary>
    /// Validates the uniqueness of the code and the name.
    /// </summary>
    /// <param name="id">The type being updated, <see langword="null"/> when creating.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    private async Task<Error?> ValidateAsync(Guid? id, SaveUnitTypeCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await db.UnitTypes.AnyAsync(t => t.Code == code && t.Id != id, cancellationToken))
        {
            return Error.Conflict($"Тип із кодом «{code}» вже існує.");
        }

        var name = command.Name?.Trim() ?? string.Empty;
        return await db.UnitTypes.AnyAsync(t => t.Name == name && t.Id != id, cancellationToken)
            ? Error.Conflict($"Тип «{name}» вже існує.")
            : null;
    }
}
