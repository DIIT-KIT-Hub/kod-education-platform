using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Rules shared by the user administration slices: administration is scoped to the organizations of the
/// administrator's positions (<see cref="AdministrationScope"/>), and nobody manages an account with more permissions
/// than their own (no privilege escalation).
/// </summary>
/// <param name="db">The master database (accounts, roles).</param>
/// <param name="organization">The organization's database (members, positions, units).</param>
/// <param name="catalog">The shared catalogs (which unit types are organizations).</param>
/// <param name="currentUser">The current user.</param>
/// <param name="permissions">Resolves effective permissions.</param>
/// <param name="accessScope">The administration scopes.</param>
/// <param name="outbox">The e-mail outbox.</param>
/// <param name="emailOptions">The e-mail settings (site address for the links).</param>
internal sealed class UserDirectory(
    MasterDbContext db,
    OrganizationDbContext organization,
    Catalog catalog,
    ICurrentUser currentUser,
    IPermissionResolver permissions,
    IAccessScope accessScope,
    IEmailOutbox outbox,
    IOptions<EmailOptions> emailOptions)
{
    /// <summary>
    /// The error shown when a position is placed outside the organizations the current user manages.
    /// </summary>
    public const string OutsideScopeMessage = "Ви можете призначати посади лише в підрозділах організацій, якими керуєте.";

    /// <summary>
    /// The error shown when an account would be left without positions by an organization administrator.
    /// </summary>
    public const string NoPositionsMessage = "Призначте хоча б одну посаду в підрозділі своєї організації: обліковий запис без посад бачить лише адміністратор усіх організацій. Щоб припинити доступ, заблокуйте обліковий запис.";

    /// <summary>
    /// The error shown when an account also has positions in organizations the current user does not manage.
    /// </summary>
    public const string SharedAccountMessage = "Користувач також обіймає посади в організаціях, якими Ви не керуєте. Особисті дані, e-mail, статус і безпеку такого облікового запису змінює адміністратор, що керує всіма його організаціями; Ви можете змінювати лише посади у своїх організаціях.";

    /// <summary>
    /// Gets a value indicating whether e-mails can be sent.
    /// </summary>
    public bool CanSendEmail => outbox.IsEnabled;

    /// <summary>
    /// The route group <c>/api/v1/users</c> (people who see users).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapUsers(IEndpointRouteBuilder api)
        => api.MapGroup("users").RequirePermission(Permissions.Users.View).WithTags("Users");

    /// <summary>
    /// The route group <c>/api/v1/user-imports</c> (people who import users).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapUserImports(IEndpointRouteBuilder api)
        => api.MapGroup("user-imports").RequirePermission(Permissions.Users.Import).WithTags("UserImports");

    /// <summary>
    /// The error of an account outside the scope or missing.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Користувача не знайдено."));

    /// <summary>
    /// Detects the spreadsheet format by the file extension.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The format or <see langword="null"/>.</returns>
    public static SpreadsheetFormat? DetectFormat(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".xlsx" => SpreadsheetFormat.Xlsx,
        ".csv" => SpreadsheetFormat.Csv,
        _ => null,
    };

    /// <summary>
    /// Wraps spreadsheet bytes into a downloadable file.
    /// </summary>
    /// <param name="content">The bytes.</param>
    /// <param name="baseName">The file name without extension.</param>
    /// <param name="format">The format.</param>
    /// <returns>The file.</returns>
    public static DownloadableFile ToFile(byte[] content, string baseName, SpreadsheetFormat format) => format == SpreadsheetFormat.Csv
        ? new DownloadableFile(new MemoryStream(content), $"{baseName}.csv", "text/csv")
        : new DownloadableFile(new MemoryStream(content), $"{baseName}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

    /// <summary>
    /// Restricts a query to the people with at least one position in the scope.
    /// </summary>
    /// <param name="members">The query.</param>
    /// <param name="scope">The administration scope.</param>
    /// <returns>The restricted query.</returns>
    public static IQueryable<Member> InScope(IQueryable<Member> members, AdministrationScope scope)
    {
        if (scope.Units is not { } scoped)
        {
            return members;
        }

        var units = scoped.ToList();
        return members.Where(u => u.Positions.Any(p => units.Contains(p.OrganizationUnitId)));
    }

    /// <summary>
    /// Returns the units of a scope as a list usable in queries (empty for the cabinet-wide scope).
    /// </summary>
    /// <param name="scope">The administration scope.</param>
    /// <returns>The units.</returns>
    public static List<Guid> ScopeUnits(AdministrationScope scope) => scope.Units is { } units ? [.. units] : [];

    /// <summary>
    /// Gets the individually granted or denied permissions of a loaded member.
    /// </summary>
    /// <param name="member">The member with permission overrides.</param>
    /// <param name="claimType">The override claim type (<see cref="PermissionClaimTypes"/>).</param>
    /// <returns>The permission codes.</returns>
    public static HashSet<string> Overrides(Member member, string claimType)
        => member.PermissionOverrides.Where(o => o.ClaimType == claimType).Select(static o => o.ClaimValue).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Applies the grid filters.
    /// </summary>
    /// <param name="users">The query.</param>
    /// <param name="filter">The filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The filtered query.</returns>
    public async Task<IQueryable<Member>> FilterAsync(IQueryable<Member> users, UserQuery filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        users = filter.Status switch
        {
            UserStatusFilter.Active => users.Where(static u => u.IsActive),
            UserStatusFilter.Blocked => users.Where(static u => !u.IsActive),
            UserStatusFilter.AwaitingActivation => users.Where(static u => u.IsActive && u.AwaitsActivation),
            _ => users,
        };

        if (filter.UnitId is { } unitId)
        {
            var hierarchy = await UnitHierarchy.LoadAsync(organization, catalog, cancellationToken);
            var subtree = hierarchy.WithDescendants([unitId]).ToList();
            users = users.Where(u => u.Positions.Any(p => subtree.Contains(p.OrganizationUnitId)));
        }

        if (filter.RoleId is { } roleId)
        {
            users = users.Where(u => u.Positions.Any(p => p.RoleId == roleId));
        }

        if (filter.Search is { } search)
        {
            var term = search;
            users = users.Where(u =>
                u.LastName.Contains(term)
                || u.FirstName.Contains(term)
                || (u.MiddleName != null && u.MiddleName.Contains(term))
                || u.Email.Contains(term));
        }

        return users;
    }

    /// <summary>
    /// Determines whether the current user may assign the role (prevents privilege escalation).
    /// </summary>
    /// <param name="role">The role with its permissions.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool CanAssign(Role role)
        => currentUser.Has(Permissions.Roles.Manage)
            || (!role.IsAdministrator && role.Permissions.All(p => currentUser.Has(p.ClaimValue!)));

    /// <summary>
    /// Validates positions: units and roles exist and the roles may be assigned by the current user.
    /// </summary>
    /// <param name="positions">The positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error or <see langword="null"/>.</returns>
    public async Task<Error?> ValidatePositionsAsync(IReadOnlyList<PositionInput> positions, CancellationToken cancellationToken)
    {
        var unitIds = positions.Select(static p => p.UnitId).Distinct().ToList();
        var roleIds = positions.Select(static p => p.RoleId).Distinct().ToList();

        if (await organization.OrganizationUnits.CountAsync(u => unitIds.Contains(u.Id), cancellationToken) != unitIds.Count)
        {
            return Error.Validation(Texts.T("Обраний підрозділ не знайдено."));
        }

        var roles = await db.Roles.AsNoTracking().Include(static r => r.Permissions).Where(r => roleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Count)
        {
            return Error.Validation(Texts.T("Обрану роль не знайдено."));
        }

        var forbidden = roles.Find(r => !CanAssign(r));
        return forbidden is null
            ? null
            : Error.Forbidden(Texts.T("Ви не можете призначати роль «{0}», оскільки вона надає більше прав, ніж маєте Ви.", Texts.T(forbidden.Name)));
    }

    /// <summary>
    /// Determines whether the command keeps everything of the account except its positions: e-mail, personal data and,
    /// for owners of <c>roles.manage</c>, the individual permissions.
    /// </summary>
    /// <param name="user">The stored account.</param>
    /// <param name="member">The account's member with the individual permissions.</param>
    /// <param name="email">The normalized requested e-mail.</param>
    /// <param name="command">The command.</param>
    /// <returns><see langword="true"/> when only the positions may change.</returns>
    public bool IsAccountUnchanged(User user, Member member, string email, SaveUserCommand command)
        => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)
            && Same(user.LastName, command.LastName)
            && Same(user.FirstName, command.FirstName)
            && Same(user.MiddleName, command.MiddleName)
            && Same(user.PhoneNumber, command.PhoneNumber)
            && (!currentUser.Has(Permissions.Roles.Manage)
                || (Overrides(member, PermissionClaimTypes.Permission).SetEquals(command.GrantedPermissions)
                    && Overrides(member, PermissionClaimTypes.DeniedPermission).SetEquals(command.DeniedPermissions)));

    /// <summary>
    /// Loads a user for a credential operation (<c>users.security</c>).
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked user.</returns>
    public async Task<Result<User>> LoadForSecurityAsync(Guid id, CancellationToken cancellationToken)
    {
        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Security, cancellationToken);
        return await LoadForManagementAsync(id, scope, requireWholeAccount: true, cancellationToken);
    }

    /// <summary>
    /// Loads a user for modification: the account must have a position in the administrator's organizations (all of
    /// its positions when <paramref name="requireWholeAccount"/> is set), and the current user's permissions must cover
    /// the target's permissions.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="scope">The administration scope of the operation's permission.</param>
    /// <param name="requireWholeAccount">Whether the operation changes the account as a whole rather than only positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked user.</returns>
    public async Task<Result<User>> LoadForManagementAsync(Guid id, AdministrationScope scope, bool requireWholeAccount, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        var units = await organization.StaffPositions.Where(p => p.UserId == id).Select(static p => p.OrganizationUnitId).ToListAsync(cancellationToken);
        if (user is null || !scope.Overlaps(units))
        {
            return NotFound();
        }

        if (requireWholeAccount && !scope.Covers(units))
        {
            return Error.Forbidden(Texts.T(SharedAccountMessage));
        }

        if (id != currentUser.Id)
        {
            var target = await permissions.GetEffectivePermissionsAsync(id, cancellationToken);
            if (target.Any(p => !currentUser.Has(p)))
            {
                return Error.Forbidden(Texts.T("Цей обліковий запис має більше прав, ніж Ви, тому Ви не можете ним керувати."));
            }
        }

        return user;
    }

    /// <summary>
    /// Loads the tracked member of an account with its positions and individual permissions, creating it from the account
    /// when the organization does not know the person yet.
    /// </summary>
    /// <param name="user">The account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked member.</returns>
    public async Task<Member> MemberOfAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var member = await organization.Members
            .Include(static m => m.Positions)
            .Include(static m => m.PermissionOverrides)
            .FirstOrDefaultAsync(m => m.Id == user.Id, cancellationToken);
        if (member is null)
        {
            member = new Member(MemberProfile.Of(user));
            organization.Members.Add(member);
        }

        return member;
    }

    /// <summary>
    /// The units of the positions of each account.
    /// </summary>
    /// <param name="userIds">The accounts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units by account (accounts without positions are left out).</returns>
    public async Task<Dictionary<Guid, List<Guid>>> PositionUnitsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var positions = await organization.StaffPositions
            .Where(p => userIds.Contains(p.UserId))
            .Select(static p => new { p.UserId, p.OrganizationUnitId })
            .ToListAsync(cancellationToken);
        return positions.GroupBy(static p => p.UserId).ToDictionary(static g => g.Key, static g => g.Select(static p => p.OrganizationUnitId).ToList());
    }

    /// <summary>
    /// Saves the changed positions and individual permissions (the organization's database).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task SaveMembersAsync(CancellationToken cancellationToken) => organization.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Sends an account the invitation to activate it (when e-mail is configured).
    /// </summary>
    /// <param name="user">The user.</param>
    public void Invite(User user)
    {
        if (outbox.IsEnabled)
        {
            outbox.Enqueue(AccountEmails.Invitation(user, emailOptions.Value.SiteUrl));
        }
    }

    /// <summary>
    /// Tells the owner that the password was reset (when e-mail is configured and the account is active).
    /// </summary>
    /// <param name="user">The user.</param>
    public void NotifyPasswordReset(User user)
    {
        if (outbox.IsEnabled && user.IsActive)
        {
            outbox.Enqueue(AccountEmails.PasswordReset(user, emailOptions.Value.SiteUrl));
        }
    }

    /// <summary>
    /// Compares a stored optional text with a requested one the way the domain normalizes it (trimmed, empty means none).
    /// </summary>
    /// <param name="current">The stored value.</param>
    /// <param name="requested">The requested value.</param>
    /// <returns><see langword="true"/> when the values are the same.</returns>
    private static bool Same(string? current, string? requested)
        => string.Equals(
            string.IsNullOrWhiteSpace(current) ? null : current.Trim(),
            string.IsNullOrWhiteSpace(requested) ? null : requested.Trim(),
            StringComparison.Ordinal);
}
