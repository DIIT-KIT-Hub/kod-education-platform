namespace KOD.Application.Exceptions.Statuses;

/// <summary>
/// Exception thrown when a requested entity or resource could not be found.
/// </summary>
public sealed class NotFoundException : Exception
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    public NotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class
    /// with a message indicating which entity was not found.
    /// </summary>
    /// <param name="entityName">The name of the entity or resource that was not found.</param>
    public NotFoundException(string entityName) : base($"{entityName} was not found.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class
    /// with a specified error message and a reference to the inner exception
    /// that caused this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

    #endregion
}
