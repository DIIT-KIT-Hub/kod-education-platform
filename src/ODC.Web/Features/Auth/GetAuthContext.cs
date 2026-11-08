using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Security;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// The start context of the application: the language, the antiforgery token (bound to the signed-in identity) and the
/// signed-in user from the access token. Reads the request only, so it needs no handler.
/// </summary>
public static class GetAuthContext
{
    /// <summary>
    /// <c>GET /api/v1/auth/context</c> (also before signing in).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapGet("context", Handle).AllowAnonymous();

        /// <summary>
        /// Gets the start context of the application: language, antiforgery token and the signed-in user.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="antiforgery">Issues antiforgery tokens.</param>
        /// <returns>The context.</returns>
        private static Ok<AuthContextResponse> Handle(HttpContext context, IAntiforgery antiforgery)
        {
            var principal = context.User;
            var user = principal.UserId is { } id
                ? new CurrentUserResponse(
                    id,
                    principal.FindFirstValue(AppClaimTypes.Email) ?? string.Empty,
                    principal.FullName,
                    principal.ShortName,
                    principal.PhotoId,
                    [.. principal.FindAll(AppClaimTypes.Permission).Select(static c => c.Value)])
                : null;

            return TypedResults.Ok(new AuthContextResponse(Languages.Current, antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty, user));
        }
    }
}
