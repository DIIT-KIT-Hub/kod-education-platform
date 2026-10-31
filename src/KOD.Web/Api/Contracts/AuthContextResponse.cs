namespace KOD.Web.Api.Contracts;

/// <summary>
/// What the single-page application needs to start: the interface language, the antiforgery token of the current
/// identity and the signed-in user, if any.
/// </summary>
/// <param name="Language">The interface language of this request (<c>uk</c>, <c>en</c>).</param>
/// <param name="AntiforgeryToken">The token for the <c>X-CSRF-TOKEN</c> header of state-changing requests; it is bound
/// to the identity, so it is fetched again after signing in or out.</param>
/// <param name="User">The signed-in user, or <see langword="null"/>.</param>
public sealed record AuthContextResponse(string Language, string AntiforgeryToken, CurrentUserResponse? User);
