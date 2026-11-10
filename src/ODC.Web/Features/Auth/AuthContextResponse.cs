namespace ODC.Web.Features.Auth;

/// <summary>
/// What the single-page application needs to start: the interface language, the antiforgery token of the current
/// identity, the signed-in user, if any, and the organizations: the one the request works in and those the user may
/// switch to.
/// </summary>
/// <param name="Language">The interface language of this request (<c>uk</c>, <c>en</c>).</param>
/// <param name="AntiforgeryToken">The token for the <c>X-CSRF-TOKEN</c> header of state-changing requests; it is bound
/// to the identity, so it is fetched again after signing in or out.</param>
/// <param name="User">The signed-in user, or <see langword="null"/>.</param>
/// <param name="Organization">The organization of this request (the one of the address <c>/o/{code}/…</c>, else the
/// user's default one), or <see langword="null"/> when there is none.</param>
/// <param name="Organizations">The organizations the user may open: where they work (the default one first), where they
/// take part as a guest and, for a platform administrator, every other one. Empty when signed out.</param>
public sealed record AuthContextResponse(
    string Language,
    string AntiforgeryToken,
    CurrentUserResponse? User,
    OrganizationSummary? Organization,
    IReadOnlyList<OrganizationSummary> Organizations);
