using ODC.Web.Endpoints;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Features.Admin.Organizations;

/// <summary>
/// The routes and texts shared by the slices of the registry of organizations (platform administrators only).
/// </summary>
internal static class OrganizationRules
{
    /// <summary>
    /// The route group of the registry (<c>administration.all</c>: the platform's power).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group <c>/api/v1/organizations</c>.</returns>
    public static RouteGroupBuilder MapOrganizations(this IEndpointRouteBuilder api)
        => api.MapGroup("organizations").RequirePermission(Permissions.Administration.AllOrganizations).WithTags("Organizations");

    /// <summary>
    /// Describes an organization in the audit trail.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The description.</returns>
    public static string Describe(Organization organization) => $"«{organization.Name}» ({organization.Code}, база {organization.DatabaseName})";
}
