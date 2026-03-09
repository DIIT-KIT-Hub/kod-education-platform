namespace KOD.Application.Abstractions.Services.Emails;

/// <summary>
/// Defines methods for sending emails.
/// </summary>
public interface IEmailService
{
    #region Public methods

    /// <summary>
    /// Sends an email asynchronously to the specified recipient.
    /// </summary>
    /// <param name="to">The email address of the recipient.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="body">The body content of the email, typically HTML or plain text.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendEmailAsync(string to, string subject, string body);

    #endregion
}
