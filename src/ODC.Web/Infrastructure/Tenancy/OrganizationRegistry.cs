using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The registry of organizations (master database) kept in memory: up to a hundred rows, read again a minute after the
/// last read or at once after a change on this server (<see cref="Invalidate"/>).
/// </summary>
/// <param name="scopes">Creates the scope of the master database.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class OrganizationRegistry(IServiceScopeFactory scopes, TimeProvider timeProvider) : IDisposable
{
    /// <summary>
    /// How long a read of the registry is used.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Serializes the reads.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// The organizations of the last read, oldest first.
    /// </summary>
    private IReadOnlyList<Organization>? _organizations;

    /// <summary>
    /// When the last read expires.
    /// </summary>
    private DateTimeOffset _expires;

    /// <summary>
    /// Every organization, oldest first.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organizations.</returns>
    public async Task<IReadOnlyList<Organization>> AllAsync(CancellationToken cancellationToken)
    {
        if (_organizations is { } cached && timeProvider.GetUtcNow() < _expires)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_organizations is { } again && timeProvider.GetUtcNow() < _expires)
            {
                return again;
            }

            await using var scope = scopes.CreateAsyncScope();
            var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var organizations = await master.Organizations.AsNoTracking().OrderBy(static o => o.CreatedAt).ThenBy(static o => o.Code).ToListAsync(cancellationToken);
            _organizations = organizations;
            _expires = timeProvider.GetUtcNow() + Lifetime;
            return organizations;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Finds an organization by code (case-insensitive).
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or null.</returns>
    public async Task<Organization?> FindByCodeAsync(string code, CancellationToken cancellationToken)
        => (await AllAsync(cancellationToken)).FirstOrDefault(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Finds an organization by identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or null.</returns>
    public async Task<Organization?> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await AllAsync(cancellationToken)).FirstOrDefault(o => o.Id == id);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Forgets the last read (after an organization was added or changed).
    /// </summary>
    public void Invalidate() => _expires = DateTimeOffset.MinValue;
}
