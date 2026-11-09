using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using ODC.Web.Infrastructure.Email;
using ODC.Web.Shared.Email;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The e-mail outbox (requests never wait for the mail server; nothing is queued without settings) and the background
/// sender writing to a pickup directory (what developers and the activation tests read).
/// </summary>
public sealed partial class EmailTests : IDisposable
{
    /// <summary>
    /// The content root of the test.
    /// </summary>
    private readonly TemporaryFolder _folder = new();

    /// <summary>
    /// Gets the cancellation token of the test.
    /// </summary>
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Without_a_server_or_sender_address_e_mail_is_off_and_nothing_is_queued()
    {
        var outbox = Outbox(new EmailOptions { Host = "smtp.example.com" });

        outbox.Enqueue(Message("a@example.com"));

        Assert.False(outbox.IsEnabled);
        Assert.False(outbox.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData("smtp.example.com", "cabinet@example.com", null, true)]
    [InlineData(null, null, "mail", true)]
    [InlineData(" ", "cabinet@example.com", null, false)]
    [InlineData("smtp.example.com", " ", null, false)]
    [InlineData(null, null, " ", false)]
    public void E_mail_is_on_with_a_server_and_sender_or_a_pickup_directory(string? host, string? from, string? pickup, bool enabled)
    {
        Assert.Equal(enabled, Outbox(new EmailOptions { Host = host, FromAddress = from, PickupDirectory = pickup }).IsEnabled);
    }

    [Fact]
    public void Messages_are_queued_in_order()
    {
        var outbox = Outbox(Pickup());

        outbox.Enqueue(Message("a@example.com"));
        outbox.Enqueue(Message("b@example.com"));

        Assert.True(outbox.Reader.TryRead(out var first));
        Assert.True(outbox.Reader.TryRead(out var second));
        Assert.Equal(["a@example.com", "b@example.com"], [first!.To, second!.To]);
    }

    [Fact]
    public void A_full_queue_drops_the_oldest_message_instead_of_blocking_a_request()
    {
        var outbox = Outbox(Pickup());

        for (var i = 0; i < 1001; i++)
        {
            outbox.Enqueue(Message($"{i}@example.com"));
        }

        Assert.True(outbox.Reader.TryRead(out var oldest));
        Assert.Equal("1@example.com", oldest!.To);
        Assert.Equal(999, outbox.Reader.Count);
    }

    [Fact]
    public void A_null_message_is_a_programming_error()
    {
        Assert.Throws<ArgumentNullException>(() => Outbox(Pickup()).Enqueue(null!));
    }

    [Fact]
    public async Task The_sender_writes_each_message_as_an_automatic_e_mail_into_the_pickup_directory()
    {
        var options = Pickup();
        options.FromAddress = "cabinet@example.com";
        options.FromName = "Кабінет";
        var outbox = Outbox(options);
        outbox.Enqueue(new EmailMessage("ivan@example.com", "Іван Петренко", "Код активації", "<p>Код: <b>123456</b></p>", "Код: 123456"));
        outbox.Enqueue(Message("olena@example.com"));

        var files = await SendAsync(outbox, options, 2);

        var messages = files.Select(static f => MimeMessage.Load(f, Token)).ToList();
        var first = messages.Single(static m => m.To.Mailboxes.Single().Address == "ivan@example.com");
        Assert.Equal("Іван Петренко", first.To.Mailboxes.Single().Name);
        Assert.Equal("cabinet@example.com", first.From.Mailboxes.Single().Address);
        Assert.Equal("Кабінет", first.From.Mailboxes.Single().Name);
        Assert.Equal("Код активації", first.Subject);
        Assert.Equal("auto-generated", first.Headers["Auto-Submitted"]);
        Assert.Contains("<b>123456</b>", first.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Код: 123456", first.TextBody, StringComparison.Ordinal);
        Assert.All(files, static f => Assert.Matches(@"^\d{8}-\d{6}-\d{3}-[0-9a-f]{32}\.eml$", Path.GetFileName(f)));
    }

    [Fact]
    public async Task Without_a_sender_address_the_pickup_e_mail_comes_from_no_reply()
    {
        var options = Pickup();
        var outbox = Outbox(options);
        outbox.Enqueue(Message("a@example.com"));

        var file = Assert.Single(await SendAsync(outbox, options, 1));

        Assert.Equal("no-reply@localhost", MimeMessage.Load(file, Token).From.Mailboxes.Single().Address);
    }

    [Fact]
    public async Task A_relative_pickup_directory_is_resolved_against_the_content_root()
    {
        var options = Pickup();
        var outbox = Outbox(options);
        outbox.Enqueue(Message("a@example.com"));

        var file = Assert.Single(await SendAsync(outbox, options, 1));

        Assert.StartsWith(Path.Combine(_folder.Path, "mail"), file, StringComparison.Ordinal);
    }

    /// <summary>
    /// A plain message.
    /// </summary>
    /// <param name="to">The recipient.</param>
    /// <returns>The message.</returns>
    private static EmailMessage Message(string to) => new(to, "Отримувач", "Тема", "<p>Текст</p>", "Текст");

    /// <summary>
    /// An outbox with the given settings.
    /// </summary>
    /// <param name="options">The settings.</param>
    /// <returns>The outbox.</returns>
    private static EmailOutbox Outbox(EmailOptions options) => new(Options.Create(options), NullLogger<EmailOutbox>.Instance);

    /// <summary>
    /// Settings that write e-mails into the folder <c>mail</c> of the content root.
    /// </summary>
    /// <returns>The settings.</returns>
    private static EmailOptions Pickup() => new() { PickupDirectory = "mail" };

    /// <summary>
    /// Runs the sender until it reports the expected number of e-mails written (a file that merely exists may still be
    /// being written).
    /// </summary>
    /// <param name="outbox">The outbox with the queued messages.</param>
    /// <param name="options">The settings.</param>
    /// <param name="count">The expected number of files.</param>
    /// <returns>The written files.</returns>
    private async Task<string[]> SendAsync(EmailOutbox outbox, EmailOptions options, int count)
    {
        var log = new SentLog();
        using var sender = new SmtpEmailSender(outbox, Options.Create(options), _folder.Environment, log);
        await sender.StartAsync(Token);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (log.Sent < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Token);
        }

        await sender.StopAsync(Token);
        var folder = Path.Combine(_folder.Path, "mail");
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.eml") : [];
        Assert.Equal(count, files.Length);
        return files;
    }

    /// <summary>
    /// A logger of the sender that counts the e-mails it reports as sent.
    /// </summary>
    private sealed partial class SentLog : ILogger<SmtpEmailSender>
    {
        /// <summary>
        /// The number of e-mails sent so far.
        /// </summary>
        private int _sent;

        /// <summary>
        /// Gets the number of e-mails sent so far.
        /// </summary>
        public int Sent => Volatile.Read(ref _sent);

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            var match = SentPattern().Match(formatter(state, exception));
            if (match.Success)
            {
                Interlocked.Add(ref _sent, int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// The message of a sent batch.
        /// </summary>
        /// <returns>The pattern.</returns>
        [GeneratedRegex(@"^Sent (d+) e-mail", RegexOptions.CultureInvariant)]
        private static partial Regex SentPattern();
    }
}
