using System.Reflection;
using ODC.Web.Security;

namespace ODC.Web.Endpoints;

/// <summary>
/// Maps the REST API from the slices and the conventions endpoints share.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// The base address of the REST API.
    /// </summary>
    public const string Base = "/api/v1";

    /// <param name="app">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps every <see cref="IEndpoint"/> of the assembly under <c>/api/v1</c>. The group is secure by default:
        /// signed-in users only (anonymous endpoints opt out with <c>AllowAnonymous</c>) and the antiforgery token is
        /// required for every state-changing request.
        /// </summary>
        /// <param name="assembly">The assembly with the slices.</param>
        /// <returns>The API group.</returns>
        public RouteGroupBuilder MapApiEndpoints(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            var api = app.MapGroup(Base)
                .RequireAuthorization()
                .RequireAntiforgeryToken();

            foreach (var type in FindEndpoints(assembly))
            {
                ((IEndpoint)Activator.CreateInstance(type)!).Map(api);
            }

            return api;
        }
    }

    /// <summary>
    /// Finds the endpoint classes of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The endpoint types.</returns>
    public static IEnumerable<Type> FindEndpoints(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetTypes().Where(static t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(t));
    }

    /// <summary>
    /// Requires the antiforgery token (the <c>X-CSRF-TOKEN</c> header) for every state-changing request. Replaces the
    /// framework's form check, which would ask for the token in the form instead of the header.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint or group builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequireAntiforgeryToken<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder
            .AddEndpointFilter<TBuilder, AntiforgeryEndpointFilter>()
            .WithMetadata(AntiforgeryTokenRequired.Instance)
            .DisableAntiforgery();

    /// <summary>
    /// Requires one of the permissions (the same policy as <see cref="RequirePermissionAttribute"/>).
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint or group builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="permissions">The permissions; any one of them is enough.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(new RequirePermissionAttribute(permissions));
}
