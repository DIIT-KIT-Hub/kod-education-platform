namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The permissions an account holds in any of its organizations: the union the access token carries (the organization
/// of a request then narrows it, see <see cref="OrganizationPermissions"/>).
/// </summary>
public interface IAccountPermissions
{
    /// <summary>
    /// The union of the permissions of every organization of an account.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The permissions.</returns>
    Task<IReadOnlySet<string>> UnionAsync(Guid userId, CancellationToken cancellationToken);
}
