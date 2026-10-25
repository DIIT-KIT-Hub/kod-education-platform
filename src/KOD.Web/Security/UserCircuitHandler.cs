using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace KOD.Web.Security;

/// <summary>
/// Copies the authenticated principal into <see cref="CircuitUserAccessor"/> before every inbound circuit activity,
/// so scoped application services can resolve the current user inside interactive components.
/// </summary>
/// <param name="authenticationStateProvider">The authentication state provider of the circuit.</param>
/// <param name="accessor">The circuit user accessor.</param>
internal sealed class UserCircuitHandler(AuthenticationStateProvider authenticationStateProvider, CircuitUserAccessor accessor) : CircuitHandler
{
    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            accessor.User = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
            await next(context);
        };
}
