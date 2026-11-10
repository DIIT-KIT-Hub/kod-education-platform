using Microsoft.AspNetCore.SignalR;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Gives every hub call the organization of its connection: SignalR runs each call in a scope of its own, outside the
/// request pipeline, so <see cref="OrganizationMiddleware"/> leaves its choice on the connection's request
/// (<see cref="OrganizationMiddleware.ContextItem"/>) and this filter enters it into the call's scope. Hubs resolve
/// services that use the organization's database inside their methods, not in their constructors: the hub is created
/// before the filters run.
/// </summary>
public sealed class OrganizationHubFilter : IHubFilter
{
    /// <inheritdoc />
    public ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);
        Enter(invocationContext.Context, invocationContext.ServiceProvider);
        return next(invocationContext);
    }

    /// <inheritdoc />
    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        Enter(context.Context, context.ServiceProvider);
        return next(context);
    }

    /// <inheritdoc />
    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        Enter(context.Context, context.ServiceProvider);
        return next(context, exception);
    }

    /// <summary>
    /// Enters the organization of the connection's request into the scope of a call.
    /// </summary>
    /// <param name="caller">The connection.</param>
    /// <param name="services">The services of the call.</param>
    private static void Enter(HubCallerContext caller, IServiceProvider services)
    {
        if (caller.GetHttpContext()?.Items[OrganizationMiddleware.ContextItem] is OrganizationContext { Current: { } organization } chosen)
        {
            services.GetRequiredService<OrganizationContext>().Enter(organization, chosen.Access);
        }
    }
}
