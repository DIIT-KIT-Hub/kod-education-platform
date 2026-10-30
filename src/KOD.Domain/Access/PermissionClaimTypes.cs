namespace KOD.Domain.Access;

/// <summary>
/// Claim types under which permissions are stored in the Identity claim tables
/// (<c>role_claims</c> for roles, <c>user_claims</c> for individual adjustments) and issued in access tokens.
/// </summary>
public static class PermissionClaimTypes
{
    /// <summary>
    /// A granted permission: a role claim, an individual grant (user claim) and an effective permission in the access token.
    /// </summary>
    public const string Permission = "kod:permission";

    /// <summary>
    /// An individually denied permission (user claim): it is removed regardless of the roles.
    /// </summary>
    public const string DeniedPermission = "kod:permission.denied";
}
