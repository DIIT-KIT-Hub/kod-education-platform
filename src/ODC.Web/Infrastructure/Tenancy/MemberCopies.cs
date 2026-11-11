using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Writes the profiles of accounts to the databases of the organizations they work in (their memberships) and take part
/// in as guests (their participations: a signer, a chat member from another organization), one organization at a time.
/// </summary>
/// <param name="scopes">Creates scopes of the master database.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="organizations">Runs work in an organization.</param>
public sealed class MemberCopies(IServiceScopeFactory scopes, OrganizationRegistry registry, OrganizationScopes organizations)
{
    /// <summary>
    /// Writes profiles to the organizations of their accounts.
    /// </summary>
    /// <param name="profiles">The profiles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task CopyAsync(IReadOnlyCollection<MemberProfile> profiles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        if (profiles.Count == 0)
        {
            return;
        }

        var ids = profiles.Select(static p => p.Id).Distinct().ToList();
        List<(Guid UserId, Guid OrganizationId)> memberships;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var members = await master.Memberships
                .AsNoTracking()
                .Where(m => ids.Contains(m.UserId))
                .Select(static m => new { m.UserId, m.OrganizationId })
                .ToListAsync(cancellationToken);
            var guests = await master.Participations
                .AsNoTracking()
                .Where(p => ids.Contains(p.UserId))
                .Select(static p => new { p.UserId, p.OrganizationId })
                .ToListAsync(cancellationToken);
            memberships = [.. members.Concat(guests).Select(static m => (m.UserId, m.OrganizationId)).Distinct()];
        }

        foreach (var group in memberships.GroupBy(static m => m.OrganizationId))
        {
            // A suspended organization's database refuses connections; its copies are repaired when it is resumed.
            if (await registry.FindAsync(group.Key, cancellationToken) is not { IsActive: true } organization)
            {
                continue;
            }

            var people = group.Select(static m => m.UserId).ToHashSet();
            await organizations.RunAsync(organization, services => MemberSync.CopyAsync(
                services.GetRequiredService<OrganizationDbContext>(),
                [.. profiles.Where(p => people.Contains(p.Id))],
                cancellationToken));
        }
    }
}
