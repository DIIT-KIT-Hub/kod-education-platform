using System.Security.Cryptography;
using KOD.Application.Abstractions;
using KOD.Application.Common.Results;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// <see cref="IIdentityService"/> backed by <see cref="UserManager{TUser}"/>.
/// </summary>
/// <param name="userManager">The user manager.</param>
internal sealed class IdentityService(UserManager<User> userManager) : IIdentityService
{
    /// <summary>
    /// Length of generated temporary passwords.
    /// </summary>
    private const int TemporaryPasswordLength = 16;

    /// <summary>
    /// Characters of temporary passwords without look-alikes (0/O, 1/l/I).
    /// </summary>
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Upper-case letters of temporary passwords.
    /// </summary>
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>
    /// Digits of temporary passwords.
    /// </summary>
    private const string Digits = "23456789";

    /// <summary>
    /// Symbols of temporary passwords.
    /// </summary>
    private const string Symbols = "!@#$%*-_+=?";

    /// <inheritdoc />
    public async Task<Result> CreateAsync(User user, string password)
        => ToResult(await userManager.CreateAsync(user, password));

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(User user)
        => ToResult(await userManager.UpdateAsync(user));

    /// <inheritdoc />
    public async Task<Result> SetPasswordAsync(User user, string password)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return ToResult(await userManager.ResetPasswordAsync(user, token, password));
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

    /// <inheritdoc />
    public string GenerateTemporaryPassword()
    {
        const string all = Lower + Upper + Digits + Symbols;
        Span<char> password = stackalloc char[TemporaryPasswordLength];
        password[0] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        password[1] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        password[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];
        for (var i = 4; i < password.Length; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        RandomNumberGenerator.Shuffle(password);
        return new string(password);
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
