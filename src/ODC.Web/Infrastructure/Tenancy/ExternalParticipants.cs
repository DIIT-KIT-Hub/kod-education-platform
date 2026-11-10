using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Models;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// People of other organizations who take part in the organization of the request (a signer of its document, a member of
/// its chat, a platform administrator at work there): an active account that holds the permission in one of its own
/// organizations. Their profiles are copied into the organization's database, so its documents and chats point at a
/// member as for its own people, and a <see cref="Participation"/> is recorded in the master database (first, so the
/// lists that gather a person's work from every organization never miss this one); they never get a membership there,
/// and in that organization they act as guests (<see cref="OrganizationAccess.Guest"/>).
/// </summary>
/// <param name="master">The master database (accounts, memberships, participations).</param>
/// <param name="db">The organization's database.</param>
/// <param name="organization">The organization of the request.</param>
/// <param name="permissions">The permissions of accounts in their organizations.</param>
/// <param name="cache">The access cache (the organizations of a person).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="registry">The registry of organizations (the names shown next to people of other organizations).</param>
public sealed class ExternalParticipants(
    MasterDbContext master,
    OrganizationDbContext db,
    OrganizationContext organization,
    IAccountPermissions permissions,
    AccessCache cache,
    TimeProvider timeProvider,
    OrganizationRegistry registry)
{
    /// <summary>
    /// Admits the people of other organizations among the given ones who hold a permission in an organization of theirs:
    /// records their participation and copies their profiles into the organization's database (again, when they are
    /// already known there).
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
            if (await HoldsAsync(user.Id, permission, cancellationToken))
            {
                admitted.Add(user);
            }
        }

        await CopyAsync(here, admitted, cancellationToken);
        return [.. admitted.Select(static u => u.Id)];
    }

    /// <summary>
    /// Names the organizations of the guests among people found here (copies of people of other organizations have no
    /// position here): their organizations stand in place of the position.
    /// </summary>
    /// <param name="people">The people found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people, guests described.</returns>
    public async Task<List<UserLookupItem>> DescribeGuestsAsync(List<UserLookupItem> people, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(people);
        var guests = people.Where(static p => string.IsNullOrEmpty(p.Position)).Select(static p => p.Id).ToList();
        if (guests.Count == 0)
        {
            return people;
        }

        var here = organization.Require().Id;
        var homes = (await master.Memberships.AsNoTracking()
                .Where(m => guests.Contains(m.UserId) && m.OrganizationId != here)
                .OrderBy(static m => m.CreatedAt)
                .Select(static m => new { m.UserId, m.OrganizationId })
                .ToListAsync(cancellationToken))
            .GroupBy(static m => m.UserId)
            .ToDictionary(static g => g.Key, static g => g.Select(static m => m.OrganizationId).ToList());
        var result = new List<UserLookupItem>(people.Count);
        foreach (var person in people)
        {
            result.Add(homes.TryGetValue(person.Id, out var organizations) ? person with { Position = await NamesAsync(organizations, cancellationToken) } : person);
        }

        return result;
    }

    /// <summary>
    /// Finds people of other organizations for a picker (a signer, a chat partner): active accounts working elsewhere
    /// but not here whose name or e-mail matches every word and who hold the permission in an organization of theirs.
    /// Their organization stands in place of the position.
    /// </summary>
    /// <param name="words">The words of the search (each must match).</param>
    /// <param name="permission">The permission they need.</param>
    /// <param name="exclude">A person to leave out (the one who searches).</param>
    /// <param name="take">How many to find at most.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The people, by name.</returns>
    public async Task<List<UserLookupItem>> SearchAsync(IReadOnlyList<string> words, string permission, Guid? exclude, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(words);
        var here = organization.Require().Id;
        var people = master.Users.AsNoTracking().Where(u => u.IsActive
            && u.Id != exclude
            && master.Memberships.Any(m => m.UserId == u.Id && m.OrganizationId != here)
            && !master.Memberships.Any(m => m.UserId == u.Id && m.OrganizationId == here));
        foreach (var word in words.Take(4))
        {
            people = people.Where(u => u.LastName.Contains(word) || u.FirstName.Contains(word) || (u.MiddleName != null && u.MiddleName.Contains(word)) || u.Email!.Contains(word));
        }

        var found = await people
            .OrderBy(static u => u.LastName)
            .ThenBy(static u => u.FirstName)
            .Take(take * 2)
            .Select(u => new
            {
                u.Id,
                Name = u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email,
                u.PhotoId,
                Organizations = master.Memberships.Where(m => m.UserId == u.Id).OrderBy(static m => m.CreatedAt).Select(static m => m.OrganizationId).ToList(),
            })
            .ToListAsync(cancellationToken);

        var result = new List<UserLookupItem>();
        foreach (var person in found)
        {
            if (result.Count == take || !await HoldsAsync(person.Id, permission, cancellationToken))
            {
                continue;
            }

            result.Add(new UserLookupItem(person.Id, person.Name, person.Email ?? string.Empty, await NamesAsync(person.Organizations, cancellationToken), person.PhotoId));
        }

        return result;
    }

    /// <summary>
    /// Makes sure a person who acts in the organization without working there (a platform administrator) has a profile
    /// copy and a participation, so what they create there can point at them. Checked once a minute per person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task EnsurePresentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var here = organization.Require().Id;
        await cache.GetAsync($"present:{userId}:{here}", async () =>
        {
            if (!await db.Members.AnyAsync(m => m.Id == userId, cancellationToken)
                && await master.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) is { } user)
            {
                await CopyAsync(here, [user], cancellationToken);
            }

            return true;
        });
    }

    /// <summary>
    /// The names of organizations (abbreviations where there are), for the place of a position.
    /// </summary>
    /// <param name="organizations">The organizations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names, comma-separated.</returns>
    private async Task<string> NamesAsync(List<Guid> organizations, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var id in organizations)
        {
            if (await registry.FindAsync(id, cancellationToken) is { } place)
            {
                names.Add(place.ShortName ?? place.Name);
            }
        }

        return string.Join(", ", names);
    }

    /// <summary>
    /// Whether a person holds a permission in an organization of theirs (read again a minute after the last time).
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="permission">The permission.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when held.</returns>
    private async Task<bool> HoldsAsync(Guid userId, string permission, CancellationToken cancellationToken)
        => (await cache.GetAsync($"union:{userId}", () => permissions.UnionAsync(userId, cancellationToken))).Contains(permission);

    /// <summary>
    /// Records the participation of people and copies their profiles into the organization's database.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="people">The accounts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    private async Task CopyAsync(Guid organizationId, List<User> people, CancellationToken cancellationToken)
    {
        if (people.Count == 0)
        {
            return;
        }

        var ids = people.Select(static u => u.Id).ToList();
        var known = await master.Participations.Where(p => p.OrganizationId == organizationId && ids.Contains(p.UserId)).Select(static p => p.UserId).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var id in ids.Except(known))
        {
            master.Participations.Add(new Participation(id, organizationId, now));
        }

        if (known.Count != ids.Count)
        {
            await master.SaveChangesAsync(cancellationToken);
            cache.Changed();
        }

        await MemberSync.CopyAsync(db, [.. people.Select(MemberProfile.Of)], cancellationToken);
    }
}
