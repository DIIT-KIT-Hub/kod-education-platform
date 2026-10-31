namespace KOD.Web.Api.Contracts;

/// <summary>
/// The key of a new authenticator.
/// </summary>
/// <param name="SharedKey">The Base32 key, for copying.</param>
/// <param name="FormattedKey">The key in groups of four, for typing.</param>
/// <param name="QrCodeSvg">The <c>otpauth://</c> URI as an SVG QR code (generated on the server, no external service).</param>
public sealed record AuthenticatorSetupResponse(string SharedKey, string FormattedKey, string QrCodeSvg);
