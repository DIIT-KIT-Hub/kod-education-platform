namespace ODC.Web.Shared.Email;

/// <summary>
/// Sends e-mails in the background: the request that causes them never waits for the mail server, and a failure is
/// logged (after retries) instead of failing the request.
/// </summary>
public interface IEmailOutbox
{
    /// <summary>
    /// Gets a value indicating whether e-mail is configured; otherwise queued messages are dropped.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Queues an e-mail.
    /// </summary>
    /// <param name="message">The e-mail.</param>
    void Enqueue(EmailMessage message);
}
