using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The organization of the current request or unit of work (scoped). Set once, before the organization's database is
/// used: by <c>OrganizationMiddleware</c> for requests and by <see cref="OrganizationScopes"/> for the server's own work.
/// The organization's database refuses to open without it (fail closed).
/// </summary>
public sealed class OrganizationContext
{
    /// <summary>
    /// Gets the organization, or null when none is chosen.
    /// </summary>
    public Organization? Current { get; private set; }

    /// <summary>
    /// Gets why the current user may act in it.
    /// </summary>
    public OrganizationAccess Access { get; private set; }

    /// <summary>
    /// Chooses the organization.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <param name="access">Why the current user may act in it.</param>
    public void Enter(Organization organization, OrganizationAccess access)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (Current is not null && Current.Id != organization.Id)
        {
            throw new InvalidOperationException("The organization of a unit of work cannot change.");
        }

        Current = organization;
        Access = access;
    }

    /// <summary>
    /// Gets the organization, or fails when none is chosen.
    /// </summary>
    /// <returns>The organization.</returns>
    /// <exception cref="NoOrganizationException">When none is chosen.</exception>
    public Organization Require() => Current ?? throw new NoOrganizationException();
}
