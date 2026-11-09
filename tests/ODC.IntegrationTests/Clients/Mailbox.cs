using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using MimeKit;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// The pickup folder the application writes its e-mails to (<c>Email:PickupDirectory</c>). E-mails leave through a
/// background queue, so readers wait for them; each e-mail is handed out once.
/// </summary>
/// <param name="folder">The folder.</param>
public sealed partial class Mailbox(string folder)
{
    /// <summary>
    /// How long to wait for an e-mail.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The files already handed out.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _taken = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the folder.
    /// </summary>
    public string Folder => folder;

    /// <summary>
    /// Waits for the next e-mail to an address.
    /// </summary>
    /// <param name="address">The recipient.</param>
    /// <returns>The e-mail.</returns>
    public async Task<MimeMessage> WaitForAsync(string address)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            if (Directory.Exists(folder))
            {
                foreach (var path in Directory.EnumerateFiles(folder, "*.eml").Order(StringComparer.Ordinal))
                {
                    if (_taken.ContainsKey(path))
                    {
                        continue;
                    }

                    if (await TryLoadAsync(path) is { } message
                        && message.To.Mailboxes.Any(m => string.Equals(m.Address, address, StringComparison.OrdinalIgnoreCase))
                        && _taken.TryAdd(path, true))
                    {
                        return message;
                    }
                }
            }

            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"No e-mail to {address} within {Timeout.TotalSeconds} s.");
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Whether an e-mail to an address is waiting in the folder (not handed out yet).
    /// </summary>
    /// <param name="address">The recipient.</param>
    /// <returns>Whether there is one.</returns>
    public bool HasMailTo(string address)
        => Directory.Exists(folder)
            && Directory.EnumerateFiles(folder, "*.eml")
                .Where(path => !_taken.ContainsKey(path))
                .Any(path => TryLoadAsync(path).GetAwaiter().GetResult()?.To.Mailboxes.Any(m => string.Equals(m.Address, address, StringComparison.OrdinalIgnoreCase)) == true);

    /// <summary>
    /// Waits for the next e-mail to an address and takes the six-digit code out of its text.
    /// </summary>
    /// <param name="address">The recipient.</param>
    /// <returns>The code.</returns>
    public async Task<string> WaitForCodeAsync(string address)
    {
        var message = await WaitForAsync(address);
        var match = SixDigits().Match(message.TextBody ?? string.Empty);
        Assert.True(match.Success, $"The e-mail to {address} has no code: {message.TextBody}");
        return match.Value;
    }

    /// <summary>
    /// Reads an e-mail file, or nothing while the application is still writing it.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The e-mail, or null.</returns>
    private static async Task<MimeMessage?> TryLoadAsync(string path)
    {
        try
        {
            return await MimeMessage.LoadAsync(path, TestContext.Current.CancellationToken);
        }
        catch (Exception exception) when (exception is IOException or FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Six digits on their own.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex(@"(?<!\d)\d{6}(?!\d)")]
    private static partial Regex SixDigits();
}
