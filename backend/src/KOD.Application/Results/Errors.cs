using KOD.Domain.Enums.Errors;

namespace KOD.Application.Results;

/// <summary>
/// Provides factory methods for creating common <see cref="Error"/> instances.
/// </summary>
public static class Errors
{
    #region Public methods

    /// <summary>
    /// Creates a validation error with a custom message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <returns>An <see cref="Error"/> representing a validation failure.</returns>
    public static Error Validation(string message) =>
        new(ErrorType.Validation, "Validation.Error", message);

    /// <summary>
    /// Creates a not found error for a specific entity.
    /// </summary>
    /// <param name="entity">The entity name that was not found.</param>
    /// <returns>An <see cref="Error"/> representing a not found error.</returns>
    public static Error NotFound(string entity) =>
        new(ErrorType.NotFound, $"{entity}.NotFound", $"{entity} was not found");

    /// <summary>
    /// Creates an unauthorized error with an optional message.
    /// </summary>
    /// <param name="message">The error message (default is "Unauthorized").</param>
    /// <returns>An <see cref="Error"/> representing an unauthorized access error.</returns>
    public static Error Unauthorized(string message = "Unauthorized") =>
        new(ErrorType.Unauthorized, "Auth.Unauthorized", message);

    /// <summary>
    /// Creates a forbidden error with an optional message.
    /// </summary>
    /// <param name="message">The error message (default is "Forbidden").</param>
    /// <returns>An <see cref="Error"/> representing a forbidden access error.</returns>
    public static Error Forbidden(string message = "Forbidden") =>
        new(ErrorType.Forbidden, "Auth.Forbidden", message);

    /// <summary>
    /// Creates a conflict error with a custom message.
    /// </summary>
    /// <param name="message">The conflict error message.</param>
    /// <returns>An <see cref="Error"/> representing a conflict.</returns>
    public static Error Conflict(string message) =>
        new(ErrorType.Conflict, "Conflict.Error", message);

    /// <summary>
    /// Creates a gone error with a custom message.
    /// </summary>
    /// <param name="message">The gone error message.</param>
    /// <returns>An <see cref="Error"/> representing a resource that is gone.</returns>
    public static Error Gone(string message) =>
        new(ErrorType.Gone, "Gone.Error", message);

    /// <summary>
    /// Creates a general failure error with a custom message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>An <see cref="Error"/> representing a general failure.</returns>
    public static Error Failure(string message) =>
        new(ErrorType.Failure, "Failure.Error", message);

    #endregion
}
