using System.Collections.Concurrent;

namespace ODC.Web.Messaging;

/// <summary>
/// Short, readable names of request types for logs: <c>SubmitTicket.Command</c> instead of the full nested type name.
/// </summary>
internal static class RequestNames
{
    /// <summary>
    /// The names computed so far.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, string> Names = new();

    /// <summary>
    /// Returns the short name of a request type.
    /// </summary>
    /// <param name="type">The request type.</param>
    /// <returns>The name.</returns>
    public static string Of(Type type) => Names.GetOrAdd(type, static t => t.DeclaringType is { } slice ? $"{slice.Name}.{t.Name}" : t.Name);
}
