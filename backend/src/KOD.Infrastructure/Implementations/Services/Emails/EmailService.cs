using KOD.Application.Abstractions.Services.Emails;
using KOD.Infrastructure.Options.Emails;

using MailKit.Net.Smtp;

using Microsoft.Extensions.Options;

using MimeKit;

namespace KOD.Infrastructure.Implementations.Services.Emails;

/// <summary>
/// Implements <see cref="IEmailService"/> for sending emails using configured email options.
/// </summary>
internal sealed class EmailService : IEmailService
{
    #region Private fields

    /// <summary>
    /// The email configuration options.
    /// </summary>
    private readonly EmailOptions _emailOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class with the specified email options.
    /// </summary>
    /// <param name="emailOptions">The email configuration options.</param>
    public EmailService(IOptions<EmailOptions> emailOptions) => _emailOptions = emailOptions.Value;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailOptions.SenderName, _emailOptions.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailOptions.SmtpServer, _emailOptions.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailOptions.SenderEmail, _emailOptions.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch
        {
            throw;
        }
    }

    #endregion
}
