using KOD.Domain.Localization;

namespace KOD.Application.Common.Results;

/// <summary>
/// Describes why an operation failed. <see cref="Message"/> is user-facing (in the interface language of the request).
/// </summary>
/// <param name="Code">A stable machine-readable code.</param>
/// <param name="Message">The user-facing message.</param>
/// <param name="Type">The error category.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">The user-facing message.</param>
    /// <param name="code">The machine-readable code.</param>
    /// <returns>The error.</returns>
    public static Error Validation(string message, string code = "validation") => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a not-found error.
    /// </summary>
    /// <param name="message">The user-facing message (a generic one when omitted).</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string? message = null) => new("not_found", message ?? Texts.T("Запис не знайдено."), ErrorType.NotFound);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="message">The user-facing message (a generic one when omitted).</param>
    /// <returns>The error.</returns>
    public static Error Forbidden(string? message = null) => new("forbidden", message ?? Texts.T("Недостатньо прав для виконання дії."), ErrorType.Forbidden);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="message">The user-facing message.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string message) => new("conflict", message, ErrorType.Conflict);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error Unauthorized() => new("unauthorized", Texts.T("Потрібна автентифікація."), ErrorType.Unauthorized);
}
