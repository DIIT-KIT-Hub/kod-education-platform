namespace KOD.Infrastructure.Options.Emails;

/// <summary>
/// Represents configuration options for sending emails, including SMTP server, sender details, and credentials.
/// </summary>
internal sealed class EmailOptions
{
    #region Public fields

    /// <summary>
    /// Gets or sets the SMTP server address.
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the email sender.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the sender.
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authenticating with the SMTP server.
    /// </summary>
    public string Password { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the port number for connecting to the SMTP server.
    /// </summary>
    public int Port { get; set; }

    #endregion
}
