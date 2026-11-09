using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Models;

namespace ODC.Web.Shared.Access;

/// <summary>
/// The positions people hold, with the role names taken from the master database's catalog: what lists show next to a
/// name («Провідний інженер, Відділ розробки»).
/// </summary>
/// <param name="db">The organization's database.</param>
/// <param name="catalog">The shared catalogs (role names).</param>
internal sealed class PositionDirectory(OrganizationDbContext db, Catalog catalog)
{
    /// <summary>
    /// The primary position of each person (people without one are left out).
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The positions by person.</returns>
    public async Task<Dictionary<Guid, HeldPosition>> PrimaryAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var positions = await db.StaffPositions
            .AsNoTracking()
            .Where(p => ids.Contains(p.UserId) && p.IsPrimary)
            .Select(static p => new { p.UserId, p.Title, p.RoleId, p.OrganizationUnitId, Unit = p.OrganizationUnit!.Name })
            .ToListAsync(cancellationToken);
        var roles = await catalog.RolesAsync(cancellationToken);
        return positions
            .GroupBy(static p => p.UserId)
            .ToDictionary(
                static g => g.Key,
                g =>
                {
                    var p = g.First();
                    var role = roles.GetValueOrDefault(p.RoleId);
                    return new HeldPosition(p.UserId, p.OrganizationUnitId, p.Unit, p.RoleId, role?.Name ?? string.Empty, role?.IsLeadership ?? false, p.Title, IsPrimary: true);
                });
    }

    /// <summary>
    /// Every position of one person, the primary one first.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The positions.</returns>
    public async Task<List<HeldPosition>> AllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var positions = await db.StaffPositions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(static p => p.IsPrimary)
            .ThenBy(static p => p.Id)
            .Select(static p => new { p.UserId, p.Title, p.RoleId, p.OrganizationUnitId, Unit = p.OrganizationUnit!.Name, p.IsPrimary })
            .ToListAsync(cancellationToken);
        var roles = await catalog.RolesAsync(cancellationToken);
        return
        [
            .. positions.Select(p =>
            {
                var role = roles.GetValueOrDefault(p.RoleId);
                return new HeldPosition(p.UserId, p.OrganizationUnitId, p.Unit, p.RoleId, role?.Name ?? string.Empty, role?.IsLeadership ?? false, p.Title, p.IsPrimary);
            }),
        ];
    }

    /// <summary>
    /// Turns people into lookup items (name, e-mail, primary position, photo), keeping their order.
    /// </summary>
    /// <param name="members">The people, already filtered, ordered and limited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The items.</returns>
    public async Task<List<UserLookupItem>> LookupAsync(IQueryable<Member> members, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(members);
        var people = await members
            .AsNoTracking()
            .Select(static u => new
            {
                u.Id,
                Name = u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email,
                u.PhotoId,
            })
            .ToListAsync(cancellationToken);
        var positions = await PrimaryAsync(people.Select(static p => p.Id), cancellationToken);
        return [.. people.Select(p => new UserLookupItem(p.Id, p.Name, p.Email, positions.GetValueOrDefault(p.Id)?.InUnit, p.PhotoId))];
    }

    /// <summary>
    /// The primary position of one person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The position, or null.</returns>
    public async Task<HeldPosition?> PrimaryAsync(Guid userId, CancellationToken cancellationToken)
        => (await PrimaryAsync([userId], cancellationToken)).GetValueOrDefault(userId);
}
