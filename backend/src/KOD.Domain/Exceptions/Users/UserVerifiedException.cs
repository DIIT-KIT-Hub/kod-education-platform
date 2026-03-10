namespace KOD.Domain.Exceptions.Users;

/// <summary>
/// Represents an error that occurs when an operation is performed on a user
/// that has already been verified, but the operation requires an unverified user.
/// </summary>
public sealed class UserVerifiedException : Exception
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UserVerifiedException"/> class
    /// with a default error message.
    /// </summary>
    public UserVerifiedException() : base("User already verified.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserVerifiedException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UserVerifiedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserVerifiedException"/> class
    /// with a specified error message and a reference to the inner exception that caused this error.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public UserVerifiedException(string message, Exception innerException) : base(message, innerException) { }

    #endregion
}
