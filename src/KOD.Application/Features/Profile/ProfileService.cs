using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Application.Features.Media;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Media;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Profile;

/// <summary>
/// Default <see cref="IProfileService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="identity">The identity service.</param>
/// <param name="permissions">The permission resolver.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="media">The image library.</param>
/// <param name="accessScope">The data access rules of the current user.</param>
internal sealed class ProfileService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    IPermissionResolver permissions,
    IAuditLogger audit,
    MediaLibrary media,
    IAccessScope accessScope) : IProfileService
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
            user.LastName,
            user.FirstName,
            user.MiddleName,
            user.Email!,
            user.PhoneNumber,
            user.Positions
                .OrderByDescending(static p => p.IsPrimary)
                .Select(static p => new PositionDto(p.OrganizationUnitId, p.OrganizationUnit!.Name, p.RoleId, StoredText.Display(p.Role!.Name), p.Title, p.IsPrimary))
                .ToList(),
            [.. definitions.Select(static d => d.Module).Distinct().Order()],
            definitions,
            user.TwoFactorEnabled,
            await identity.CountRecoveryCodesAsync(user),
            user.CreatedAt,
            user.LastSignInAt,
            user.Language,
            user.PhotoId,
            await CanEditPersonalDataAsync(user, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(ProfileUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        var user = await LoadAsync(cancellationToken);
        if (user is null)
        {
            return Error.Unauthorized();
        }

        var lastName = update.LastName ?? user.LastName;
        var firstName = update.FirstName ?? user.FirstName;
        var middleName = update.MiddleName ?? user.MiddleName;
        var email = update.Email is null ? user.Email! : EmailAddress.Normalize(update.Email);
        if (email is null)
        {
            return Error.Validation(Texts.T("Вкажіть коректну адресу електронної пошти."));
        }

        var personalDataChanged = !string.Equals(lastName.Trim(), user.LastName, StringComparison.Ordinal)
            || !string.Equals(firstName.Trim(), user.FirstName, StringComparison.Ordinal)
            || !string.Equals(string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(), user.MiddleName, StringComparison.Ordinal)
            || !string.Equals(email, user.Email, StringComparison.Ordinal);
        if (!personalDataChanged)
        {
            return await DomainErrors.GuardAsync(async () =>
            {
                user.UpdatePersonalData(user.LastName, user.FirstName, user.MiddleName, update.PhoneNumber);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }

        if (!await CanEditPersonalDataAsync(user, cancellationToken))
        {
            return Error.Forbidden(Texts.T("ПІБ та електронну пошту змінює адміністратор Вашої організації або відділ кадрів."));
        }

        var normalizedEmail = email.ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != user.Id, cancellationToken))
        {
            return Error.Conflict(Texts.T("Користувач із такою електронною поштою вже існує."));
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            user.Email = email;
            user.UserName = email;
            user.UpdatePersonalData(lastName, firstName, middleName, update.PhoneNumber);
            audit.Add(AuditActions.UserUpdated, nameof(User), user.Id.ToString(), $"Змінено {user.Email} (профіль)");
            return await identity.UpdateAsync(user);
        });
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> SetPhotoAsync(FileUpload file, ImageCrop? crop, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var user = currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;
        if (user is null)
        {
            return Error.Unauthorized();
        }

        var added = await media.AddAsync(file, MediaPurpose.Avatar, user.Id, crop, cancellationToken);
        if (!added.IsSuccess)
        {
            return added.Error;
        }

        var previous = user.PhotoId is { } old ? await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == old, cancellationToken) : null;
        user.SetPhoto(added.Value.Id);
        if (previous is not null)
        {
            media.Remove(previous);
        }

        audit.Add(AuditActions.ProfilePhotoChanged, nameof(User), user.Id.ToString(), null);
        await media.SaveChangesAsync(cancellationToken);
        return added.Value.Id;
    }

    /// <inheritdoc />
    public async Task<Result> SetLanguageAsync(string language, CancellationToken cancellationToken)
    {
        var user = currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;
        if (user is null)
        {
            return Error.Unauthorized();
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            user.SetLanguage(language);
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
            return Error.Validation(Texts.T("Новий пароль має відрізнятися від поточного."));
        }

        user.RequirePasswordChange(false);
        var result = await identity.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.IsSuccess)
        {
            // The new security stamp ends the other sign-ins; the caller re-issues the tokens of this browser.
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
            return Error.Validation(Texts.T("Спочатку підключіть застосунок-автентифікатор."));
        }

        if (!await identity.VerifyAuthenticatorCodeAsync(user, authenticatorCode))
        {
            await audit.WriteAsync(AuditActions.SignInFailed, user.Id, user.FullName, "Неправильний код підтвердження (нові резервні коди)", cancellationToken);
            return Error.Validation(Texts.T("Код неправильний або вже використаний. Введіть наступний код із застосунку."));
        }

        var codes = await identity.GenerateRecoveryCodesAsync(user);
        await audit.WriteAsync(AuditActions.RecoveryCodesGenerated, user.Id, user.FullName, null, cancellationToken);
        return Result<IReadOnlyList<string>>.Success(codes);
    }

    /// <inheritdoc />
    public async Task<Result> SignOutEverywhereAsync(CancellationToken cancellationToken)
    {
        var user = currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;
        if (user is null)
        {
            return Error.Unauthorized();
        }

        await identity.InvalidateSessionsAsync(user);
        await audit.WriteAsync(AuditActions.SessionRevoked, user.Id, user.FullName, "Вихід на всіх інших пристроях", cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Determines whether the user may change own name and e-mail: like in the user administration, only an administrator
    /// of users whose scope covers every position of the account (a system administrator always).
    /// </summary>
    /// <param name="user">The current user with positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    private async Task<bool> CanEditPersonalDataAsync(User user, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Manage))
        {
            return false;
        }

        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
        return scope.Covers([.. user.Positions.Select(static p => p.OrganizationUnitId)]);
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
