using System.Reflection;

namespace ODC.Web.Messaging;

/// <summary>
/// Registers the request handlers and the domain event handlers of an assembly. Our own replacement for MediatR
/// (commercial since July 2025): every <see cref="IRequestHandler{TRequest, TResponse}"/> implementation is found once at
/// startup and registered as a scoped service; the interface resolves to
/// <c>Logging(Authorization(DomainEvents(handler)))</c>. The decorator factories are compiled once per request type, so
/// resolving a handler costs no reflection.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// The pipeline steps from the innermost (closest to the handler) to the outermost.
    /// </summary>
    private static readonly Type[] Pipeline = [typeof(DomainEventsDecorator<,>), typeof(AuthorizationDecorator<,>), typeof(LoggingDecorator<,>)];

    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds every request handler of the assembly wrapped in the pipeline, every domain event handler, and the
        /// delivery of domain events.
        /// </summary>
        /// <param name="assembly">The assembly with the handlers.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddMessaging(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            services.AddScoped<DomainEvents>();
            services.AddScoped<IDomainEventSource>(static provider => provider.GetRequiredService<DomainEvents>());
            services.AddScoped<DomainEventDispatcher>();

            foreach (var (handler, contract) in FindEventHandlers(assembly))
            {
                services.AddScoped(contract, handler);
            }

            foreach (var (handler, contract) in FindHandlers(assembly))
            {
                services.AddScoped(handler);
                var arguments = contract.GetGenericArguments();
                var factories = Pipeline
                    .Select(step => ActivatorUtilities.CreateFactory(step.MakeGenericType(arguments), [contract]))
                    .ToArray();

                services.AddScoped(contract, provider =>
                {
                    var current = provider.GetRequiredService(handler);
                    foreach (var factory in factories)
                    {
                        current = factory(provider, [current]);
                    }

                    return current;
                });
            }

            return services;
        }
    }

    /// <summary>
    /// Finds the request handler classes and the handler interfaces they implement.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The pairs of handler class and closed <see cref="IRequestHandler{TRequest, TResponse}"/>.</returns>
    public static IEnumerable<(Type Handler, Type Contract)> FindHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return from type in assembly.GetTypes()
               where type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                     && !Pipeline.Any(step => type.IsGenericType && type.GetGenericTypeDefinition() == step)
               from contract in type.GetInterfaces()
               where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
               select (type, contract);
    }

    /// <summary>
    /// Finds the domain event handler classes and the handler interfaces they implement (a class may handle several
    /// events).
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The pairs of handler class and closed <see cref="IDomainEventHandler{TEvent}"/>.</returns>
    public static IEnumerable<(Type Handler, Type Contract)> FindEventHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return from type in assembly.GetTypes()
               where type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
               from contract in type.GetInterfaces()
               where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)
               select (type, contract);
    }
}
