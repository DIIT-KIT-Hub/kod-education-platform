using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Shared.Auditing;

namespace ODC.UnitTests.Application;

/// <summary>
/// An audit logger that records the events.
/// </summary>
internal sealed class RecordingAudit : IAuditLogger
{
    /// <summary>Gets the events in order (action and details).</summary>
    public List<(string Action, string? Details)> Entries { get; } = [];

    /// <summary>
    /// Gets the recorded actions.
    /// </summary>
    /// <returns>The actions.</returns>
    public IReadOnlyList<string> Actions() => [.. Entries.Select(static e => e.Action)];

    /// <inheritdoc />
    public void Add(string action, string? entityType = null, string? entityId = null, string? details = null) => Entries.Add((action, details));

    /// <inheritdoc />
    public Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken)
    {
        Entries.Add((action, details));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers this logger (and replaces the default one).
    /// </summary>
    /// <param name="services">The services.</param>
    public void Register(IServiceCollection services) => services.AddSingleton<IAuditLogger>(this);
}
