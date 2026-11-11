using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ODC.Web.Infrastructure.Files;

/// <summary>
/// The format of an encrypted file (documents, signatures, attachments, images, backups): AES-256-GCM in chunks of
/// 64 KiB (the STREAM construction), so files of any size are written and read as streams and a range of a file is read
/// without the rest.
/// </summary>
/// <remarks>
/// <para>Layout: <c>"ODC1"</c>, a random 16-byte salt, then the chunks, each the ciphertext of up to 64 KiB of content
/// followed by its 16-byte tag. Every file has a key of its own, derived from the key of its owner (an organization or
/// the platform) and the salt with HKDF-SHA256, so nonces never repeat under a key: the nonce is the chunk's number
/// (11 bytes, big-endian) and a last-chunk flag. The header is the associated data of every chunk.</para>
/// <para>A changed, reordered, removed or appended chunk fails the authentication: the last chunk is sealed as the last,
/// so a file cut at a chunk boundary does not open.</para>
/// </remarks>
internal static class EncryptedFile
{
    /// <summary>
    /// The size of a chunk's content.
    /// </summary>
    public const int ChunkSize = 64 * 1024;

    /// <summary>
    /// The size of a tag.
    /// </summary>
    public const int TagSize = 16;

    /// <summary>
    /// The size of the header (magic and salt).
    /// </summary>
    public const int HeaderSize = 4 + SaltSize;

    /// <summary>
    /// The size of a key.
    /// </summary>
    public const int KeySize = 32;

    /// <summary>
    /// The size of the salt.
    /// </summary>
    private const int SaltSize = 16;

    /// <summary>
    /// The size of a nonce.
    /// </summary>
    private const int NonceSize = 12;

    /// <summary>
    /// The first bytes of the format.
    /// </summary>
    private static readonly byte[] Magic = "ODC1"u8.ToArray();

    /// <summary>
    /// The HKDF info of the file keys.
    /// </summary>
    private static readonly byte[] Info = Encoding.ASCII.GetBytes("odc-file-v1");

    /// <summary>
    /// Creates a random key.
    /// </summary>
    /// <returns>The key.</returns>
    public static byte[] NewKey() => RandomNumberGenerator.GetBytes(KeySize);

    /// <summary>
    /// Starts an encrypted file.
    /// </summary>
    /// <param name="target">Where the file is written (left open).</param>
    /// <param name="key">The key of the owner.</param>
    /// <returns>The writer: write the content, then complete it.</returns>
    public static Writer Write(Stream target, ReadOnlySpan<byte> key) => new(target, key);

    /// <summary>
    /// Encrypts a whole stream.
    /// </summary>
    /// <param name="source">The content.</param>
    /// <param name="target">Where the file is written (left open).</param>
    /// <param name="key">The key of the owner.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The size of the content.</returns>
    public static async Task<long> EncryptAsync(Stream source, Stream target, byte[] key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var writer = Write(target, key);
        var buffer = new byte[ChunkSize];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
        return writer.Length;
    }

    /// <summary>
    /// Opens an encrypted file for reading its content.
    /// </summary>
    /// <param name="source">The file (disposed with the returned stream); seekable for ranges.</param>
    /// <param name="key">The key of the owner.</param>
    /// <returns>The content.</returns>
    public static Stream Read(Stream source, ReadOnlySpan<byte> key) => new Reader(source, key);

