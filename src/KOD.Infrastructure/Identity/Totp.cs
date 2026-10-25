using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Time-based one-time passwords (RFC 6238 with HMAC-SHA1, 6 digits, 30-second steps), compatible with the keys that
/// ASP.NET Core Identity issues (Base32, RFC 4648) and with Google / Microsoft Authenticator.
/// </summary>
internal static class Totp
{
    /// <summary>
    /// The length of a time step.
    /// </summary>
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The Base32 alphabet.
    /// </summary>
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Gets the time step of a moment.
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The number of 30-second steps since the Unix epoch.</returns>
    public static long TimeStep(DateTimeOffset moment) => moment.ToUnixTimeSeconds() / (long)Step.TotalSeconds;

    /// <summary>
    /// Computes the code of a time step.
    /// </summary>
    /// <param name="key">The shared secret.</param>
    /// <param name="timeStep">The time step.</param>
    /// <returns>The code (0–999999).</returns>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "RFC 6238 authenticator apps use HMAC-SHA1; HMAC does not rely on SHA-1 collision resistance.")]
    public static int Compute(ReadOnlySpan<byte> key, long timeStep)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);

        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(key, counter, hash);

        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.Slice(offset, 4)) & 0x7FFFFFFF;
        return binary % 1_000_000;
    }

    /// <summary>
    /// Decodes a Base32 key (case-insensitive, spaces and padding ignored).
    /// </summary>
    /// <param name="value">The encoded key.</param>
    /// <returns>The key bytes, or <see langword="null"/> when the value is not Base32.</returns>
    public static byte[]? DecodeBase32(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var output = new List<byte>(value.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        foreach (var character in value)
        {
            if (character is ' ' or '=')
            {
                continue;
            }

            var index = Base32Alphabet.IndexOf(char.ToUpperInvariant(character), StringComparison.Ordinal);
            if (index < 0)
            {
                return null;
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return output.Count > 0 ? [.. output] : null;
    }
}
