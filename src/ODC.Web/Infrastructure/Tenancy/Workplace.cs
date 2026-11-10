using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// An organization a person may open, and why.
/// </summary>
/// <param name="Organization">The organization.</param>
/// <param name="Access">Why the person may act there (member, guest, platform administrator).</param>
public sealed record Workplace(Organization Organization, OrganizationAccess Access);
