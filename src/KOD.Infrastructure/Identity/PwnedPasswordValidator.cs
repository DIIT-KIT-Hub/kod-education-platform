using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Rejects passwords that appear in known data breaches (OWASP ASVS 2.1.7), using the k-anonymity range API of
/// Have I Been Pwned: only the first five characters of the password's SHA-1 hash leave the server, and responses are
/// padded so the request does not reveal the result. When the service is unreachable the check is skipped (logged),
/// so an outage does not block password changes.
/// </summary>
/// <param name="clientFactory">The HTTP client factory.</param>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class PwnedPasswordValidator(
    IHttpClientFactory clientFactory,
    IOptions<PasswordSecurityOptions> options,
    ILogger<PwnedPasswordValidator> logger) : IPasswordValidator<User>
{
    /// <summary>
    /// The named HTTP client.
    /// </summary>
    public const string HttpClientName = "pwned-passwords";

    /// <inheritdoc />
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "The range API is keyed by SHA-1 prefixes; the hash is a lookup key, not a security control.")]
    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        var settings = options.Value;
        if (!settings.CheckBreachedPasswords || string.IsNullOrEmpty(password))
        {
            return IdentityResult.Success;
        }

        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var (prefix, suffix) = (hash[..5], hash[5..]);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(settings.BreachedPasswordsApi, prefix));
            request.Headers.Add("Add-Padding", "true");
            using var response = await clientFactory.CreateClient(HttpClientName).SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var reader = new StringReader(await response.Content.ReadAsStringAsync());
            while (await reader.ReadLineAsync() is { } line)
            {
                var separator = line.IndexOf(':', StringComparison.Ordinal);
                if (separator == suffix.Length
                    && line.AsSpan(0, separator).Equals(suffix, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line.AsSpan(separator + 1), out var count)
                    && count > 0)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "PasswordBreached",
                        Description = Texts.T("Цей пароль трапляється у відомих витоках даних. Оберіть інший."),
                    });
                }
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            LogUnavailable(logger, exception);
        }

        return IdentityResult.Success;
    }

    /// <summary>
    /// Logs a skipped check.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Breached password check skipped: the service is unavailable")]
    private static partial void LogUnavailable(ILogger logger, Exception exception);
}
