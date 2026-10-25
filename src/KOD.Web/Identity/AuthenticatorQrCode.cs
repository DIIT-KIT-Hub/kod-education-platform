using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using QRCoder;

namespace KOD.Web.Identity;

/// <summary>
/// Builds the <c>otpauth://</c> URI and its QR code for authenticator apps (Google Authenticator, Microsoft Authenticator, 1Password…).
/// </summary>
internal static class AuthenticatorQrCode
{
    /// <summary>
    /// The issuer shown in authenticator apps.
    /// </summary>
    private const string Issuer = "КОД";

    /// <summary>
    /// Builds the provisioning URI (RFC 6238 key URI format).
    /// </summary>
    /// <param name="email">The account name.</param>
    /// <param name="key">The Base32 shared key.</param>
    /// <returns>The URI.</returns>
    public static string BuildUri(string email, string key)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"otpauth://totp/{UrlEncoder.Default.Encode(Issuer)}:{UrlEncoder.Default.Encode(email)}?secret={key}&issuer={UrlEncoder.Default.Encode(Issuer)}&digits=6&period=30");

    /// <summary>
    /// Renders the URI as an inline SVG QR code (no external services involved).
    /// </summary>
    /// <param name="uri">The provisioning URI.</param>
    /// <returns>The SVG markup.</returns>
    public static string RenderSvg(string uri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
        var svg = new SvgQRCode(data).GetGraphic(6, "#000000", "#ffffff", drawQuietZones: true);
        return svg;
    }

    /// <summary>
    /// Formats the shared key in groups of four characters for manual entry.
    /// </summary>
    /// <param name="key">The unformatted key.</param>
    /// <returns>The formatted key.</returns>
    public static string FormatKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var builder = new StringBuilder(key.Length + (key.Length / 4));
        for (var i = 0; i < key.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToLowerInvariant(key[i]));
        }

        return builder.ToString();
    }
}
