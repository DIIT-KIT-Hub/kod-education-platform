using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// The format of refresh tokens: an opaque base64url string of the family identifier (16 bytes), the generation
/// (4 bytes, big-endian) and <c>HMAC-SHA256(familyKey, familyId ‖ generation)</c> (32 bytes). The identifier and the
/// generation only select the family row; the MAC, compared in constant time, proves the token was issued by the server.
/// </summary>
public static class RefreshToken
{
    /// <summary>
    /// The length of a family key (256 bits).
    /// </summary>
    public const int KeyLength = 32;

    /// <summary>
    /// The length of the family identifier.
    /// </summary>
    private const int IdLength = 16;

    /// <summary>
    /// The length of the generation.
    /// </summary>
    private const int GenerationLength = 4;

    /// <summary>
    /// The length of the selector (identifier + generation).
    /// </summary>
    private const int SelectorLength = IdLength + GenerationLength;

    /// <summary>
    /// The length of a token in bytes.
    /// </summary>
    private const int TokenLength = SelectorLength + HMACSHA256.HashSizeInBytes;

    /// <summary>
    /// Generates a new family key.
    /// </summary>
    /// <returns>The key.</returns>
    public static byte[] NewKey() => RandomNumberGenerator.GetBytes(KeyLength);

    /// <summary>
    /// Formats the refresh token of a generation.
    /// </summary>
    /// <param name="key">The family key.</param>
    /// <param name="familyId">The family identifier.</param>
    /// <param name="generation">The generation.</param>
    /// <returns>The token.</returns>
    public static string Format(ReadOnlySpan<byte> key, Guid familyId, int generation)
    {
        Span<byte> bytes = stackalloc byte[TokenLength];
        WriteSelector(bytes, familyId, generation);
        HMACSHA256.HashData(key, bytes[..SelectorLength], bytes[SelectorLength..]);
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>
    /// Parses a refresh token without verifying it.
    /// </summary>
    /// <param name="token">The token from the cookie.</param>
    /// <param name="familyId">The family identifier.</param>
    /// <param name="generation">The generation.</param>
    /// <returns><see langword="true"/> when the token is well-formed.</returns>
    public static bool TryParse(string? token, out Guid familyId, out int generation)
    {
        familyId = Guid.Empty;
        generation = 0;
        Span<byte> bytes = stackalloc byte[TokenLength];
        if (token is null
            || Base64Url.GetMaxDecodedLength(token.Length) < TokenLength
            || !Base64Url.TryDecodeFromChars(token, bytes, out var written)
            || written != TokenLength)
        {
            return false;
        }

        familyId = new Guid(bytes[..IdLength], bigEndian: true);
        generation = BinaryPrimitives.ReadInt32BigEndian(bytes[IdLength..SelectorLength]);
        return generation >= 0;
    }

    /// <summary>
    /// Verifies the MAC of a parsed token in constant time.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="key">The family key.</param>
    /// <returns><see langword="true"/> when the server issued this token.</returns>
    public static bool Verify(string token, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!TryParse(token, out var familyId, out var generation))
        {
            return false;
        }

        Span<byte> presented = stackalloc byte[TokenLength];
        Span<byte> expected = stackalloc byte[HMACSHA256.HashSizeInBytes];
        Base64Url.TryDecodeFromChars(token, presented, out _);
        Span<byte> selector = stackalloc byte[SelectorLength];
        WriteSelector(selector, familyId, generation);
        HMACSHA256.HashData(key, selector, expected);
        return CryptographicOperations.FixedTimeEquals(presented[SelectorLength..], expected);
    }

    /// <summary>
    /// Writes the selector bytes.
    /// </summary>
    /// <param name="destination">The destination (at least the selector length).</param>
    /// <param name="familyId">The family identifier.</param>
    /// <param name="generation">The generation.</param>
    private static void WriteSelector(Span<byte> destination, Guid familyId, int generation)
    {
        familyId.TryWriteBytes(destination[..IdLength], bigEndian: true, out _);
        BinaryPrimitives.WriteInt32BigEndian(destination[IdLength..SelectorLength], generation);
    }
}
