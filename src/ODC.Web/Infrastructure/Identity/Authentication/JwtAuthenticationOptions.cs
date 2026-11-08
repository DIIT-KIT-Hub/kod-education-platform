using Microsoft.AspNetCore.Authentication;

namespace ODC.Web.Infrastructure.Identity.Authentication;

/// <summary>
/// Options of the <see cref="JwtAuthenticationHandler"/> (the token settings come from <c>Authentication:Jwt</c>).
/// </summary>
internal sealed class JwtAuthenticationOptions : AuthenticationSchemeOptions
{
}
