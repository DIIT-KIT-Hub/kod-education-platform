using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The organizations a person may open and why: the ones they work in (their memberships, the default one first), the
/// ones they take part in as a guest (<see cref="Participation"/>) and, for a platform administrator, every other
/// active organization.
/// </summary>
/// <param name="master">The master database.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="memberships">The memberships of people.</param>
/// <param name="cache">The access cache.</param>
public sealed class Workplaces(MasterDbContext master, OrganizationRegistry registry, Memberships memberships, AccessCache cache)
{
    /// <summary>
    /// The organizations of a person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="platform">Whether every organization counts (a platform administrator choosing where to go); the
    /// lists that gather a person's own work pass <see langword="false"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organizations: memberships first (the default one first), then the rest by name.</returns>
    public async Task<IReadOnlyList<Workplace>> OfAsync(Guid userId, bool platform, CancellationToken cancellationToken)
    {
        var all = await registry.AllAsync(cancellationToken);
        var mine = await memberships.OfAsync(userId, cancellationToken);
        var guest = await cache.GetAsync(
            $"participations:{userId}",
            () => master.Participations.AsNoTracking().Where(p => p.UserId == userId).Select(static p => p.OrganizationId).ToListAsync(cancellationToken));

        var result = new List<Workplace>();
        foreach (var id in mine)
        {
            if (all.FirstOrDefault(o => o.Id == id) is { IsActive: true } organization)
            {
                result.Add(new Workplace(organization, OrganizationAccess.Member));
            }
        }

        foreach (var organization in all.Where(o => o.IsActive && !mine.Contains(o.Id)).OrderBy(static o => o.Name, StringComparer.CurrentCulture))
        {
            if (guest.Contains(organization.Id))
            {
                result.Add(new Workplace(organization, OrganizationAccess.Guest));
            }
            else if (platform)
            {
                result.Add(new Workplace(organization, OrganizationAccess.Platform));
            }
        }

        return result;
    }
}
