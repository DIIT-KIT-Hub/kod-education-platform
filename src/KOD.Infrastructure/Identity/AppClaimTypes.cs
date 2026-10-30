using KOD.Domain.Access;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Claim types of the cabinet principal and of its access tokens: registered JWT claim names (RFC 7519, OpenID Connect)
/// for the standard ones, <c>kod:</c> names for the cabinet-specific ones.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>The user identifier (<c>sub</c>); also the Identity user id claim.</summary>
    public const string Subject = "sub";

    /// <summary>The login (e-mail) of the user; also the Identity user name claim.</summary>
    public const string Name = "name";

    /// <summary>The e-mail address.</summary>
    public const string Email = "email";

    /// <summary>A role claim (not issued: access is decided by permissions).</summary>
    public const string Role = "role";

    /// <summary>An effective permission code (one claim per permission, a JSON array in the token).</summary>
    public const string Permission = PermissionClaimTypes.Permission;

    /// <summary>The full name of the user.</summary>
    public const string FullName = "kod:full_name";

    /// <summary>The short name (with initials) of the user.</summary>
    public const string ShortName = "kod:short_name";

    /// <summary>Set when the user must replace a temporary password.</summary>
    public const string MustChangePassword = "kod:must_change_password";

    /// <summary>The chosen interface language (<c>uk</c>, <c>en</c>); absent when the user has not chosen one.</summary>
    public const string Language = "kod:lang";

    /// <summary>The profile photo (a media identifier); absent until the user uploads one.</summary>
    public const string Photo = "kod:photo";

    /// <summary>The authentication methods of the sign-in (RFC 8176, e.g. <c>mfa</c>).</summary>
    public const string AuthenticationMethod = "amr";

    /// <summary>When the user signed in (<c>auth_time</c>, OpenID Connect; seconds since the epoch).</summary>
    public const string AuthenticationTime = "auth_time";

    /// <summary>Set when the sign-in survives closing the browser ("remember me").</summary>
    public const string Persistent = "kod:persistent";

    /// <summary>The sign-in the token belongs to (<c>sid</c>, OpenID Connect): its refresh token family.</summary>
    public const string SessionId = "sid";
}
