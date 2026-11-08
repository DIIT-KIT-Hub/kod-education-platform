using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Messaging;

/// <summary>
/// The delivery of domain events by the request pipeline: only after success, once, failures isolated.
/// </summary>
public sealed class DomainEventTests : IDisposable
{
    /// <summary>
    /// The services: the pipeline over the handlers of this file.
    /// </summary>
    private readonly ServiceProvider _provider;

    /// <summary>
    /// The scope of one request.
    /// </summary>
    private readonly IServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventTests"/> class.
    /// </summary>
    public DomainEventTests()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ICurrentUser, Anonymous>()
            .AddSingleton<Journal>()
            .AddScoped<Tracked>()
            .AddScoped<IDomainEventSource>(static provider => provider.GetRequiredService<Tracked>())
            .AddMessaging(typeof(DomainEventTests).Assembly)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
    }

    /// <summary>
    /// Gets what the handlers recorded.
    /// </summary>
    private Journal Log => _provider.GetRequiredService<Journal>();

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public async Task Events_of_entities_and_handlers_are_delivered_once_after_the_handler_succeeded()
    {
        var result = await SendAsync(new Act(Fail: false, Throw: false));

        Assert.True(result.IsSuccess);
        Assert.Equal(["handled", "entity:a", "handler:b", "reaction:b"], Log.Entries);

        Log.Entries.Clear();
        await SendAsync(new Act(Fail: false, Throw: false));
        Assert.Equal(["handled", "entity:a", "handler:b", "reaction:b"], Log.Entries);
    }

    [Fact]
    public async Task A_failed_result_or_an_exception_drops_the_events()
    {
        Assert.False((await SendAsync(new Act(Fail: true, Throw: false))).IsSuccess);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(new Act(Fail: false, Throw: true)));
        Assert.Equal(["handled", "handled"], Log.Entries);

        Log.Entries.Clear();
        await SendAsync(new Act(Fail: false, Throw: false));
        Assert.Equal(["handled", "entity:a", "handler:b", "reaction:b"], Log.Entries);
    }

    [Fact]
    public async Task A_failing_reaction_neither_fails_the_request_nor_stops_the_other_reactions()
    {
        var result = await SendAsync(new Act(Fail: false, Throw: false, ReactionFails: true));

        Assert.True(result.IsSuccess);
        Assert.Contains("reaction:b", Log.Entries);
    }

    [Fact]
    public async Task Events_raised_by_reactions_are_delivered_too()
    {
        await SendAsync(new Act(Fail: false, Throw: false, Chain: true));

        Assert.Equal("chained", Log.Entries[^1]);
    }

    /// <summary>
    /// Sends a request through the pipeline.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result.</returns>
    private Task<Result> SendAsync(Act request) => _scope.ServiceProvider.GetRequiredService<IRequestHandler<Act, Result>>().HandleAsync(request, TestContext.Current.CancellationToken);

    /// <summary>
    /// A command that changes an entity and raises events.
    /// </summary>
    /// <param name="Fail">Whether the handler answers with a failure.</param>
    /// <param name="Throw">Whether the handler throws.</param>
    /// <param name="ReactionFails">Whether the first reaction throws.</param>
    /// <param name="Chain">Whether a reaction raises a further event.</param>
    internal sealed record Act(bool Fail, bool Throw, bool ReactionFails = false, bool Chain = false) : ICommand<Result>;

    /// <summary>
    /// An event raised by an entity.
    /// </summary>
    /// <param name="Name">The name recorded.</param>
    internal sealed record EntityChanged(string Name) : IDomainEvent;

    /// <summary>
    /// An event raised by a handler.
    /// </summary>
    /// <param name="Name">The name recorded.</param>
    /// <param name="ReactionFails">Whether the first reaction throws.</param>
    /// <param name="Chain">Whether a reaction raises a further event.</param>
    internal sealed record HandlerNoticed(string Name, bool ReactionFails, bool Chain) : IDomainEvent;

    /// <summary>
    /// An event raised by a reaction.
    /// </summary>
    internal sealed record Chained : IDomainEvent;

    /// <summary>
    /// What the handlers did, in order.
    /// </summary>
    internal sealed class Journal
    {
        /// <summary>
        /// Gets the entries.
        /// </summary>
        public List<string> Entries { get; } = [];
    }

    /// <summary>
    /// An entity that raises an event when changed.
    /// </summary>
    internal sealed class Thing : Entity
    {
        /// <summary>
        /// Changes the entity.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        public void Change(string name) => Raise(new EntityChanged(name));
    }

    /// <summary>
    /// The tracked entities of a request (what the database context is in the application).
    /// </summary>
    internal sealed class Tracked : IDomainEventSource
    {
        /// <summary>
        /// Gets the entities.
        /// </summary>
        public List<Entity> Entities { get; } = [];

        /// <inheritdoc />
        public IReadOnlyList<IDomainEvent> TakeDomainEvents() => [.. Entities.SelectMany(static e => e.TakeDomainEvents())];
    }

    /// <summary>
    /// Handles <see cref="Act"/>.
    /// </summary>
    /// <param name="tracked">The tracked entities.</param>
    /// <param name="domainEvents">The events of the handler.</param>
    /// <param name="journal">The journal.</param>
    internal sealed class ActHandler(Tracked tracked, DomainEvents domainEvents, Journal journal) : IRequestHandler<Act, Result>
    {
        /// <inheritdoc />
        public Task<Result> HandleAsync(Act request, CancellationToken cancellationToken)
        {
            var thing = new Thing();
            tracked.Entities.Add(thing);
            thing.Change("a");
            domainEvents.Raise(new HandlerNoticed("b", request.ReactionFails, request.Chain));
            journal.Entries.Add("handled");

            if (request.Throw)
            {
                throw new InvalidOperationException("Broken.");
            }

            return Task.FromResult(request.Fail ? Result.Failure(Error.Validation("Ні.")) : Result.Success());
        }
    }

    /// <summary>
    /// Records the events.
    /// </summary>
    /// <param name="journal">The journal.</param>
    /// <param name="domainEvents">The events of the reaction.</param>
    internal sealed class Recorder(Journal journal, DomainEvents domainEvents) :
        IDomainEventHandler<EntityChanged>,
        IDomainEventHandler<HandlerNoticed>,
        IDomainEventHandler<Chained>
    {
        /// <inheritdoc />
        public Task HandleAsync(EntityChanged domainEvent, CancellationToken cancellationToken)
        {
            journal.Entries.Add($"entity:{domainEvent.Name}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task HandleAsync(HandlerNoticed domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent.ReactionFails)
            {
                throw new InvalidOperationException("The reaction broke.");
            }

            journal.Entries.Add($"handler:{domainEvent.Name}");
            if (domainEvent.Chain)
            {
                domainEvents.Raise(new Chained());
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task HandleAsync(Chained domainEvent, CancellationToken cancellationToken)
        {
            journal.Entries.Add("chained");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A second reaction to the same event.
    /// </summary>
    /// <param name="journal">The journal.</param>
    internal sealed class SecondRecorder(Journal journal) : IDomainEventHandler<HandlerNoticed>
    {
        /// <inheritdoc />
        public Task HandleAsync(HandlerNoticed domainEvent, CancellationToken cancellationToken)
        {
            journal.Entries.Add($"reaction:{domainEvent.Name}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Nobody is signed in (the request needs no permission).
    /// </summary>
    private sealed class Anonymous : ICurrentUser
    {
        /// <inheritdoc />
        public bool IsAuthenticated => false;

        /// <inheritdoc />
        public Guid? Id => null;

        /// <inheritdoc />
        public string? DisplayName => null;

        /// <inheritdoc />
        public string? IpAddress => null;

        /// <inheritdoc />
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Has(string permission) => false;
    }
}
