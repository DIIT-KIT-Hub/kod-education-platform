using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// The roles for select inputs (system roles first). Sent by the user administration (position editor, import).
/// </summary>
public static class ListRoleOptions
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Query : IQuery<IReadOnlyList<RoleOption>>;

    /// <summary>
    /// Reads the roles.
    /// </summary>
    /// <param name="db">The database context.</param>
    internal sealed class Handler(ApplicationDbContext db) : IRequestHandler<Query, IReadOnlyList<RoleOption>>
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<RoleOption>> HandleAsync(Query request, CancellationToken cancellationToken)
            => await db.Roles
                .AsNoTracking()
                .OrderByDescending(static r => r.IsSystem)
                .ThenBy(static r => r.CreatedAt)
                .Select(static r => new RoleOption(r.Id, r.Code, StoredText.Display(r.Name)))
                .ToListAsync(cancellationToken);
    }
}