    /// <summary>
    /// Derives the key of one file.
    /// </summary>
    /// <param name="key">The key of the owner.</param>
    /// <param name="salt">The file's salt.</param>
    /// <returns>The file's key.</returns>
    private static byte[] FileKey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> salt)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("A file key has 32 bytes.", nameof(key));
        }

        var derived = new byte[KeySize];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, key, derived, salt, Info);
        return derived;
    }

    /// <summary>
    /// Builds the nonce of a chunk.
    /// </summary>
    /// <param name="nonce">The nonce to fill.</param>
    /// <param name="index">The chunk's number.</param>
    /// <param name="last">Whether it is the last chunk.</param>
    private static void Nonce(Span<byte> nonce, long index, bool last)
    {
        nonce.Clear();
        BinaryPrimitives.WriteInt64BigEndian(nonce[3..11], index);
        nonce[11] = last ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Writes an encrypted file chunk by chunk: a chunk is sealed once the next content shows it is not the last.
    /// </summary>
    internal sealed class Writer : IDisposable
    {
        /// <summary>
        /// The target.
        /// </summary>
        private readonly Stream _target;

        /// <summary>
        /// The cipher with the file's key.
        /// </summary>
        private readonly AesGcm _aes;

        /// <summary>
        /// The header (associated data of every chunk).
        /// </summary>
        private readonly byte[] _header;

        /// <summary>
        /// The content of the open chunk.
        /// </summary>
        private readonly byte[] _chunk = new byte[ChunkSize];

        /// <summary>
        /// The sealed chunk to write.
        /// </summary>
        private readonly byte[] _sealed = new byte[ChunkSize + TagSize];

        /// <summary>
        /// How much of the open chunk is filled.
        /// </summary>
        private int _filled;

        /// <summary>
        /// The number of the open chunk.
        /// </summary>
        private long _index;

        /// <summary>
        /// Whether the header was written.
        /// </summary>
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="Writer"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="key">The key of the owner.</param>
        public Writer(Stream target, ReadOnlySpan<byte> key)
        {
            ArgumentNullException.ThrowIfNull(target);
            _target = target;
            _header = new byte[HeaderSize];
            Magic.CopyTo(_header, 0);
            RandomNumberGenerator.Fill(_header.AsSpan(4));
            _aes = new AesGcm(FileKey(key, _header.AsSpan(4)), TagSize);
        }

        /// <summary>
        /// Gets the size of the content written so far.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Writes content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task WriteAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
        {
            await StartAsync(cancellationToken);
            while (!content.IsEmpty)
            {
                if (_filled == ChunkSize)
                {
                    // More content follows: the full chunk is not the last one.
                    await SealAsync(last: false, cancellationToken);
                }

                var part = Math.Min(content.Length, ChunkSize - _filled);
                content[..part].CopyTo(_chunk.AsMemory(_filled));
                _filled += part;
                Length += part;
                content = content[part..];
            }
        }

        /// <summary>
        /// Seals the last chunk.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task CompleteAsync(CancellationToken cancellationToken)
        {
            await StartAsync(cancellationToken);
            await SealAsync(last: true, cancellationToken);
            await _target.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose() => _aes.Dispose();

        /// <summary>
        /// Writes the header once.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_started)
            {
                _started = true;
                await _target.WriteAsync(_header, cancellationToken);
            }
        }

        /// <summary>
        /// Encrypts and writes the open chunk.
        /// </summary>
        /// <param name="last">Whether it is the last one.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task SealAsync(bool last, CancellationToken cancellationToken)
        {
            Span<byte> nonce = stackalloc byte[NonceSize];
            Nonce(nonce, _index, last);
            _aes.Encrypt(nonce, _chunk.AsSpan(0, _filled), _sealed.AsSpan(0, _filled), _sealed.AsSpan(_filled, TagSize), _header);
            await _target.WriteAsync(_sealed.AsMemory(0, _filled + TagSize), cancellationToken);
            _index++;
            _filled = 0;
        }
    }

    /// <summary>
    /// Reads the content of an encrypted file; seekable when the file is (ranges of downloads).
    /// </summary>
    private sealed class Reader : Stream
    {
        /// <summary>
        /// The encrypted file.
        /// </summary>
        private readonly Stream _source;

        /// <summary>
        /// The key of the owner (the file's key is derived with the header's salt).
        /// </summary>
        private readonly byte[] _key;

        /// <summary>
        /// The content of the decrypted chunk.
        /// </summary>
        private readonly byte[] _chunk = new byte[ChunkSize];

        /// <summary>
        /// The sealed chunk read from the file.
        /// </summary>
        private readonly byte[] _sealed = new byte[ChunkSize + TagSize];

        /// <summary>
        /// The content size (known for a seekable file).
        /// </summary>
        private readonly long? _length;

        /// <summary>
        /// The number of the last chunk (known for a seekable file).
        /// </summary>
        private readonly long? _lastIndex;

        /// <summary>
        /// The header.
        /// </summary>
        private byte[]? _header;

        /// <summary>
        /// The cipher.
        /// </summary>
        private AesGcm? _aes;

        /// <summary>
        /// The number of the decrypted chunk (-1: none).
        /// </summary>
        private long _current = -1;

        /// <summary>
        /// The content size of the decrypted chunk.
        /// </summary>
        private int _currentSize;

        /// <summary>
        /// The position in the content.
        /// </summary>
        private long _position;

        /// <summary>
        /// Whether the last chunk of a non-seekable file was read.
        /// </summary>
        private bool _ended;

        /// <summary>
        /// The next chunk of a non-seekable file read ahead (to know whether the current one is the last).
        /// </summary>
        private byte[]? _ahead;

        /// <summary>
        /// The size of <see cref="_ahead"/>.
        /// </summary>
        private int _aheadSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="Reader"/> class.
        /// </summary>
        /// <param name="source">The encrypted file.</param>
        /// <param name="key">The key of the owner.</param>
        public Reader(Stream source, ReadOnlySpan<byte> key)
        {
            ArgumentNullException.ThrowIfNull(source);
            _source = source;
            _key = key.ToArray();
            if (source.CanSeek)
            {
                var body = source.Length - HeaderSize;
                var chunks = (body + ChunkSize + TagSize - 1) / (ChunkSize + TagSize);
                if (body < TagSize || body - (chunks * TagSize) < 0)
                {
                    throw new CryptographicException("The file is not an encrypted file of the cabinet.");
                }

                _lastIndex = chunks - 1;
                _length = body - (chunks * TagSize);
            }
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => _source.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => _length ?? throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty || !Load())
            {
                return 0;
            }

            var inChunk = (int)(_position - (_current * ChunkSize));
            var count = Math.Min(buffer.Length, _currentSize - inChunk);
            _chunk.AsSpan(inChunk, count).CopyTo(buffer);
            _position += count;
            return count;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromResult(Read(buffer.AsSpan(offset, count)));

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Read(buffer.Span));

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }

            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                _ => Length + offset,
            };
            _position = target >= 0 ? target : throw new IOException("A position before the start.");
            return _position;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _aes?.Dispose();
                _source.Dispose();
                CryptographicOperations.ZeroMemory(_key);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Makes the chunk of the position the decrypted one.
        /// </summary>
        /// <returns>Whether there is content at the position.</returns>
        private bool Load()
        {
            EnsureHeader();
            var index = _position / ChunkSize;
            if (index == _current && _position - (index * ChunkSize) < _currentSize)
            {
                return true;
            }

            if (_length is { } length)
            {
                if (_position >= length)
                {
                    return false;
                }

                _source.Position = HeaderSize + (index * (ChunkSize + TagSize));
                var size = (int)Math.Min(ChunkSize + TagSize, _source.Length - _source.Position);
                _source.ReadExactly(_sealed, 0, size);
                Open(index, size, last: index == _lastIndex);
                return true;
            }

            // A stream read once from the start: one chunk after another.
            if (_ended || index != _current + 1)
            {
                return false;
            }

            var read = _ahead is null ? Fill(_sealed) : _aheadSize;
            if (_ahead is not null)
            {
                _ahead.AsSpan(0, read).CopyTo(_sealed);
            }

            _ahead ??= new byte[ChunkSize + TagSize];
            _aheadSize = read == ChunkSize + TagSize ? Fill(_ahead) : 0;
            var last = _aheadSize == 0;
            Open(index, read, last);
            _ended = last;
            return _currentSize > 0;
        }

        /// <summary>
        /// Reads as much of a sealed chunk as the file has.
        /// </summary>
        /// <param name="target">The buffer.</param>
        /// <returns>The number of bytes read.</returns>
        private int Fill(byte[] target) => _source.ReadAtLeast(target, target.Length, throwOnEndOfStream: false);

        /// <summary>
        /// Decrypts a sealed chunk.
        /// </summary>
        /// <param name="index">Its number.</param>
        /// <param name="size">Its size with the tag.</param>
        /// <param name="last">Whether it is the last one.</param>
        private void Open(long index, int size, bool last)
        {
            if (size < TagSize)
            {
                throw new CryptographicException("The encrypted file is damaged.");
            }

            Span<byte> nonce = stackalloc byte[NonceSize];
            Nonce(nonce, index, last);
            var content = size - TagSize;
            _aes!.Decrypt(nonce, _sealed.AsSpan(0, content), _sealed.AsSpan(content, TagSize), _chunk.AsSpan(0, content), _header);
            _current = index;
            _currentSize = content;
        }

        /// <summary>
        /// Reads the header and derives the file's key once.
        /// </summary>
        private void EnsureHeader()
        {
            if (_header is not null)
            {
                return;
            }

            var header = new byte[HeaderSize];
            if (_source.CanSeek)
            {
                _source.Position = 0;
            }

            _source.ReadExactly(header);
            if (!header.AsSpan(0, 4).SequenceEqual(Magic))
            {
                throw new CryptographicException("The file is not an encrypted file of the cabinet.");
            }

            _header = header;
            _aes = new AesGcm(FileKey(_key, header.AsSpan(4)), TagSize);
        }
    }
}
