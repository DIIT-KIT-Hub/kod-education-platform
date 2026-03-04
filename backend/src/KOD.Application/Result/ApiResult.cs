using Microsoft.AspNetCore.Http;

namespace KOD.Application.Result;

/// <summary>
/// Represents a standardized API response with status, message, and optional data.
/// </summary>
/// <typeparam name="T">The type of the data returned in the API result.</typeparam>
public sealed class ApiResult<T>
{
    #region Public fields

    /// <summary>
    /// Gets the HTTP status code of the API result.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the optional message associated with the API result.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the optional data returned by the API result.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Gets a value indicating whether the API result represents a success (status code 200).
    /// </summary>
    public bool IsSuccess => StatusCode == 200;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiResult{T}"/> class with the specified status code, message, and data.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the API result.</param>
    /// <param name="message">The optional message associated with the API result.</param>
    /// <param name="data">The optional data returned by the API result.</param>
    private ApiResult(int statusCode, string? message, T? data)
    {
        StatusCode = statusCode;
        Message = message;
        Data = data;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Creates a successful API result with optional data and message.
    /// </summary>
    /// <param name="data">The optional data to include.</param>
    /// <param name="message">The optional message to include.</param>
    /// <returns>An <see cref="ApiResult{T}"/> representing success.</returns>
    public static ApiResult<T> Success(T? data = default, string? message = null)
        => new(StatusCodes.Status200OK, message, data);

    /// <summary>
    /// Creates a failure API result with a specified status code and message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the failure.</param>
    /// <param name="message">The message describing the failure.</param>
    /// <returns>An <see cref="ApiResult{T}"/> representing failure.</returns>
    public static ApiResult<T> Failure(int statusCode, string message)
        => new(statusCode, message, default);

    #endregion
}
