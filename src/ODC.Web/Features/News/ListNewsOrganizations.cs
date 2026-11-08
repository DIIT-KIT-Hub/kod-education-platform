using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Features.Structure.Units;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.News;

/// <summary>
/// The organizations the current user publishes news for (sent to the structure feature as a unit options query).
/// </summary>
public static class ListNewsOrganizations
{
    /// <summary>
    /// <c>GET /api/v1/news/organizations</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => NewsDesk.MapNews(api).MapGet("organizations", HandleAsync).RequirePermission(Permissions.News.Manage);

        /// <summary>
        /// Gets the organizations the user publishes news for; <c>cabinetWide</c> tells whether news for the whole cabinet
        /// (no organization) are allowed too.
        /// </summary>
        /// <param name="unitOptions">Lists the units of a permission scope.</param>
        /// <param name="principal">The signed-in user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organizations.</returns>
        private static async Task<Ok<NewsOrganizationsResponse>> HandleAsync(
            IRequestHandler<ListUnitOptions.Query, IReadOnlyList<UnitOption>> unitOptions,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
        {
            var options = await unitOptions.HandleAsync(new ListUnitOptions.Query(Permissions.News.Manage), cancellationToken);
            return TypedResults.Ok(new NewsOrganizationsResponse([.. options.Where(static unit => unit.IsOrganization)], principal.HasPermission(Permissions.Administration.AllOrganizations)));
        }
    }
}
