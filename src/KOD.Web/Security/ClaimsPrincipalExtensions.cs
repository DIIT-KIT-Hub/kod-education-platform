using System.Security.Claims;
using KOD.Infrastructure.Identity;

namespace KOD.Web.Security;

/// <summary>
/// Helpers for reading cabinet claims from a <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <param name="principal">The principal.</param>
    extension(ClaimsPrincipal principal)
    {
        /// <summary>
        /// Gets the user identifier, or <see langword="null"/> for anonymous users.
        /// </summary>
        public Guid? UserId => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        /// <summary>
        /// Gets the full name of the user.
        /// </summary>
        public string FullName => principal.FindFirstValue(AppClaimTypes.FullName) ?? principal.Identity?.Name ?? string.Empty;

        /// <summary>
        /// Gets the short name (with initials) of the user.
        /// </summary>
        public string ShortName => principal.FindFirstValue(AppClaimTypes.ShortName) ?? principal.FullName;

        /// <summary>
        /// Gets the server-side session identifier.
        /// </summary>
        public Guid? SessionId => Guid.TryParse(principal.FindFirstValue(AppClaimTypes.SessionId), out var id) ? id : null;

        /// <summary>
        /// Gets the profile photo, or <see langword="null"/> until the user uploads one.
        /// </summary>
        public Guid? PhotoId => Guid.TryParseExact(principal.FindFirstValue(AppClaimTypes.Photo), "N", out var id) ? id : null;

        /// <summary>
        /// Gets a value indicating whether the user must replace a temporary password.
        /// </summary>
        public bool MustChangePassword => principal.HasClaim(AppClaimTypes.MustChangePassword, "true");

        /// <summary>
        /// Determines whether the user has the permission.
        /// </summary>
        /// <param name="permission">The permission code.</param>
        /// <returns><see langword="true"/> when granted.</returns>
        public bool HasPermission(string permission) => principal.HasClaim(AppClaimTypes.Permission, permission);

        /// <summary>
        /// Determines whether the user has at least one of the permissions.
        /// </summary>
        /// <param name="permissions">The permission codes.</param>
        /// <returns><see langword="true"/> when any is granted.</returns>
        public bool HasAnyPermission(params ReadOnlySpan<string> permissions)
        {
            foreach (var permission in permissions)
            {
                if (principal.HasPermission(permission))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
