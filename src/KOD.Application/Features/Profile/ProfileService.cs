using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Profile;

/// <summary>
/// Default <see cref="IProfileService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="sessionContext">The current session accessor.</param>
/// <param name="identity">The identity service.</param>
/// <param name="permissions">The permission resolver.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class ProfileService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ISessionContext sessionContext,
    IIdentityService identity,
    IPermissionResolver permissions,
    IAuditLogger audit,
    TimeProvider timeProvider) : IProfileService
{
    /// <inheritdoc />
    public async Task<Result<ProfileDto>> GetAsync(CancellationToken cancellationToken)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized();
        }

        var effective = await permissions.GetEffectivePermissionsAsync(user.Id, cancellationToken);
        var definitions = Permissions.All.Where(p => effective.Contains(p.Code)).ToList();
        return new ProfileDto(
            user.Id,
            user.FullName,
            user.Email!,
            user.PhoneNumber,
            user.Positions
                .OrderByDescending(static p => p.IsPrimary)
                .Select(static p => new PositionDto(p.OrganizationUnitId, p.OrganizationUnit!.Name, p.RoleId, p.Role!.Name, p.Title, p.IsPrimary))
                .ToList(),
            [.. definitions.Select(static d => d.Module).Distinct().Order()],
            definitions,
            user.TwoFactorEnabled,
            await identity.CountRecoveryCodesAsync(user),
            user.CreatedAt,
            user.LastSignInAt);
    }

    /// <inheritdoc />
    public async Task<Result> UpdatePhoneAsync(string? phoneNumber, CancellationToken cancellationToken)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized();
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            user.UpdatePersonalData(user.LastName, user.FirstName, user.MiddleName, phoneNumber);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized();
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            return Error.Validation("Новий пароль має відрізнятися від поточного.");
        }

        user.RequirePasswordChange(false);
        var result = await identity.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.IsSuccess)
        {
            // Other sessions must sign in again with the new password.
            var current = sessionContext.CurrentSessionId;
            await db.UserSessions.Where(s => s.UserId == user.Id && s.Id != current).ExecuteDeleteAsync(cancellationToken);
            await audit.WriteAsync(AuditActions.PasswordChanged, user.Id, user.FullName, null, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(string authenticatorCode, CancellationToken cancellationToken)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized();
        }

        if (!user.TwoFactorEnabled)
        {
            return Error.Validation("Спочатку підключіть застосунок-автентифікатор.");
        }

        if (!await identity.VerifyAuthenticatorCodeAsync(user, authenticatorCode))
        {
            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний код підтвердження (нові резервні коди)", cancellationToken);
            return Error.Validation("Код неправильний або вже використаний. Введіть наступний код із застосунку.");
        }

        var codes = await identity.GenerateRecoveryCodesAsync(user);
        await audit.WriteAsync(AuditActions.RecoveryCodesGenerated, user.Id, user.FullName, null, cancellationToken);
        return Result<IReadOnlyList<string>>.Success(codes);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SessionDto>>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var now = timeProvider.GetUtcNow();
        var current = sessionContext.CurrentSessionId;
        var sessions = await db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == me && (s.ExpiresAt == null || s.ExpiresAt > now))
            .OrderByDescending(static s => s.LastSeenAt)
            .Select(s => new SessionDto(s.Id, s.CreatedAt, s.LastSeenAt, s.ExpiresAt, s.IpAddress, s.UserAgent, s.Id == current))
            .ToListAsync(cancellationToken);

        return sessions;
    }

    /// <inheritdoc />
    public async Task<Result> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var deleted = await db.UserSessions.Where(s => s.Id == sessionId && s.UserId == me).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return Error.NotFound("Сесію не знайдено.");
        }

        await audit.WriteAsync(AuditActions.SessionRevoked, me, currentUser.DisplayName, "Завершено одну сесію", cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<int>> RevokeOtherSessionsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        // Without a known current session the filter below would also end the session of this request.
        if (sessionContext.CurrentSessionId is not { } current)
        {
            return Error.Validation("Не вдалося визначити поточну сесію. Увійдіть знову та повторіть дію.");
        }

        var deleted = await db.UserSessions.Where(s => s.UserId == me && s.Id != current).ExecuteDeleteAsync(cancellationToken);
        await audit.WriteAsync(AuditActions.SessionRevoked, me, currentUser.DisplayName, $"Завершено {deleted} інших сесій", cancellationToken);
        return deleted;
    }

    /// <summary>
    /// Loads the tracked current user with positions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user or <see langword="null"/>.</returns>
    private async Task<User?> LoadAsync(CancellationToken cancellationToken)
        => currentUser.Id is { } me
            ? await db.Users
                .Include(static u => u.Positions).ThenInclude(static p => p.OrganizationUnit)
                .Include(static u => u.Positions).ThenInclude(static p => p.Role)
                .FirstOrDefaultAsync(u => u.Id == me, cancellationToken)
            : null;
}
