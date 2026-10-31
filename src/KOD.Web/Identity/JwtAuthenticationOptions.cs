using Microsoft.AspNetCore.Authentication;

namespace KOD.Web.Identity;

/// <summary>
/// Options of the <see cref="JwtAuthenticationHandler"/> (the token settings come from <c>Authentication:Jwt</c>).
/// </summary>
internal sealed class JwtAuthenticationOptions : AuthenticationSchemeOptions
{
}
