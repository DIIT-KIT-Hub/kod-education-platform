namespace ODC.Web.Endpoints;

/// <summary>
/// The HTTP side of a slice: maps one route of the REST API. Implementations are found at startup
/// (<see cref="ApiEndpoints.MapApiEndpoints"/>) and created with their parameterless constructor.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the route.
    /// </summary>
    /// <param name="api">The <c>/api/v1</c> group: signed-in users only, antiforgery checked for unsafe methods.</param>
    void Map(IEndpointRouteBuilder api);
}
