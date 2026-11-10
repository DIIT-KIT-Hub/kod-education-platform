using Microsoft.Extensions.Caching.Memory;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Keeps who works where and with which permissions for a minute (every request needs them). Any change of positions,
/// individual permissions, roles or memberships on this server starts a new generation, so the next request reads them
/// again; another server sees the change within the minute.
/// </summary>
/// <param name="cache">The memory cache.</param>
public sealed class AccessCache(IMemoryCache cache)
{
    /// <summary>
    /// How long an entry is used.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The current generation.
    /// </summary>
    private long _generation;

    /// <summary>
    /// Gets a cached value or computes it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key (without the generation).</param>
    /// <param name="compute">Computes the value.</param>
    /// <returns>The value.</returns>
    public async Task<T> GetAsync<T>(string key, Func<Task<T>> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var full = $"odc:access:{Interlocked.Read(ref _generation)}:{key}";
        if (cache.TryGetValue(full, out T? found) && found is not null)
        {
            return found;
        }

        var value = await compute();
        cache.Set(full, value, Lifetime);
        return value;
    }

    /// <summary>
    /// Forgets everything (after positions, permissions, roles or memberships changed).
    /// </summary>
    public void Changed() => Interlocked.Increment(ref _generation);
}
