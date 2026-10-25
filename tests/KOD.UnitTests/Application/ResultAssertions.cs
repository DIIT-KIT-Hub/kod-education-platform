using KOD.Application.Common.Results;

namespace KOD.UnitTests.Application;

/// <summary>
/// Assertions on use case results.
/// </summary>
internal static class ResultAssertions
{
    /// <summary>
    /// Asserts that the result is successful and returns its value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The value.</returns>
    public static T Ok<T>(this Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }

    /// <summary>
    /// Returns the kind of the failure, or <see langword="null"/> for a success.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The error type.</returns>
    public static ErrorType? Failure(this Result result) => result.Error?.Type;

    /// <summary>
    /// Returns the kind of the failure, or <see langword="null"/> for a success.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The error type.</returns>
    public static ErrorType? Failure<T>(this Result<T> result) => result.Error?.Type;
}
