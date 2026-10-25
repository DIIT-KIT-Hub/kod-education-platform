namespace KOD.Infrastructure.Identity;

/// <summary>
/// Claim types issued by the cabinet in addition to the standard Identity claims.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>An effective permission code (one claim per permission).</summary>
    public const string Permission = "kod:permission";

    /// <summary>The full name of the user.</summary>
    public const string FullName = "kod:full_name";

    /// <summary>The short name (with initials) of the user.</summary>
    public const string ShortName = "kod:short_name";

    /// <summary>Set when the user must replace a temporary password.</summary>
    public const string MustChangePassword = "kod:must_change_password";

    /// <summary>The identifier of the server-side session.</summary>
    public const string SessionId = "kod:sid";

    /// <summary>The authentication methods of the sign-in (RFC 8176, e.g. <c>mfa</c>).</summary>
    public const string AuthenticationMethod = "amr";
}
