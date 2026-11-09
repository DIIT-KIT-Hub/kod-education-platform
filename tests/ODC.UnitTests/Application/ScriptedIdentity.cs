using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application;

/// <summary>
/// An identity service whose answers the test sets and whose calls it records.
/// </summary>
/// <param name="db">The database (creating and updating users saves it).</param>
internal sealed class ScriptedIdentity(MasterDbContext db) : IIdentityService
{
    /// <summary>Gets or sets the authenticator code accepted by <see cref="VerifyAuthenticatorCodeAsync"/>.</summary>
    public string ValidCode { get; set; } = "123456";

    /// <summary>Gets or sets the result of the identity operations (validators, password rules).</summary>
    public Result Outcome { get; set; } = Result.Success();

    /// <summary>Gets or sets the number of recovery codes left.</summary>
    public int RecoveryCodesLeft { get; set; } = 7;

    /// <summary>Gets the calls in order (method name and user e-mail).</summary>
    public List<string> Calls { get; } = [];

    /// <inheritdoc />
    public async Task<Result> CreateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"Create {user.Email}");
        if (!Outcome.IsSuccess)
        {
            return Outcome;
        }

        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Outcome;
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"Update {user.Email}");
        if (Outcome.IsSuccess)
        {
            user.NormalizedEmail = user.Email!.ToUpperInvariant();
            await db.SaveChangesAsync();
        }

        return Outcome;
    }

    /// <inheritdoc />
    public Task<Result> RemovePasswordAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"RemovePassword {user.Email}");
        user.PasswordHash = null;
        return Task.FromResult(Outcome);
    }

    /// <inheritdoc />
    public Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"ChangePassword {user.Email}");
        return Task.FromResult(Outcome);
    }

    /// <inheritdoc />
    public Task<Result> ResetAuthenticatorAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"ResetAuthenticator {user.Email}");
        return Task.FromResult(Outcome);
    }

    /// <inheritdoc />
    public Task InvalidateSessionsAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"InvalidateSessions {user.Email}");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> VerifyAuthenticatorCodeAsync(User user, string code)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"Verify {user.Email}");
        return Task.FromResult(code == ValidCode);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"GenerateRecoveryCodes {user.Email}");
        return Task.FromResult<IReadOnlyList<string>>(["AAAAA-11111", "BBBBB-22222"]);
    }

    /// <inheritdoc />
    public Task<int> CountRecoveryCodesAsync(User user) => Task.FromResult(RecoveryCodesLeft);

    /// <inheritdoc />
    public Task UnlockAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Calls.Add($"Unlock {user.Email}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers this service (and replaces the default one).
    /// </summary>
    /// <param name="services">The services.</param>
    public void Register(IServiceCollection services) => services.AddSingleton<IIdentityService>(this);
}
