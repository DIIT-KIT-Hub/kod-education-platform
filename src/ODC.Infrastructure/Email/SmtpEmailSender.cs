using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ODC.Application.Common.Email;

namespace ODC.Infrastructure.Email;

/// <summary>
/// Sends the queued e-mails over SMTP (MailKit): the messages waiting together go through one connection; a failed
/// connection is tried again a few times, then the messages are logged as not sent. Addresses are never logged. With
/// <see cref="EmailOptions.PickupDirectory"/> the messages are written as <c>.eml</c> files instead (development, tests).
/// </summary>
/// <param name="outbox">The queue.</param>
/// <param name="options">The e-mail settings.</param>
/// <param name="environment">The host environment (the content root of the pickup folder).</param>
/// <param name="logger">The logger.</param>
internal sealed partial class SmtpEmailSender(EmailOutbox outbox, IOptions<EmailOptions> options, IHostEnvironment environment, ILogger<SmtpEmailSender> logger) : BackgroundService
{
    /// <summary>
    /// The most messages sent through one connection.
    /// </summary>
    private const int BatchSize = 50;

    /// <summary>
    /// How many times a connection is tried.
    /// </summary>
    private const int Attempts = 3;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.IsConfigured)
        {
            LogNotConfigured(logger);
        }

        var reader = outbox.Reader;
        while (await reader.WaitToReadAsync(stoppingToken))
        {
            var batch = new List<EmailMessage>();
            while (batch.Count < BatchSize && reader.TryRead(out var message))
            {
                batch.Add(message);
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await SendAsync(batch, stoppingToken);
                    break;
                }
                catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
                {
                    if (attempt == Attempts)
                    {
                        LogFailed(logger, batch.Count, exception);
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10 * attempt), stoppingToken);
                }
            }
        }
    }

    /// <summary>
    /// Logs that e-mail is off.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "E-mail is not configured (Email:Host, Email:FromAddress): no e-mails are sent")]
    private static partial void LogNotConfigured(ILogger logger);

    /// <summary>
    /// Logs sent e-mails.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of e-mails.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Count} e-mail(s)")]
    private static partial void LogSent(ILogger logger, int count);

    /// <summary>
    /// Logs e-mails that could not be sent.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of e-mails.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "{Count} e-mail(s) were not sent")]
    private static partial void LogFailed(ILogger logger, int count, Exception exception);

    /// <summary>
    /// Logs one e-mail the server refused (the others of the batch still go).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The mail server refused an e-mail")]
    private static partial void LogRefused(ILogger logger, Exception exception);

    /// <summary>
    /// Builds the MIME message.
    /// </summary>
    /// <param name="message">The e-mail.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The MIME message.</returns>
    private static MimeMessage ToMime(EmailMessage message, EmailOptions settings)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, string.IsNullOrWhiteSpace(settings.FromAddress) ? "no-reply@localhost" : settings.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.To));
        mime.Subject = message.Subject;

        // RFC 3834: an automatic message, so mail programs do not send out-of-office answers to it.
        mime.Headers.Add("Auto-Submitted", "auto-generated");
        mime.Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody();
        return mime;
    }

    /// <summary>
    /// Sends messages through one connection.
    /// </summary>
    /// <param name="batch">The messages; the sent ones are removed (a retry sends only the rest).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when sent.</returns>
    private async Task SendAsync(List<EmailMessage> batch, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.UsesPickupDirectory)
        {
            await WriteToPickupDirectoryAsync(batch, settings, cancellationToken);
            return;
        }

        using var client = new SmtpClient();
        var security = settings.Security switch
        {
            EmailSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            EmailSecurity.None => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls,
        };
        await client.ConnectAsync(settings.Host ?? string.Empty, settings.Port, security, cancellationToken);
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken);
        }

        var sent = 0;
        while (batch.Count > 0)
        {
            try
            {
                await client.SendAsync(ToMime(batch[0], settings), cancellationToken);
                sent++;
            }
            catch (Exception exception) when (exception is SmtpCommandException or ParseException or FormatException)
            {
                // A wrong address: this message is dropped, the connection is still good.
                LogRefused(logger, exception);
            }

            batch.RemoveAt(0);
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
        LogSent(logger, sent);
    }

    /// <summary>
    /// Writes messages into the pickup folder, one <c>.eml</c> file each.
    /// </summary>
    /// <param name="batch">The messages; the written ones are removed.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when written.</returns>
    private async Task WriteToPickupDirectoryAsync(List<EmailMessage> batch, EmailOptions settings, CancellationToken cancellationToken)
    {
        var folder = Path.GetFullPath(settings.PickupDirectory!, environment.ContentRootPath);
        Directory.CreateDirectory(folder);
        var written = 0;
        while (batch.Count > 0)
        {
            var path = Path.Combine(folder, FormattableString.Invariant($"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.eml"));
            await ToMime(batch[0], settings).WriteToAsync(path, cancellationToken);
            batch.RemoveAt(0);
            written++;
        }

        LogSent(logger, written);
    }
}
