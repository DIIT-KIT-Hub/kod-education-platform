namespace KOD.Application.Results;

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    #region Public fields

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Contains the error information if the operation failed; <see cref="Error.None"/> if successful.
    /// </summary>
    public Error Error { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <param name="error">The error associated with a failed operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when success result has an error or failure result has no error.
    /// </exception>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Success result cannot have error");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failure result must have error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Creates a successful result without a value.
    /// </summary>
    /// <returns>A success <see cref="Result"/> instance.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A failure <see cref="Result"/> instance.</returns>
    public static Result Failure(Error error) => new(false, error);

    #endregion
}

/// <summary>
/// Represents the result of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public sealed class Result<T> : Result
{
    #region Public fields

    /// <summary>
    /// The value of the operation if successful; <c>null</c> if failed.
    /// </summary>
    public T? Value { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a value, success flag, and error.
    /// </summary>
    /// <param name="value">The value of the operation if successful; otherwise <c>null</c>.</param>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error associated with a failed operation.</param>
    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => Value = value;

    #endregion

    #region Public methods

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value returned by the successful operation.</param>
    /// <returns>A success <see cref="Result{T}"/> instance containing the value.</returns>
    public static Result<T> Success(T value) =>
        new(value, true, Error.None);

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A failure <see cref="Result{T}"/> instance with no value.</returns>
    public static new Result<T> Failure(Error error) =>
        new(default, false, error);

    #endregion
}
