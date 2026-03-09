namespace KOD.Domain.Exceptions.Auth;

/// <summary>
/// Represents an error that occurs when authentication credentials (such as refresh tokens or OTP codes) have errors.
/// </summary>
public sealed class CredentialsException : Exception
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialsException"/> class.
    /// </summary>
    public CredentialsException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialsException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CredentialsException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialsException"/> class
    /// with a specified error message and a reference to the inner exception that caused this error.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public CredentialsException(string message, Exception innerException) : base(message, innerException) { }

    #endregion
}
