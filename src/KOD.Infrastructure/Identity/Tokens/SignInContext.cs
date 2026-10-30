namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// What a sign-in established, carried unchanged through every refresh of its tokens.
/// </summary>
/// <param name="Id">The sign-in (its refresh token family, the <c>sid</c> claim).</param>
/// <param name="AuthenticatedAt">When the user entered the password and the second factor (<c>auth_time</c>): the absolute
/// lifetime counts from it.</param>
/// <param name="AuthenticationMethod">The authentication method (RFC 8176 <c>amr</c>, e.g. <c>mfa</c>).</param>
/// <param name="IsPersistent">Whether the sign-in survives closing the browser.</param>
public sealed record SignInContext(Guid Id, DateTimeOffset AuthenticatedAt, string? AuthenticationMethod, bool IsPersistent);
