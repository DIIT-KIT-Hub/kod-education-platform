using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The domain events of the organization's database, taken only when the request has an organization: requests without
/// one (the sign-in) must not open it.
/// </summary>
/// <param name="services">The services of the scope.</param>
internal sealed class OrganizationEvents(IServiceProvider services) : IDomainEventSource
{
    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
        => services.GetRequiredService<OrganizationContext>().Current is null
            ? []
            : services.GetRequiredService<OrganizationDbContext>().TakeDomainEvents();
}
