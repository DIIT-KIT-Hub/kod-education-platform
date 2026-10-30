using System.Security.Claims;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// A newly issued access token.
/// </summary>
/// <param name="Token">The compact JWT.</param>
/// <param name="ExpiresAt">The expiry (<c>exp</c>).</param>
/// <param name="Identity">The identity the token carries, for the request that issued it.</param>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt, ClaimsIdentity Identity);
