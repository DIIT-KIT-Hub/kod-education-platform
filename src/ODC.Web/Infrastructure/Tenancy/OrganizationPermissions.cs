using System.Collections.Frozen;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Which permissions a person has in the organization of a request. The access token carries the union of the
/// permissions of every organization the person works in; in one organization they hold:
/// <list type="bullet">
/// <item>a platform administrator (<c>administration.all</c>): all of them, everywhere;</item>
/// <item>a member: those of their positions there, plus the platform-wide ones they hold anywhere;</item>
/// <item>a guest: the platform-wide ones and what a participant from another organization needs (to see and sign a
/// document, to write in a chat they are in).</item>
/// </list>
/// </summary>
public static class OrganizationPermissions
{
    /// <summary>
    /// Gets the permissions that are not bound to an organization: roles, the audit log and the support desk.
    /// </summary>
    public static FrozenSet<string> PlatformWide { get; } = FrozenSet.Create(
        StringComparer.Ordinal,
        Permissions.Administration.AllOrganizations,
        Permissions.Roles.Manage,
        Permissions.Audit.View,
        Permissions.Tickets.Manage);

    /// <summary>
    /// Gets what a participant from another organization may do there (the data rules limit it to their documents and
    /// chats).
    /// </summary>
    public static FrozenSet<string> Participant { get; } = FrozenSet.Create(
        StringComparer.Ordinal,
        Permissions.Documents.View,
        Permissions.Documents.Sign,
        Permissions.Chats.Use);

    /// <summary>
    /// Whether the token makes its owner a platform administrator.
    /// </summary>
    /// <param name="token">The permissions of the access token.</param>
    /// <returns>Whether it does.</returns>
    public static bool IsPlatformAdministrator(IReadOnlySet<string> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Contains(Permissions.Administration.AllOrganizations);
    }

    /// <summary>
    /// The permissions in one organization.
    /// </summary>
    /// <param name="access">Why the person may act there.</param>
    /// <param name="token">The permissions of the access token (every organization).</param>
    /// <param name="here">The permissions of the person's positions there (members only).</param>
    /// <returns>The permissions.</returns>
    public static HashSet<string> Effective(OrganizationAccess access, IReadOnlySet<string> token, IReadOnlySet<string> here)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(here);
        var platform = token.Where(PlatformWide.Contains);
        return access switch
        {
            OrganizationAccess.Platform => [.. token],
            OrganizationAccess.Member => [.. here, .. platform],
            OrganizationAccess.Guest => [.. token.Where(Participant.Contains), .. platform],
            _ => [.. platform],
        };
    }
}
