namespace KOD.Domain.Enums.Errors;

/// <summary>
/// Represents types of errors that can occur in the application.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Conflict = 5,
    Gone = 6,
    Failure = 7
}
