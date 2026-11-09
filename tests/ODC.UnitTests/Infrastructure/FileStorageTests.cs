using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Infrastructure.Media;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Media;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The private file storage (documents, signatures, attachments) and the image storage on the local disk: keys that
/// cannot leave the storage root, size limits, hashes, cleanup of partial files.
/// </summary>
public sealed class FileStorageTests : IDisposable
{
    /// <summary>
    /// The content root of the test.
    /// </summary>
    private readonly TemporaryFolder _folder = new();

    /// <summary>
    /// The clock of the storage (fixes the year/month folders).
    /// </summary>
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 3, 9, 10, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Gets the cancellation token of the test.
    /// </summary>
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task A_saved_file_gets_a_dated_key_its_size_and_SHA_256_and_reads_back()
    {
        var storage = Files();
        byte[] content = [.. Enumerable.Range(0, 200_000).Select(static i => (byte)i)];

        var blob = await storage.SaveAsync(new MemoryStream(content), "documents", 1_000_000, Token);

        Assert.Matches("^documents/2026/03/[0-9a-f]{32}$", blob.StorageKey);
        Assert.Equal(content.Length, blob.Size);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), blob.Sha256);
        await using var read = await storage.OpenReadAsync(blob.StorageKey, Token);
        var copy = new MemoryStream();
        await read.CopyToAsync(copy, Token);
        Assert.Equal(content, copy.ToArray());
        Assert.StartsWith("App_Data/storage/documents/2026/03/", Assert.Single(_folder.Files()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Two_files_never_share_a_key()
    {
        var storage = Files();

        var first = await storage.SaveAsync(new MemoryStream([1]), "chats", 10, Token);
        var second = await storage.SaveAsync(new MemoryStream([1]), "chats", 10, Token);

        Assert.NotEqual(first.StorageKey, second.StorageKey);
        Assert.Equal(first.Sha256, second.Sha256);
    }

    [Fact]
    public async Task A_file_over_the_limit_is_refused_and_nothing_stays_on_disk()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => Files().SaveAsync(new MemoryStream(new byte[3 * 1024 * 1024]), "documents", 2 * 1024 * 1024, Token));

        Assert.Contains("2 МБ", exception.Message, StringComparison.Ordinal);
        Assert.Empty(_folder.Files());
    }

    [Fact]
    public async Task A_file_exactly_at_the_limit_is_accepted()
    {
        var blob = await Files().SaveAsync(new MemoryStream(new byte[1024]), "documents", 1024, Token);

        Assert.Equal(1024, blob.Size);
    }

    [Fact]
    public async Task An_empty_file_is_refused_and_removed()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => Files().SaveAsync(new MemoryStream(), "documents", 1024, Token));

        Assert.Equal("Файл порожній.", exception.Message);
        Assert.Empty(_folder.Files());
    }

    [Fact]
    public async Task A_cancelled_upload_leaves_no_partial_file()
    {
        using var cancellation = new CancellationTokenSource();
        var stream = new CancellingStream(cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Files().SaveAsync(stream, "documents", 10_000_000, cancellation.Token));

        Assert.Empty(_folder.Files());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Documents")]
    [InlineData("../documents")]
    [InlineData("docs/sub")]
    [InlineData("документи")]
    [InlineData("averyveryveryveryveryverylongcategory")]
    public async Task A_category_outside_the_pattern_is_refused(string category)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Files().SaveAsync(new MemoryStream([1]), category, 10, Token));

        Assert.Empty(_folder.Files());
    }

    [Theory]
    [InlineData("../../secrets.json")]
    [InlineData("documents/2026/03/../../../../appsettings.json")]
    [InlineData("documents/2026/03/0123456789abcdef0123456789abcdef/../x")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("documents\\2026\\03\\0123456789abcdef0123456789abcdef")]
    [InlineData("documents/2026/03/0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("documents/2026/3/0123456789abcdef0123456789abcdef")]
    [InlineData("")]
    public async Task Keys_that_do_not_name_a_stored_file_are_refused_for_reading_and_deleting(string key)
    {
        var storage = Files();

        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(key, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(key, Token));
    }

    [Fact]
    public async Task A_deleted_file_is_gone_and_deleting_again_is_harmless()
    {
        var storage = Files();
        var blob = await storage.SaveAsync(new MemoryStream([1, 2, 3]), "tickets", 10, Token);

        await storage.DeleteAsync(blob.StorageKey, Token);
        await storage.DeleteAsync(blob.StorageKey, Token);

        Assert.Empty(_folder.Files());
        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.OpenReadAsync(blob.StorageKey, Token));
    }

    [Fact]
    public async Task An_image_is_stored_with_its_thumbnail_and_deleted_with_it()
    {
        var media = Media();
        var id = Guid.NewGuid();

        await media.SaveAsync(id, new ProcessedImage([1, 2, 3], [4, 5], 10, 10), Token);

        var name = id.ToString("N");
        Assert.Equal([$"App_Data/storage/media/{name[^2..]}/{name}.avif", $"App_Data/storage/media/{name[^2..]}/{name}.thumb.avif"], _folder.Files());
        await using (var main = media.OpenRead(id, thumbnail: false))
        await using (var thumbnail = media.OpenRead(id, thumbnail: true))
        {
            Assert.Equal(3, main!.Length);
            Assert.Equal(2, thumbnail!.Length);
        }

        media.Delete(id);
        Assert.Empty(_folder.Files());
        Assert.Null(media.OpenRead(id, thumbnail: false));
    }

    [Fact]
    public async Task Saving_an_image_again_replaces_it()
    {
        var media = Media();
        var id = Guid.NewGuid();

        await media.SaveAsync(id, new ProcessedImage([1], [1], 1, 1), Token);
        await media.SaveAsync(id, new ProcessedImage([1, 2, 3, 4], [1, 2], 2, 2), Token);

        await using var main = media.OpenRead(id, thumbnail: false);
        Assert.Equal(4, main!.Length);
        Assert.DoesNotContain(_folder.Files(), static f => f.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public void A_missing_image_reads_as_null_and_deleting_it_is_harmless()
    {
        var media = Media();

        Assert.Null(media.OpenRead(Guid.NewGuid(), thumbnail: true));
        media.Delete(Guid.NewGuid());
    }

    /// <summary>
    /// The file storage under the test folder.
    /// </summary>
    /// <returns>The storage.</returns>
    private LocalFileStorage Files() => new(Options.Create(new StorageOptions()), _folder.Environment, _clock);

    /// <summary>
    /// The image storage under the test folder.
    /// </summary>
    /// <returns>The storage.</returns>
    private LocalMediaStorage Media() => new(Options.Create(new StorageOptions()), _folder.Environment);

    /// <summary>
    /// A clock that stands still.
    /// </summary>
    /// <param name="now">The time.</param>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>
    /// A stream that delivers one block and then cancels the upload (a closed browser tab).
    /// </summary>
    /// <param name="cancellation">The cancellation to trigger.</param>
    private sealed class CancellingStream(CancellationTokenSource cancellation) : MemoryStream(new byte[100_000])
    {
        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Position > 0)
            {
                await cancellation.CancelAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await base.ReadAsync(buffer[..Math.Min(buffer.Length, 1000)], cancellationToken);
        }
    }
}
