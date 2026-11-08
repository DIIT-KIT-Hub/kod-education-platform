using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// Builds a failed answer of a handler from an error, when the answer type is <see cref="Result"/> or
/// <see cref="Result{T}"/>; the delegate is built once per answer type.
/// </summary>
/// <typeparam name="TResponse">The answer type.</typeparam>
internal static class ResultFailure<TResponse>
{
    /// <summary>
    /// Creates the failed answer, or null when the answer type cannot carry an error.
    /// </summary>
    public static readonly Func<Error, TResponse>? Create = Build();

    /// <summary>
    /// Finds the failure factory of the answer type.
    /// </summary>
    /// <returns>The factory or null.</returns>
    private static Func<Error, TResponse>? Build()
    {
        var type = typeof(TResponse);
        if (type == typeof(Result))
        {
            return static error => (TResponse)(object)Result.Failure(error);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = type.GetMethod(nameof(Result<>.Failure), [typeof(Error)])!;
            return method.CreateDelegate<Func<Error, TResponse>>();
        }

        return null;
    }
}
