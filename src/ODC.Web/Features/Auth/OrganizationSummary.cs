using ODC.Web.Infrastructure.Tenancy;

namespace ODC.Web.Features.Auth;

/// <summary>
/// An organization the signed-in user may open (the switcher, the labels of lists gathered from several organizations).
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code (the address <c>/o/{code}/…</c>).</param>
/// <param name="Name">The official name.</param>
/// <param name="ShortName">The abbreviation, if any.</param>
/// <param name="Access">Why the user may act there: <c>Member</c> (works there), <c>Guest</c> (signs or chats there),
/// <c>Platform</c> (a platform administrator).</param>
public sealed record OrganizationSummary(Guid Id, string Code, string Name, string? ShortName, OrganizationAccess Access);
