using System.Collections.Concurrent;
using System.Reflection;
using ODC.Web.Security;

namespace ODC.Web.Messaging;

/// <summary>
/// The permission attributes of request types, read once per type.
/// </summary>
internal static class RequiredPermissions
{
    /// <summary>
    /// The attributes read so far.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>[]> Cache = new();

    /// <summary>
    /// Returns the permission sets of a request type (each set: any one of its permissions is enough).
    /// </summary>
    /// <param name="type">The request type.</param>
    /// <returns>The sets; empty when the request needs no permission.</returns>
    public static IReadOnlyList<string>[] Of(Type type)
        => Cache.GetOrAdd(type, static t => [.. t.GetCustomAttributes<RequirePermissionAttribute>(inherit: true).Select(static a => a.Permissions)]);
}
