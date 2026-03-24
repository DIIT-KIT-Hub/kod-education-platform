using KOD.Domain.Enums.Errors;

namespace KOD.Application.Results;

/// <summary>
/// Represents an error with type, code, and message.
/// </summary>
public sealed record Error
{
    #region Public fields

    /// <summary>
    /// The type of the error.
    /// </summary>
    public ErrorType ErrorType { get; init; }

    /// <summary>
    /// A unique error code identifying the error.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// A descriptive error message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record.
    /// </summary>
    /// <param name="errorType">The type of the error.</param>
    /// <param name="code">A unique code identifying the error.</param>
    /// <param name="message">The descriptive message of the error.</param>
    public Error(ErrorType errorType, string code, string message)
    {
        ErrorType = errorType;
        Code = code;
        Message = message;
    }

    #endregion
}
