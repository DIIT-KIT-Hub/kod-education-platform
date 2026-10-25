using Microsoft.AspNetCore.DataProtection;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Encrypts the secret user tokens of Identity (the authenticator key and the recovery codes) with Data Protection.
/// Every value is bound to its user and token name, so a ciphertext copied to another account cannot be decrypted.
/// Values written before encryption was introduced (no prefix) are still readable and are encrypted at start-up.
/// </summary>
/// <param name="dataProtection">The data protection provider.</param>
public sealed class UserTokenProtector(IDataProtectionProvider dataProtection)
{
    /// <summary>
    /// The login provider Identity uses for its own tokens.
    /// </summary>
    public const string IdentityProvider = "[AspNetUserStore]";

    /// <summary>
    /// The prefix of encrypted values.
    /// </summary>
    private const string Prefix = "dp1:";

    /// <summary>
    /// The token names that hold secrets.
    /// </summary>
    private static readonly string[] SecretNames = ["AuthenticatorKey", "RecoveryCodes"];

    /// <summary>
    /// The root protector.
    /// </summary>
    private readonly IDataProtector _protector = dataProtection.CreateProtector("KOD.Identity.UserTokens.v1");

    /// <summary>
    /// Gets the names of the secret tokens.
    /// </summary>
    public static IReadOnlyList<string> Secrets => SecretNames;

    /// <summary>
    /// Determines whether a token holds a secret.
    /// </summary>
    /// <param name="loginProvider">The token provider.</param>
    /// <param name="name">The token name.</param>
    /// <returns>The result.</returns>
    public static bool IsSecret(string loginProvider, string name)
        => loginProvider == IdentityProvider && SecretNames.Contains(name, StringComparer.Ordinal);

    /// <summary>
    /// Determines whether a stored value is already encrypted.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The result.</returns>
    public static bool IsProtected(string? value) => value?.StartsWith(Prefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// Encrypts a secret token value.
    /// </summary>
    /// <param name="userId">The owner.</param>
    /// <param name="loginProvider">The token provider.</param>
    /// <param name="name">The token name.</param>
    /// <param name="value">The plain value.</param>
    /// <returns>The value to store.</returns>
    public string? Protect(Guid userId, string loginProvider, string name, string? value)
        => value is null || !IsSecret(loginProvider, name) || IsProtected(value)
            ? value
            : Prefix + For(userId, name).Protect(value);

    /// <summary>
    /// Decrypts a stored secret token value.
    /// </summary>
    /// <param name="userId">The owner.</param>
    /// <param name="loginProvider">The token provider.</param>
    /// <param name="name">The token name.</param>
    /// <param name="value">The stored value.</param>
    /// <returns>The plain value.</returns>
    public string? Unprotect(Guid userId, string loginProvider, string name, string? value)
        => IsSecret(loginProvider, name) && IsProtected(value)
            ? For(userId, name).Unprotect(value![Prefix.Length..])
            : value;

    /// <summary>
    /// Gets the protector bound to a user and token.
    /// </summary>
    /// <param name="userId">The owner.</param>
    /// <param name="name">The token name.</param>
    /// <returns>The protector.</returns>
    private IDataProtector For(Guid userId, string name) => _protector.CreateProtector(userId.ToString("N"), name);
}
