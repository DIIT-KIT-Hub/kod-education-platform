using KOD.Application.Abstractions;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Roles;

/// <summary>
/// Default <see cref="IRoleService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="audit">The audit logger.</param>
internal sealed class RoleService(IApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit) : IRoleService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<RoleListItem>>> ListAsync(PageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!currentUser.Has(Permissions.Roles.Manage))
        {
            return Error.Forbidden();
        }

        var roles = db.Roles.AsNoTracking();
        if (request.Search is { } search)
        {
            var term = search;
            roles = roles.Where(r => r.Name.Contains(term) || r.Code.Contains(term));
        }

        roles = request.SortBy switch
        {
            "code" => roles.OrderByDirection(static r => r.Code, request.Descending),
            "name" => roles.OrderByDirection(static r => r.Name, request.Descending),
            _ => roles.OrderByDescending(static r => r.IsSystem).ThenBy(static r => r.CreatedAt),
        };

        return await roles
            .Select(r => new RoleListItem(
                r.Id,
                r.Code,
                StoredText.Display(r.Name),
                StoredText.Display(r.Description),
                r.IsSystem,
                r.IsLeadership,
                r.Permissions.Count,
                db.StaffPositions.Where(p => p.RoleId == r.Id).Select(static p => p.UserId).Distinct().Count()))
            .ToPagedResultAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDetails>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Roles.Manage))
        {
            return Error.Forbidden();
        }

        var role = await db.Roles.AsNoTracking().Include(static r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return Error.NotFound(Texts.T("Роль не знайдено."));
        }

        var users = await db.StaffPositions.Where(p => p.RoleId == id).Select(static p => p.UserId).Distinct().CountAsync(cancellationToken);
        return new RoleDetails(
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsAdministrator,
            role.IsLeadership,
            [.. role.Permissions.Select(static p => p.Permission)],
            users);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleOption>> GetOptionsAsync(CancellationToken cancellationToken)
        => await db.Roles
            .AsNoTracking()
            .OrderByDescending(static r => r.IsSystem)
            .ThenBy(static r => r.CreatedAt)
            .Select(static r => new RoleOption(r.Id, r.Code, StoredText.Display(r.Name)))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(SaveRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Roles.Manage))
        {
            return Error.Forbidden();
        }

        var code = command.Code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (code.Length == 0 || !code.All(static c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
        {
            return Error.Validation(Texts.T("Код ролі може містити лише малі латинські літери, цифри та «-»."));
        }

        if (await db.Roles.AnyAsync(r => r.Code == code, cancellationToken))
        {
            return Error.Conflict(Texts.T("Роль із кодом «{0}» вже існує.", code));
        }

        return await DomainErrors.GuardAsync<Guid>(async () =>
        {
            var role = new Role(code, command.Name, command.Description, isSystem: false);
            role.Update(command.Name, command.Description, command.IsLeadership);
            role.SetPermissions(command.Permissions);
            db.Roles.Add(role);
            audit.Add(AuditActions.RoleChanged, nameof(Role), role.Id.ToString(), $"Створено роль «{role.Name}»: {string.Join(", ", command.Permissions)}");
            await db.SaveChangesAsync(cancellationToken);
            return role.Id;
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Roles.Manage))
        {
            return Error.Forbidden();
        }

        var role = await db.Roles.Include(static r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return Error.NotFound(Texts.T("Роль не знайдено."));
        }

        // Holders get the new permissions at the next periodic session validation.
        return await DomainErrors.GuardAsync(async () =>
        {
            role.Update(command.Name, command.Description, command.IsLeadership);
            role.SetPermissions(command.Permissions);
            audit.Add(AuditActions.RoleChanged, nameof(Role), role.Id.ToString(), $"Змінено роль «{role.Name}»: {string.Join(", ", role.Permissions.Select(static p => p.Permission))}");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Roles.Manage))
        {
            return Error.Forbidden();
        }

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return Error.NotFound(Texts.T("Роль не знайдено."));
        }

        if (role.IsSystem)
        {
            return Error.Validation(Texts.T("Системну роль не можна видалити."));
        }

        if (await db.StaffPositions.AnyAsync(p => p.RoleId == id, cancellationToken))
        {
            return Error.Conflict(Texts.T("Роль призначена користувачам — спочатку змініть їхні посади."));
        }

        db.Roles.Remove(role);
        audit.Add(AuditActions.RoleDeleted, nameof(Role), role.Id.ToString(), $"Видалено роль «{role.Name}»");
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
