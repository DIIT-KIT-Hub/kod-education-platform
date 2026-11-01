namespace KOD.Application.Common.Email;

/// <summary>
/// E-mail settings (section <c>Email</c>). Without <see cref="Host"/> and <see cref="FromAddress"/> no e-mail is sent.
/// The password lives in user secrets or the environment, never in the settings files.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Gets or sets the SMTP server (for Gmail <c>smtp.gmail.com</c>).
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the SMTP port (587 with STARTTLS, 465 with TLS from the start).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets how the connection is protected.
    /// </summary>
    public EmailSecurity Security { get; set; } = EmailSecurity.StartTls;

    /// <summary>
    /// Gets or sets the SMTP account; empty when the server needs no sign-in.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the SMTP password (for Gmail an app password of the account).
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the sender address (for Gmail the account itself or one of its verified aliases).
    /// </summary>
    public string? FromAddress { get; set; }

    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    public string FromName { get; set; } = "КОД";

    /// <summary>
    /// Gets or sets the public address of the cabinet, used in the links of e-mails.
    /// </summary>
    public string SiteUrl { get; set; } = "https://localhost:7180";

    /// <summary>
    /// Gets a value indicating whether e-mail can be sent.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
