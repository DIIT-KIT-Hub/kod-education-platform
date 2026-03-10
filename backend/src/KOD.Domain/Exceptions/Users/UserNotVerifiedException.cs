namespace KOD.Domain.Exceptions.Users;

/// <summary>
/// Represents an error that occurs when an operation requires a verified user, but the user account has not yet been verified.
/// </summary>
public sealed class UserNotVerifiedException : Exception
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotVerifiedException"/> class
    /// with a default error message.
    /// </summary>
    public UserNotVerifiedException() : base("User is not verified.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotVerifiedException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UserNotVerifiedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotVerifiedException"/> class
    /// with a specified error message and a reference to the inner exception that caused this error.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public UserNotVerifiedException(string message, Exception innerException) : base(message, innerException) { }

    #endregion
}
