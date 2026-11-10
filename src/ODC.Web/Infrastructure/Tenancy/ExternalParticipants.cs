using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// People of other organizations who take part in the organization of the request (a signer of its document, a member of
/// its chat): an active account that holds the permission in one of its own organizations. Their profiles are copied into
/// the organization's database, so its documents and chats point at a member as for its own people; they never get a
/// membership there, and in that organization they act as guests (<see cref="OrganizationAccess.Guest"/>).
/// </summary>
/// <param name="master">The master database (accounts and memberships).</param>
/// <param name="db">The organization's database.</param>
/// <param name="organization">The organization of the request.</param>
/// <param name="permissions">The permissions of accounts in their organizations.</param>
public sealed class ExternalParticipants(MasterDbContext master, OrganizationDbContext db, OrganizationContext organization, IAccountPermissions permissions)
{
    /// <summary>
    /// Admits the people of other organizations among the given ones who hold a permission in an organization of theirs:
    /// copies their profiles into the organization's database (again, when they are already known there).
    /// </summary>
    /// <param name="userIds">The people (members of the organization are left out).</param>
    /// <param name="permission">The permission they need (e.g. <c>documents.sign</c>, <c>chats.use</c>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people admitted.</returns>
    public async Task<IReadOnlyList<Guid>> AdmitAsync(IReadOnlyCollection<Guid> userIds, string permission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var here = organization.Require().Id;
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var members = await master.Memberships.Where(m => m.OrganizationId == here && ids.Contains(m.UserId)).Select(static m => m.UserId).ToListAsync(cancellationToken);
        var outside = ids.Except(members).ToList();
        if (outside.Count == 0)
        {
            return [];
        }

        var admitted = new List<User>();
        foreach (var user in await master.Users.AsNoTracking().Where(u => outside.Contains(u.Id) && u.IsActive).ToListAsync(cancellationToken))
        {
            if ((await permissions.UnionAsync(user.Id, cancellationToken)).Contains(permission))
            {
                admitted.Add(user);
            }
        }

        await MemberSync.CopyAsync(db, [.. admitted.Select(MemberProfile.Of)], cancellationToken);
        return [.. admitted.Select(static u => u.Id)];
    }
}
