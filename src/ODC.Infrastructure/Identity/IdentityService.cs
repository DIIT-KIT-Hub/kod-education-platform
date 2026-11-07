using Microsoft.AspNetCore.Identity;
using ODC.Application.Abstractions;
using ODC.Application.Common.Results;
using ODC.Domain.Localization;
using ODC.Domain.Users;

namespace ODC.Infrastructure.Identity;

/// <summary>
/// <see cref="IIdentityService"/> backed by <see cref="UserManager{TUser}"/>.
/// </summary>
/// <param name="userManager">The user manager.</param>
internal sealed class IdentityService(UserManager<User> userManager) : IIdentityService
{
    /// <inheritdoc />
    public async Task<Result> CreateAsync(User user)
        => ToResult(await userManager.CreateAsync(user));

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(User user)
        => ToResult(await userManager.UpdateAsync(user));

    /// <inheritdoc />
    public async Task<Result> RemovePasswordAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.AwaitsActivation)
        {
            // Already waiting: only end the sign-ins.
            return ToResult(await userManager.UpdateSecurityStampAsync(user));
        }

        // RemovePasswordAsync also changes the security stamp, so every refresh token family of the user stops working.
        return ToResult(await userManager.RemovePasswordAsync(user));
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        // Wrong current passwords count toward the lockout, so a stolen session cannot be used to guess the password.
        if (await userManager.IsLockedOutAsync(user))
        {
            return Error.Validation(Texts.T("Забагато невдалих спроб. Спробуйте пізніше."), "identity");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Errors.Any(static e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            await userManager.AccessFailedAsync(user);
        }

        return ToResult(result);
    }

    /// <inheritdoc />
    public async Task<Result> ResetAuthenticatorAsync(User user)
    {
        var disabled = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disabled.Succeeded)
        {
            return ToResult(disabled);
        }

        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        return ToResult(await userManager.ResetAuthenticatorKeyAsync(user));
    }

    /// <inheritdoc />
    public Task InvalidateSessionsAsync(User user) => userManager.UpdateSecurityStampAsync(user);

    /// <inheritdoc />
    public async Task<bool> VerifyAuthenticatorCodeAsync(User user, string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        // Failed confirmations count toward the lockout like failed sign-ins, so a stolen session cannot guess codes.
        if (await userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        var normalized = code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, normalized))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return true;
        }

        await userManager.AccessFailedAsync(user);
        return false;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(User user)
        => [.. await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? []];

    /// <inheritdoc />
    public Task<int> CountRecoveryCodesAsync(User user) => userManager.CountRecoveryCodesAsync(user);

    /// <inheritdoc />
    public async Task UnlockAsync(User user)
    {
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
    }

    /// <summary>
    /// Converts an Identity result into an application result.
    /// </summary>
    /// <param name="result">The Identity result.</param>
    /// <returns>The application result.</returns>
    private static Result ToResult(IdentityResult result)
        => result.Succeeded
            ? Result.Success()
            : Error.Validation(string.Join(" ", result.Errors.Select(static e => e.Description)), "identity");
}
