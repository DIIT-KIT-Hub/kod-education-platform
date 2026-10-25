using System.Diagnostics.CodeAnalysis;

namespace KOD.Application.Common.Results;

/// <summary>
/// The outcome of an operation that returns a value on success.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="error">The error.</param>
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets the value of a successful operation.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error of a failed operation.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded and <see cref="Value"/> is available.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Converts a value into a successful result.
    /// </summary>
    /// <param name="value">The value.</param>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Converts an error into a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));
}
