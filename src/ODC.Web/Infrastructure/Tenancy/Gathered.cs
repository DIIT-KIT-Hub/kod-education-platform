using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// What a read found in one organization.
/// </summary>
/// <typeparam name="T">The type of the finding.</typeparam>
/// <param name="Organization">The organization.</param>
/// <param name="Value">What was found there.</param>
public sealed record Gathered<T>(Organization Organization, T Value);
