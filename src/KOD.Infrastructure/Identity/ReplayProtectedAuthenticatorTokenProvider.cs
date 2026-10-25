using System.Globalization;
using System.Security.Cryptography;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// The authenticator (TOTP) token provider. Unlike the default Identity provider it accepts a code only once: the last
/// accepted time step is stored per user and older or equal steps are rejected, so an intercepted code cannot be replayed
/// (OWASP ASVS 2.8.4). The tolerated clock drift is one step (±30 seconds) instead of two.
/// </summary>
/// <param name="timeProvider">The clock.</param>
internal sealed class ReplayProtectedAuthenticatorTokenProvider(TimeProvider timeProvider) : IUserTwoFactorTokenProvider<User>
{
    /// <summary>
    /// The token store provider of the replay marker.
    /// </summary>
    internal const string MarkerProvider = "[KOD]";

    /// <summary>
    /// The token name of the replay marker (the last accepted time step).
    /// </summary>
    internal const string MarkerName = "AuthenticatorLastStep";

    /// <summary>
    /// The tolerated clock drift in steps.
    /// </summary>
    private const int Drift = 1;

    /// <inheritdoc />
    public async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return !string.IsNullOrWhiteSpace(await manager.GetAuthenticatorKeyAsync(user));
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) => Task.FromResult(string.Empty);

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (token is not { Length: 6 } || !int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var code))
        {
            return false;
        }

        var key = await manager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key) || Totp.DecodeBase32(key) is not { } secret)
        {
            return false;
        }

        var lastStep = long.TryParse(await manager.GetAuthenticationTokenAsync(user, MarkerProvider, MarkerName), NumberStyles.None, CultureInfo.InvariantCulture, out var stored)
            ? stored
            : long.MinValue;

        var now = Totp.TimeStep(timeProvider.GetUtcNow());
        for (var step = now - Drift; step <= now + Drift; step++)
        {
            if (step <= lastStep || !Matches(Totp.Compute(secret, step), code))
            {
                continue;
            }

            // Saving the marker uses the concurrency stamp: of two parallel requests with the same code only one succeeds.
            var saved = await manager.SetAuthenticationTokenAsync(user, MarkerProvider, MarkerName, step.ToString(CultureInfo.InvariantCulture));
            return saved.Succeeded;
        }

        return false;
    }

    /// <summary>
    /// Compares two codes in constant time.
    /// </summary>
    /// <param name="expected">The expected code.</param>
    /// <param name="actual">The submitted code.</param>
    /// <returns>The result.</returns>
    private static bool Matches(int expected, int actual)
    {
        Span<byte> left = stackalloc byte[sizeof(int)];
        Span<byte> right = stackalloc byte[sizeof(int)];
        BitConverter.TryWriteBytes(left, expected);
        BitConverter.TryWriteBytes(right, actual);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
