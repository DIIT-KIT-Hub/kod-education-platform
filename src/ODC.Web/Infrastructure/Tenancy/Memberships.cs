using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Which organizations a person works in (the memberships of the master database): read for every request (cached), and
/// brought in line with the positions after they change in an organization.
/// </summary>
/// <param name="master">The master database.</param>
/// <param name="cache">The access cache.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class Memberships(MasterDbContext master, AccessCache cache, TimeProvider timeProvider)
{
    /// <summary>
    /// The organizations of a person, the earliest membership (the default organization) first.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization identifiers.</returns>
    public Task<List<Guid>> OfAsync(Guid userId, CancellationToken cancellationToken)
        => cache.GetAsync($"memberships:{userId}", () => master.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(static m => m.CreatedAt)
            .Select(static m => m.OrganizationId)
            .ToListAsync(cancellationToken));

    /// <summary>
    /// Records who works in an organization after their positions there changed: a person with a position is a member,
    /// a person without one is not.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="people">The people and whether they hold a position there now.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task SyncAsync(Guid organizationId, IReadOnlyCollection<(Guid UserId, bool HasPositions)> people, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(people);
        if (people.Count == 0)
        {
            return;
        }

        var ids = people.Select(static p => p.UserId).Distinct().ToList();
        var existing = await master.Memberships
            .Where(m => m.OrganizationId == organizationId && ids.Contains(m.UserId))
            .ToDictionaryAsync(static m => m.UserId, cancellationToken);
        var known = await master.Users.Where(u => ids.Contains(u.Id)).Select(static u => u.Id).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var (userId, hasPositions) in people)
        {
            if (hasPositions && !existing.ContainsKey(userId) && known.Contains(userId))
            {
                var membership = new Membership(userId, organizationId, now);
                master.Memberships.Add(membership);
                existing[userId] = membership;
            }
            else if (!hasPositions && existing.Remove(userId, out var ended))
            {
                master.Memberships.Remove(ended);
            }
        }

        await master.SaveChangesAsync(cancellationToken);
        cache.Changed();
    }
}
