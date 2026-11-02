using System.Net.Mail;
using KOD.Domain.Users;

namespace KOD.Application.Common.Text;

/// <summary>
/// E-mail addresses used as logins.
/// </summary>
internal static class EmailAddress
{
    /// <summary>
    /// Validates and normalizes an e-mail address.
    /// </summary>
    /// <param name="email">The raw value.</param>
    /// <returns>The lower-case address or <see langword="null"/> when invalid.</returns>
    public static string? Normalize(string? email)
    {
        var value = email?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(value) && value.Length <= User.EmailMaxLength && MailAddress.TryCreate(value, out var address) && address.Address == value
            ? value
            : null;
    }
}
