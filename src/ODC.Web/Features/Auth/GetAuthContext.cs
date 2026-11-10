using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// The start context of the application: the language, the antiforgery token (bound to the signed-in identity), the
/// signed-in user from the access token (with the permissions of the request's organization) and the organizations the
/// user may open. Reads the request and the registry only, so it needs no handler.
/// </summary>
public static class GetAuthContext
{
    /// <summary>
    /// <c>GET /api/v1/auth/context</c> (also before signing in).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapGet("context", HandleAsync).AllowAnonymous();

        /// <summary>
        /// Gets the start context of the application: language, antiforgery token, the signed-in user and their
        /// organizations.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="antiforgery">Issues antiforgery tokens.</param>
        /// <param name="organization">The organization of the request.</param>
        /// <param name="workplaces">The organizations of people.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The context.</returns>
        private static async Task<Ok<AuthContextResponse>> HandleAsync(
            HttpContext context,
            IAntiforgery antiforgery,
            OrganizationContext organization,
            Workplaces workplaces,
            CancellationToken cancellationToken)
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

            List<OrganizationSummary> organizations = [];
            if (user is not null)
            {
                var platform = principal.HasPermission(Permissions.Administration.AllOrganizations);
                foreach (var place in await workplaces.OfAsync(user.Id, platform, cancellationToken))
                {
                    organizations.Add(Summary(place.Organization, place.Access));
                }
            }

            var current = user is not null && organization.Current is { } chosen ? Summary(chosen, organization.Access) : null;
            return TypedResults.Ok(new AuthContextResponse(
                Languages.Current,
                antiforgery.GetAndStoreTokens(context).RequestToken ?? string.Empty,
                user,
                current,
                organizations));
        }

        /// <summary>
        /// Describes an organization for the client.
        /// </summary>
        /// <param name="organization">The organization.</param>
        /// <param name="access">Why the user may act there.</param>
        /// <returns>The summary.</returns>
        private static OrganizationSummary Summary(Shared.Domain.Tenancy.Organization organization, OrganizationAccess access)
            => new(organization.Id, organization.Code, organization.Name, organization.ShortName, access);
    }
}
