using System.Threading.Channels;
using KOD.Application.Abstractions;
using KOD.Application.Common.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Email;

/// <summary>
/// The in-memory queue of e-mails, emptied by <see cref="SmtpEmailSender"/>. Messages still queued when the application
/// stops are lost (notifications in the cabinet carry the same news).
/// </summary>
/// <param name="options">The e-mail settings.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class EmailOutbox(IOptions<EmailOptions> options, ILogger<EmailOutbox> logger) : IEmailOutbox
{
    /// <summary>
    /// The queue; when it is full the oldest message gives way.
    /// </summary>
    private readonly Channel<EmailMessage> _queue = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    /// <inheritdoc />
    public bool IsEnabled => options.Value.IsConfigured;

    /// <summary>
    /// Gets the queued messages.
    /// </summary>
    internal ChannelReader<EmailMessage> Reader => _queue.Reader;

    /// <inheritdoc />
    public void Enqueue(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsEnabled)
        {
            LogDisabled(logger);
            return;
        }

        _queue.Writer.TryWrite(message);
    }

    /// <summary>
    /// Logs an e-mail that was not queued because e-mail is not configured.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "E-mail is not configured (Email:Host, Email:FromAddress); an e-mail was not sent")]
    private static partial void LogDisabled(ILogger logger);
}
