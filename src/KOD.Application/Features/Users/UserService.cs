using System.Net.Mail;
using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Common;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Users;

/// <summary>
/// Default <see cref="IUserService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="identity">The identity service.</param>
/// <param name="permissions">The permission resolver.</param>
/// <param name="spreadsheet">The spreadsheet reader/writer.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class UserService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    IPermissionResolver permissions,
    IUserSpreadsheet spreadsheet,
    IAuditLogger audit,
    TimeProvider timeProvider) : IUserService
{
    /// <summary>
    /// Accepted values of the «Статус» column meaning a blocked account.
    /// </summary>
    private static readonly string[] BlockedStatuses = ["заблокований", "заблоковано", "blocked", "inactive", "0", "ні", "no"];

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserListItem>>> ListAsync(UserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.Users.View))
        {
            return Error.Forbidden();
        }

        var now = timeProvider.GetUtcNow();
        var users = Filter(db.Users.AsNoTracking(), query);
        users = query.SortBy switch
        {
            "email" => users.OrderByDirection(static u => u.Email, query.Descending),
            "lastSignIn" => users.OrderByDirection(static u => u.LastSignInAt, query.Descending),
            _ => query.Descending
                ? users.OrderByDescending(static u => u.LastName).ThenByDescending(static u => u.FirstName)
                : users.OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName),
        };

        return await users
            .Select(u => new UserListItem(
                u.Id,
                u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email!,
                u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title ?? p.Role!.Name).FirstOrDefault(),
                u.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault(),
                u.IsActive,
                u.TwoFactorEnabled,
                u.LockoutEnd != null && u.LockoutEnd > now,
                u.LastSignInAt))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<UserDetails>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.View))
        {
            return Error.Forbidden();
        }

        var user = await db.Users
            .AsNoTracking()
            .Include(static u => u.Positions).ThenInclude(static p => p.OrganizationUnit)
            .Include(static u => u.Positions).ThenInclude(static p => p.Role)
            .Include(static u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("Користувача не знайдено.");
        }

        var now = timeProvider.GetUtcNow();
        var sessions = await db.UserSessions.CountAsync(s => s.UserId == id && (s.ExpiresAt == null || s.ExpiresAt > now), cancellationToken);
        var effective = await permissions.GetEffectivePermissionsAsync(id, cancellationToken);

        return new UserDetails(
            user.Id,
            user.Email!,
            user.LastName,
            user.FirstName,
            user.MiddleName,
            user.FullName,
            user.PhoneNumber,
            user.IsActive,
            user.TwoFactorEnabled,
            user.MustChangePassword,
            user.LockoutEnd > now,
            user.CreatedAt,
            user.LastSignInAt,
            sessions,
            user.Positions
                .OrderByDescending(static p => p.IsPrimary)
                .Select(static p => new PositionDto(p.OrganizationUnitId, p.OrganizationUnit!.Name, p.RoleId, p.Role!.Name, p.Title, p.IsPrimary))
                .ToList(),
            [.. user.PermissionOverrides.Where(static o => o.IsGranted).Select(static o => o.Permission)],
            [.. user.PermissionOverrides.Where(static o => !o.IsGranted).Select(static o => o.Permission)],
            effective);
    }

    /// <inheritdoc />
    public async Task<Result<UserCredentials>> CreateAsync(SaveUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Users.Manage))
        {
            return Error.Forbidden();
        }

        var email = NormalizeEmail(command.Email);
        if (email is null)
        {
            return Error.Validation("Вкажіть коректну адресу електронної пошти.");
        }

        var normalizedEmail = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return Error.Conflict("Користувач із такою електронною поштою вже існує.");
        }

        var positionError = await ValidatePositionsAsync(command.Positions, cancellationToken);
        if (positionError is not null)
        {
            return positionError;
        }

        return await DomainErrors.GuardAsync<UserCredentials>(async () =>
        {
            var user = new User(email, command.LastName, command.FirstName, command.MiddleName);
            user.UpdatePersonalData(command.LastName, command.FirstName, command.MiddleName, command.PhoneNumber);
            user.SetPositions(command.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
            if (currentUser.Has(Permissions.Roles.Manage))
            {
                user.SetPermissionOverrides(command.GrantedPermissions, command.DeniedPermissions);
            }

            var password = identity.GenerateTemporaryPassword();
            var created = await identity.CreateAsync(user, password);
            if (!created.IsSuccess)
            {
                return created.Error;
            }

            await audit.WriteAsync(AuditActions.UserCreated, currentUser.Id, currentUser.DisplayName, $"Створено {user.Email} ({user.FullName})", cancellationToken);
            return new UserCredentials(user.Id, user.Email!, user.FullName, password);
        });
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid id, SaveUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!currentUser.Has(Permissions.Users.Manage))
        {
            return Error.Forbidden();
        }

        var user = await LoadForManagementAsync(id, cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error;
        }

        var email = NormalizeEmail(command.Email);
        if (email is null)
        {
            return Error.Validation("Вкажіть коректну адресу електронної пошти.");
        }

        var normalizedEmail = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != id, cancellationToken))
        {
            return Error.Conflict("Користувач із такою електронною поштою вже існує.");
        }

        var positionError = await ValidatePositionsAsync(command.Positions, cancellationToken);
        if (positionError is not null)
        {
            return positionError;
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            var entity = user.Value;
            entity.Email = email;
            entity.UserName = email;
            entity.UpdatePersonalData(command.LastName, command.FirstName, command.MiddleName, command.PhoneNumber);
            entity.SetPositions(command.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
            if (currentUser.Has(Permissions.Roles.Manage))
            {
                entity.SetPermissionOverrides(command.GrantedPermissions, command.DeniedPermissions);
            }

            audit.Add(AuditActions.UserUpdated, nameof(User), entity.Id.ToString(), $"Змінено {entity.Email}");
            return await identity.UpdateAsync(entity);
        });
    }

    /// <inheritdoc />
    public async Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Manage))
        {
            return Error.Forbidden();
        }

        if (id == currentUser.Id && !isActive)
        {
            return Error.Validation("Ви не можете заблокувати власний обліковий запис.");
        }

        var user = await LoadForManagementAsync(id, cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error;
        }

        user.Value.SetActive(isActive);
        audit.Add(AuditActions.UserStatusChanged, nameof(User), id.ToString(), isActive ? $"Розблоковано {user.Value.Email}" : $"Заблоковано {user.Value.Email}");
        var result = await identity.UpdateAsync(user.Value);
        if (result.IsSuccess && !isActive)
        {
            await identity.InvalidateSessionsAsync(user.Value);
            await db.UserSessions.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<UserCredentials>> ResetPasswordAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Security))
        {
            return Error.Forbidden();
        }

        var user = await LoadForManagementAsync(id, cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error;
        }

        var password = identity.GenerateTemporaryPassword();
        user.Value.RequirePasswordChange(true);
        var result = await identity.SetPasswordAsync(user.Value, password);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        await db.UserSessions.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
        await audit.WriteAsync(AuditActions.PasswordReset, currentUser.Id, currentUser.DisplayName, $"Скинуто пароль {user.Value.Email}", cancellationToken);
        return new UserCredentials(user.Value.Id, user.Value.Email!, user.Value.FullName, password);
    }

    /// <inheritdoc />
    public async Task<Result> ResetAuthenticatorAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Security))
        {
            return Error.Forbidden();
        }

        var user = await LoadForManagementAsync(id, cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error;
        }

        var result = await identity.ResetAuthenticatorAsync(user.Value);
        if (result.IsSuccess)
        {
            await db.UserSessions.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
            await audit.WriteAsync(AuditActions.AuthenticatorReset, currentUser.Id, currentUser.DisplayName, $"Скинуто автентифікатор {user.Value.Email}", cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result> RevokeSessionsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Security))
        {
            return Error.Forbidden();
        }

        var user = await LoadForManagementAsync(id, cancellationToken);
        if (!user.IsSuccess)
        {
            return user.Error;
        }

        await identity.InvalidateSessionsAsync(user.Value);
        var count = await db.UserSessions.Where(s => s.UserId == id).ExecuteDeleteAsync(cancellationToken);
        await identity.UnlockAsync(user.Value);
        await audit.WriteAsync(AuditActions.SessionRevoked, currentUser.Id, currentUser.DisplayName, $"Завершено {count} сесій {user.Value.Email}", cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<UserImportReport>> ImportAsync(FileUpload file, bool dryRun, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!currentUser.Has(Permissions.Users.Import))
        {
            return Error.Forbidden();
        }

        if (file.Length > FileRules.MaxImportSize)
        {
            return Error.Validation("Файл імпорту не може бути більшим за 5 МБ.");
        }

        var format = DetectFormat(file.FileName);
        if (format is null)
        {
            return Error.Validation("Підтримуються файли .xlsx та .csv.");
        }

        IReadOnlyList<UserSheetRow> rows;
        try
        {
            rows = spreadsheet.Read(file.Content, format.Value);
        }
        catch (InvalidDataException exception)
        {
            return Error.Validation(exception.Message);
        }

        if (rows.Count == 0)
        {
            return Error.Validation("Файл не містить жодного рядка з даними.");
        }

        var units = await db.OrganizationUnits.AsNoTracking().Select(static u => new { u.Id, u.Code }).ToDictionaryAsync(static u => u.Code, static u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var roles = await db.Roles.AsNoTracking().Include(static r => r.Permissions).ToDictionaryAsync(static r => r.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var errors = new List<ImportRowError>();
        var plans = new List<ImportPlan>();

        foreach (var group in rows.GroupBy(static r => r.Email.Trim().ToLowerInvariant()))
        {
            var first = group.First();
            var email = NormalizeEmail(first.Email);
            if (email is null)
            {
                errors.Add(new ImportRowError(first.RowNumber, first.Email, "Некоректна електронна пошта."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(first.LastName) || string.IsNullOrWhiteSpace(first.FirstName))
            {
                errors.Add(new ImportRowError(first.RowNumber, email, "Прізвище та ім'я є обов'язковими."));
                continue;
            }

            var positions = new List<PositionInput>();
            foreach (var row in group.Where(static r => !string.IsNullOrWhiteSpace(r.UnitCode) || !string.IsNullOrWhiteSpace(r.RoleCode)))
            {
                if (!units.TryGetValue(row.UnitCode?.Trim() ?? string.Empty, out var unitId))
                {
                    errors.Add(new ImportRowError(row.RowNumber, email, $"Підрозділ із кодом «{row.UnitCode}» не знайдено."));
                    continue;
                }

                if (!roles.TryGetValue(row.RoleCode?.Trim() ?? string.Empty, out var role))
                {
                    errors.Add(new ImportRowError(row.RowNumber, email, $"Роль із кодом «{row.RoleCode}» не знайдено."));
                    continue;
                }

                if (!CanAssign(role))
                {
                    errors.Add(new ImportRowError(row.RowNumber, email, $"Ви не можете призначати роль «{role.Name}»."));
                    continue;
                }

                positions.Add(new PositionInput(unitId, role.Id, row.PositionTitle, positions.Count == 0));
            }

            var isActive = !BlockedStatuses.Contains(first.Status?.Trim().ToLowerInvariant());
            plans.Add(new ImportPlan(first.RowNumber, email, first, positions, isActive));
        }

        var emails = plans.Select(static p => p.Email.ToUpperInvariant()).ToList();
        var existing = await db.Users
            .Include(static u => u.Positions)
            .Where(u => emails.Contains(u.NormalizedEmail!))
            .ToDictionaryAsync(static u => u.Email!, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var plan in plans)
        {
            if (!existing.TryGetValue(plan.Email, out var user))
            {
                continue;
            }

            if (user.Id == currentUser.Id)
            {
                if (!plan.IsActive)
                {
                    errors.Add(new ImportRowError(plan.Row, plan.Email, "Ви не можете заблокувати власний обліковий запис."));
                }

                continue;
            }

            // The same hierarchy rule as for manual editing: only accounts whose permissions the importer also has.
            var target = await permissions.GetEffectivePermissionsAsync(user.Id, cancellationToken);
            if (target.Any(p => !currentUser.Has(p)))
            {
                errors.Add(new ImportRowError(plan.Row, plan.Email, "Цей обліковий запис має більше прав, ніж ви, тому імпорт не може його змінювати."));
            }
        }

        var toCreate = plans.Count(p => !existing.ContainsKey(p.Email));
        var toUpdate = plans.Count - toCreate;
        if (errors.Count > 0 || dryRun)
        {
            return new UserImportReport(rows.Count, toCreate, toUpdate, Applied: false, [.. errors.OrderBy(static e => e.Row)], []);
        }

        try
        {
            var credentials = await db.InTransactionAsync<List<UserCredentials>>(
                async ct =>
                {
                    var issued = new List<UserCredentials>();
                    foreach (var plan in plans)
                    {
                        var data = plan.Data;
                        if (existing.TryGetValue(plan.Email, out var user))
                        {
                            user.UpdatePersonalData(data.LastName, data.FirstName, data.MiddleName, data.PhoneNumber ?? user.PhoneNumber);
                            if (plan.Positions.Count > 0)
                            {
                                user.SetPositions(plan.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
                            }

                            var blocked = user.IsActive && !plan.IsActive;
                            user.SetActive(plan.IsActive);
                            var updated = await identity.UpdateAsync(user);
                            if (!updated.IsSuccess)
                            {
                                throw new DomainException($"{plan.Email}: {updated.Error.Message}");
                            }

                            // A blocked account loses its sessions immediately, as with manual blocking.
                            if (blocked)
                            {
                                await identity.InvalidateSessionsAsync(user);
                                await db.UserSessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(ct);
                            }
                        }
                        else
                        {
                            var created = new User(plan.Email, data.LastName, data.FirstName, data.MiddleName);
                            created.UpdatePersonalData(data.LastName, data.FirstName, data.MiddleName, data.PhoneNumber);
                            created.SetPositions(plan.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)));
                            created.SetActive(plan.IsActive);
                            var password = identity.GenerateTemporaryPassword();
                            var result = await identity.CreateAsync(created, password);
                            if (!result.IsSuccess)
                            {
                                throw new DomainException($"{plan.Email}: {result.Error.Message}");
                            }

                            issued.Add(new UserCredentials(created.Id, created.Email!, created.FullName, password));
                        }
                    }

                    audit.Add(AuditActions.UsersImported, details: $"Імпорт «{file.FileName}»: створено {toCreate}, оновлено {toUpdate}");
                    await db.SaveChangesAsync(ct);
                    return issued;
                },
                cancellationToken);

            return new UserImportReport(rows.Count, toCreate, toUpdate, Applied: true, [], credentials);
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DownloadableFile>> ExportAsync(UserQuery query, SpreadsheetFormat format, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!currentUser.Has(Permissions.Users.Export))
        {
            return Error.Forbidden();
        }

        var users = await Filter(db.Users.AsNoTracking(), query)
            .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
            .Select(static u => new
            {
                u.Email,
                u.LastName,
                u.FirstName,
                u.MiddleName,
                u.PhoneNumber,
                u.IsActive,
                Positions = u.Positions
                    .OrderByDescending(static p => p.IsPrimary)
                    .Select(static p => new { UnitCode = p.OrganizationUnit!.Code, RoleCode = p.Role!.Code, p.Title })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var rows = users.SelectMany(u =>
        {
            var status = u.IsActive ? "активний" : "заблокований";
            return u.Positions.Count == 0
                ? [new UserSheetRow(0, u.Email!, u.LastName, u.FirstName, u.MiddleName, u.PhoneNumber, null, null, null, status)]
                : u.Positions.Select(p => new UserSheetRow(0, u.Email!, u.LastName, u.FirstName, u.MiddleName, u.PhoneNumber, p.UnitCode, p.RoleCode, p.Title, status));
        });

        var content = spreadsheet.Write(rows, format);
        audit.Add(AuditActions.UsersExported, details: $"Експортовано {users.Count} користувачів ({format})");
        await db.SaveChangesAsync(cancellationToken);
        return ToFile(content, $"users-{timeProvider.GetUtcNow():yyyyMMdd-HHmm}", format);
    }

    /// <inheritdoc />
    public Result<DownloadableFile> GetImportTemplate(SpreadsheetFormat format)
    {
        if (!currentUser.Has(Permissions.Users.Import))
        {
            return Error.Forbidden();
        }

        UserSheetRow[] sample =
        [
            new(0, "ivan.petrenko@example.com", "Петренко", "Іван", "Олегович", "+380501234567", "SALES", SystemRoles.HeadOfUnit, "Начальник відділу продажу", "активний"),
            new(0, "ivan.petrenko@example.com", "Петренко", "Іван", "Олегович", "+380501234567", "SALES", SystemRoles.Employee, "Менеджер з ключових клієнтів", "активний"),
            new(0, "olena.koval@example.com", "Коваль", "Олена", "Петрівна", null, "HR", SystemRoles.HrSpecialist, "Інспектор з кадрів", "активний"),
        ];

        return ToFile(spreadsheet.Write(sample, format), "users-import-template", format);
    }

    /// <inheritdoc />
    public DownloadableFile BuildCredentialsFile(IEnumerable<UserCredentials> credentials, SpreadsheetFormat format)
        => ToFile(spreadsheet.WriteCredentials(credentials, format), $"credentials-{timeProvider.GetUtcNow():yyyyMMdd-HHmm}", format);

    /// <summary>
    /// Detects the spreadsheet format by the file extension.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The format or <see langword="null"/>.</returns>
    private static SpreadsheetFormat? DetectFormat(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".xlsx" => SpreadsheetFormat.Xlsx,
        ".csv" => SpreadsheetFormat.Csv,
        _ => null,
    };

    /// <summary>
    /// Wraps spreadsheet bytes into a downloadable file.
    /// </summary>
    /// <param name="content">The bytes.</param>
    /// <param name="baseName">The file name without extension.</param>
    /// <param name="format">The format.</param>
    /// <returns>The file.</returns>
    private static DownloadableFile ToFile(byte[] content, string baseName, SpreadsheetFormat format) => format == SpreadsheetFormat.Csv
        ? new DownloadableFile(new MemoryStream(content), $"{baseName}.csv", "text/csv")
        : new DownloadableFile(new MemoryStream(content), $"{baseName}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    /// <summary>
    /// Validates and normalizes an e-mail address.
    /// </summary>
    /// <param name="email">The raw value.</param>
    /// <returns>The lower-case address or <see langword="null"/> when invalid.</returns>
    private static string? NormalizeEmail(string? email)
    {
        var value = email?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(value) && value.Length <= User.EmailMaxLength && MailAddress.TryCreate(value, out var address) && address.Address == value
            ? value
            : null;
    }

    /// <summary>
    /// Applies the grid filters.
    /// </summary>
    /// <param name="users">The query.</param>
    /// <param name="query">The filters.</param>
    /// <returns>The filtered query.</returns>
    private static IQueryable<User> Filter(IQueryable<User> users, UserQuery query)
    {
        users = query.Status switch
        {
            UserStatusFilter.Active => users.Where(static u => u.IsActive),
            UserStatusFilter.Blocked => users.Where(static u => !u.IsActive),
            _ => users,
        };

        if (query.UnitId is { } unitId)
        {
            users = users.Where(u => u.Positions.Any(p => p.OrganizationUnitId == unitId));
        }

        if (query.RoleId is { } roleId)
        {
            users = users.Where(u => u.Positions.Any(p => p.RoleId == roleId));
        }

        if (query.Search is { } search)
        {
            var term = search;
            users = users.Where(u =>
                u.LastName.Contains(term)
                || u.FirstName.Contains(term)
                || (u.MiddleName != null && u.MiddleName.Contains(term))
                || u.Email!.Contains(term));
        }

        return users;
    }

    /// <summary>
    /// Determines whether the current user may assign the role (prevents privilege escalation).
    /// </summary>
    /// <param name="role">The role.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    private bool CanAssign(Role role)
        => currentUser.Has(Permissions.Roles.Manage)
            || (!role.IsAdministrator && role.Permissions.All(p => currentUser.Has(p.Permission)));

    /// <summary>
    /// Validates positions: units and roles exist and the roles may be assigned by the current user.
    /// </summary>
    /// <param name="positions">The positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    private async Task<Error?> ValidatePositionsAsync(IReadOnlyList<PositionInput> positions, CancellationToken cancellationToken)
    {
        var unitIds = positions.Select(static p => p.UnitId).Distinct().ToList();
        var roleIds = positions.Select(static p => p.RoleId).Distinct().ToList();

        if (await db.OrganizationUnits.CountAsync(u => unitIds.Contains(u.Id), cancellationToken) != unitIds.Count)
        {
            return Error.Validation("Обраний підрозділ не знайдено.");
        }

        var roles = await db.Roles.AsNoTracking().Include(static r => r.Permissions).Where(r => roleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Count)
        {
            return Error.Validation("Обрану роль не знайдено.");
        }

        var forbidden = roles.Find(r => !CanAssign(r));
        return forbidden is null
            ? null
            : Error.Forbidden($"Ви не можете призначати роль «{forbidden.Name}», оскільки вона надає більше прав, ніж маєте ви.");
    }

    /// <summary>
    /// Loads a user for modification and checks that the current user's permissions cover the target's permissions.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked user.</returns>
    private async Task<Result<User>> LoadForManagementAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(static u => u.Positions)
            .Include(static u => u.PermissionOverrides)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("Користувача не знайдено.");
        }

        if (id != currentUser.Id)
        {
            var target = await permissions.GetEffectivePermissionsAsync(id, cancellationToken);
            if (target.Any(p => !currentUser.Has(p)))
            {
                return Error.Forbidden("Цей обліковий запис має більше прав, ніж ви, тому ви не можете ним керувати.");
            }
        }

        return user;
    }

    /// <summary>
    /// A validated import group for a single e-mail.
    /// </summary>
    /// <param name="Row">The first row number.</param>
    /// <param name="Email">The normalized e-mail.</param>
    /// <param name="Data">The first row with the personal data.</param>
    /// <param name="Positions">The positions.</param>
    /// <param name="IsActive">The account state.</param>
    private sealed record ImportPlan(int Row, string Email, UserSheetRow Data, List<PositionInput> Positions, bool IsActive);
}
