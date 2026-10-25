using System.Diagnostics.CodeAnalysis;

namespace KOD.Application.Common.Results;

/// <summary>
/// The outcome of an operation that does not return a value.
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="error">The error, or <see langword="null"/> on success.</param>
    private Result(Error? error) => Error = error;

    /// <summary>
    /// Gets the error of a failed operation.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Converts an error into a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>The result.</returns>
    public static Result Success() => new(null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
}
