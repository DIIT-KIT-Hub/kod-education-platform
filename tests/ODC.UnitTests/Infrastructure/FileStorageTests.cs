using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Infrastructure.Media;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Media;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The private file storage (documents, signatures, attachments) and the image storage on the local disk: a folder per
/// organization and one of the platform, every file encrypted with the key of its owner, keys that cannot leave the
/// owner's folder, size limits, hashes of the content, cleanup of partial files.
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
    /// The services of the platform's key (an in-memory master database).
    /// </summary>
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddDbContext<MasterDbContext>(static options => options.UseInMemoryDatabase($"files-{Guid.NewGuid():N}"))
        .BuildServiceProvider();

    /// <summary>
    /// The keys of the files.
    /// </summary>
    private readonly FileKeys _keys;

    /// <summary>
    /// The first organization.
    /// </summary>
    private readonly Organization _a;

    /// <summary>
    /// The second organization.
    /// </summary>
    private readonly Organization _b;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageTests"/> class.
    /// </summary>
    public FileStorageTests()
    {
        _keys = new FileKeys(_services.GetRequiredService<IServiceScopeFactory>(), new OrganizationSecrets(new EphemeralDataProtectionProvider()), _clock);
        _a = NewOrganization("A");
        _b = NewOrganization("B");
    }

    /// <summary>
    /// Gets the cancellation token of the test.
    /// </summary>
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _folder.Dispose();
        _services.Dispose();
        _keys.Dispose();
    }

    [Fact]
    public async Task A_saved_file_gets_a_dated_key_its_size_and_SHA_256_and_reads_back()
    {
        var storage = Files(_a);
        byte[] content = [.. Enumerable.Range(0, 200_000).Select(static i => (byte)i)];

        var blob = await storage.SaveAsync(new MemoryStream(content), "documents", 1_000_000, Token);

        Assert.Matches("^documents/2026/03/[0-9a-f]{32}$", blob.StorageKey);
        Assert.Equal(content.Length, blob.Size);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), blob.Sha256);
        Assert.Equal(content, await ReadAllAsync(await storage.OpenReadAsync(blob.StorageKey, Token)));
        Assert.StartsWith($"App_Data/storage/orgs/{_a.Id:N}/documents/2026/03/", Assert.Single(_folder.Files()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_file_on_disk_is_encrypted()
    {
        byte[] content = [.. Enumerable.Repeat((byte)'x', 100_000)];

        await Files(_a).SaveAsync(new MemoryStream(content), "documents", 1_000_000, Token);

        var onDisk = await File.ReadAllBytesAsync(Path.Combine(_folder.Path, Assert.Single(_folder.Files())), Token);
        Assert.Equal("ODC1"u8.ToArray(), onDisk[..4]);
        Assert.Equal(-1, onDisk.AsSpan().IndexOf(content.AsSpan(0, 64)));
    }

    [Fact]
    public async Task Another_organization_cannot_read_or_delete_a_file_by_its_key()
    {
        var blob = await Files(_a).SaveAsync(new MemoryStream([1, 2, 3]), "documents", 10, Token);

        await Assert.ThrowsAsync<FileNotFoundException>(() => Files(_b).OpenReadAsync(blob.StorageKey, Token));
        await Files(_b).DeleteAsync(blob.StorageKey, Token);
        Assert.Single(_folder.Files());
    }

    [Fact]
    public async Task A_file_moved_into_another_organization_does_not_open_with_its_key()
    {
        var blob = await Files(_a).SaveAsync(new MemoryStream([1, 2, 3]), "documents", 10, Token);
        var from = Path.Combine(_folder.Path, Assert.Single(_folder.Files()));
        var to = from.Replace($"{_a.Id:N}", $"{_b.Id:N}", StringComparison.Ordinal);
        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
        File.Copy(from, to);

        await using var stream = await Files(_b).OpenReadAsync(blob.StorageKey, Token);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => ReadAllAsync(stream));
    }

    [Fact]
    public async Task A_changed_byte_makes_the_file_unreadable()
    {
        var storage = Files(_a);
        var blob = await storage.SaveAsync(new MemoryStream(new byte[1000]), "documents", 10_000, Token);
        var path = Path.Combine(_folder.Path, Assert.Single(_folder.Files()));
        var bytes = await File.ReadAllBytesAsync(path, Token);
        bytes[^20] ^= 1;
        await File.WriteAllBytesAsync(path, bytes, Token);

        await using var stream = await storage.OpenReadAsync(blob.StorageKey, Token);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => ReadAllAsync(stream));
    }

    [Fact]
    public async Task A_range_is_read_without_the_rest()
    {
        var storage = Files(_a);
        byte[] content = [.. Enumerable.Range(0, 300_000).Select(static i => (byte)(i * 7))];
        var blob = await storage.SaveAsync(new MemoryStream(content), "documents", 1_000_000, Token);

        await using var stream = await storage.OpenReadAsync(blob.StorageKey, Token);
        Assert.True(stream.CanSeek);
        Assert.Equal(content.Length, stream.Length);
        stream.Position = 140_000;
        var part = new byte[70_000];
        await stream.ReadExactlyAsync(part, Token);
        Assert.Equal(content.AsSpan(140_000, 70_000).ToArray(), part);
    }

    [Fact]
    public async Task Requests_to_the_support_are_files_of_the_platform_without_an_organization()
    {
        var storage = new LocalFileStorage(Options.Create(new StorageOptions()), _folder.Environment, _clock, new OrganizationContext(), _keys);

        var blob = await storage.SaveAsync(new MemoryStream([1, 2, 3]), "tickets", 10, Token);

        Assert.StartsWith("App_Data/storage/platform/tickets/2026/03/", Assert.Single(_folder.Files()), StringComparison.Ordinal);
        Assert.Equal([1, 2, 3], await ReadAllAsync(await storage.OpenReadAsync(blob.StorageKey, Token)));
        await Assert.ThrowsAsync<NoOrganizationException>(() => storage.SaveAsync(new MemoryStream([1]), "documents", 10, Token));
    }

    [Fact]
    public async Task Two_files_never_share_a_key()
    {
        var storage = Files(_a);

        var first = await storage.SaveAsync(new MemoryStream([1]), "chats", 10, Token);
        var second = await storage.SaveAsync(new MemoryStream([1]), "chats", 10, Token);

        Assert.NotEqual(first.StorageKey, second.StorageKey);
        Assert.Equal(first.Sha256, second.Sha256);
    }

    [Fact]
    public async Task A_file_over_the_limit_is_refused_and_nothing_stays_on_disk()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => Files(_a).SaveAsync(new MemoryStream(new byte[3 * 1024 * 1024]), "documents", 2 * 1024 * 1024, Token));

        Assert.Contains("2 МБ", exception.Message, StringComparison.Ordinal);
        Assert.Empty(_folder.Files());
    }

    [Fact]
    public async Task A_file_exactly_at_the_limit_is_accepted()
    {
        var blob = await Files(_a).SaveAsync(new MemoryStream(new byte[1024]), "documents", 1024, Token);

        Assert.Equal(1024, blob.Size);
    }

    [Fact]
    public async Task An_empty_file_is_refused_and_removed()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() => Files(_a).SaveAsync(new MemoryStream(), "documents", 1024, Token));

        Assert.Equal("Файл порожній.", exception.Message);
        Assert.Empty(_folder.Files());
    }

    [Fact]
    public async Task A_cancelled_upload_leaves_no_partial_file()
    {
        using var cancellation = new CancellationTokenSource();
        var stream = new CancellingStream(cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Files(_a).SaveAsync(stream, "documents", 10_000_000, cancellation.Token));

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
        await Assert.ThrowsAsync<ArgumentException>(() => Files(_a).SaveAsync(new MemoryStream([1]), category, 10, Token));

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
        var storage = Files(_a);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync(key, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(key, Token));
    }

    [Fact]
    public async Task A_deleted_file_is_gone_and_deleting_again_is_harmless()
    {
        var storage = Files(_a);
        var blob = await storage.SaveAsync(new MemoryStream([1, 2, 3]), "chats", 10, Token);

        await storage.DeleteAsync(blob.StorageKey, Token);
        await storage.DeleteAsync(blob.StorageKey, Token);

        Assert.Empty(_folder.Files());
        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.OpenReadAsync(blob.StorageKey, Token));
    }

    [Fact]
    public async Task An_image_is_stored_encrypted_with_its_thumbnail_and_deleted_with_it()
    {
        var media = Media();
        var id = Guid.NewGuid();

        await media.SaveAsync(id, new ProcessedImage([1, 2, 3], [4, 5], 10, 10), Token);

        var name = id.ToString("N");
        Assert.Equal([$"App_Data/storage/platform/media/{name[^2..]}/{name}", $"App_Data/storage/platform/media/{name[^2..]}/{name}.thumb"], _folder.Files());
        await using (var main = await media.OpenReadAsync(id, thumbnail: false, Token))
        await using (var thumbnail = await media.OpenReadAsync(id, thumbnail: true, Token))
        {
            Assert.Equal(3, main!.Length);
            Assert.Equal([4, 5], await ReadAllAsync(thumbnail!));
        }

        media.Delete(id);
        Assert.Empty(_folder.Files());
        Assert.Null(await media.OpenReadAsync(id, thumbnail: false, Token));
    }

    [Fact]
    public async Task Saving_an_image_again_replaces_it()
    {
        var media = Media();
        var id = Guid.NewGuid();

        await media.SaveAsync(id, new ProcessedImage([1], [1], 1, 1), Token);
        await media.SaveAsync(id, new ProcessedImage([1, 2, 3, 4], [1, 2], 2, 2), Token);

        await using var main = await media.OpenReadAsync(id, thumbnail: false, Token);
        Assert.Equal(4, main!.Length);
        Assert.DoesNotContain(_folder.Files(), static f => f.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_missing_image_reads_as_null_and_deleting_it_is_harmless()
    {
        var media = Media();

        Assert.Null(await media.OpenReadAsync(Guid.NewGuid(), thumbnail: true, Token));
        media.Delete(Guid.NewGuid());
    }

    [Fact]
    public async Task A_stream_read_once_from_the_start_opens_and_a_cut_one_does_not()
    {
        var key = EncryptedFile.NewKey();
        byte[] content = [.. Enumerable.Range(0, (EncryptedFile.ChunkSize * 2) + 5).Select(static i => (byte)i)];
        var encrypted = new MemoryStream();
        await EncryptedFile.EncryptAsync(new MemoryStream(content), encrypted, key, Token);

        Assert.Equal(content, await ReadAllAsync(EncryptedFile.Read(new OnceStream(encrypted.ToArray()), key)));

        // Without its last chunk the file ends at a chunk boundary: the chunk before was not sealed as the last.
        var cut = encrypted.ToArray()[..(EncryptedFile.HeaderSize + (2 * (EncryptedFile.ChunkSize + EncryptedFile.TagSize)))];
        await Assert.ThrowsAnyAsync<CryptographicException>(() => ReadAllAsync(EncryptedFile.Read(new MemoryStream(cut), key)));
        await Assert.ThrowsAnyAsync<CryptographicException>(() => ReadAllAsync(EncryptedFile.Read(new OnceStream(cut), key)));
    }

    /// <summary>
    /// Reads a stream to the end and disposes it.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>Its content.</returns>
    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        await using (stream)
        {
            var copy = new MemoryStream();
            await stream.CopyToAsync(copy, Token);
            return copy.ToArray();
        }
    }

    /// <summary>
    /// An organization with a key of its files.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The organization.</returns>
    private Organization NewOrganization(string code)
    {
        var organization = new Organization(Guid.CreateVersion7(), code, code, "odc_org_" + code.ToLowerInvariant());
        organization.BeginCreation(null, null, _keys.NewFor(organization.Id));
        return organization;
    }

    /// <summary>
    /// The file storage of an organization under the test folder.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The storage.</returns>
    private LocalFileStorage Files(Organization organization)
    {
        var context = new OrganizationContext();
        context.Enter(organization, OrganizationAccess.Member);
        return new(Options.Create(new StorageOptions()), _folder.Environment, _clock, context, _keys);
    }

    /// <summary>
    /// The image storage under the test folder.
    /// </summary>
    /// <returns>The storage.</returns>
    private LocalMediaStorage Media() => new(Options.Create(new StorageOptions()), _folder.Environment, _keys);

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
    /// A stream that can only be read once from the start (a pipe), in small pieces.
    /// </summary>
    /// <param name="content">The content.</param>
    private sealed class OnceStream(byte[] content) : MemoryStream(content)
    {
        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 7000));
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
