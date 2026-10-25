using System.Security.Cryptography;
using System.Text;
using KOD.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace KOD.UnitTests.Infrastructure;

public sealed class IdentitySecurityTests
{
    /// <summary>
    /// The shared secret of the RFC 6238 test vectors (HMAC-SHA1).
    /// </summary>
    private static readonly byte[] Rfc6238Secret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59, 287082)]
    [InlineData(1111111109, 81804)]
    [InlineData(1111111111, 50471)]
    [InlineData(1234567890, 5924)]
    [InlineData(2000000000, 279037)]
    public void Totp_matches_the_rfc_6238_test_vectors(long unixSeconds, int expected)
    {
        var step = Totp.TimeStep(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));

        Assert.Equal(expected, Totp.Compute(Rfc6238Secret, step));
    }

    [Fact]
    public void Base32_keys_from_identity_decode_to_the_secret()
    {
        Assert.Equal(Rfc6238Secret, Totp.DecodeBase32("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ"));
        Assert.Equal(Rfc6238Secret, Totp.DecodeBase32("gezd gnbv gy3t qojq gezd gnbv gy3t qojq"));
        Assert.Null(Totp.DecodeBase32("not base32!"));
    }

    [Fact]
    public void Secret_tokens_are_encrypted_and_bound_to_their_owner()
    {
        var protector = new UserTokenProtector(new EphemeralDataProtectionProvider());
        var owner = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();

        var stored = protector.Protect(owner, UserTokenProtector.IdentityProvider, "AuthenticatorKey", "JBSWY3DPEHPK3PXP");

        Assert.True(UserTokenProtector.IsProtected(stored));
        Assert.DoesNotContain("JBSWY3DPEHPK3PXP", stored, StringComparison.Ordinal);
        Assert.Equal("JBSWY3DPEHPK3PXP", protector.Unprotect(owner, UserTokenProtector.IdentityProvider, "AuthenticatorKey", stored));

        // A ciphertext copied into another account (or another token) cannot be decrypted.
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(other, UserTokenProtector.IdentityProvider, "AuthenticatorKey", stored));
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(owner, UserTokenProtector.IdentityProvider, "RecoveryCodes", stored));
    }

    [Fact]
    public void Other_tokens_and_legacy_values_pass_through()
    {
        var protector = new UserTokenProtector(new EphemeralDataProtectionProvider());
        var owner = Guid.CreateVersion7();

        Assert.Equal("12345", protector.Protect(owner, "[KOD]", "AuthenticatorLastStep", "12345"));
        Assert.Equal("LEGACYKEY", protector.Unprotect(owner, UserTokenProtector.IdentityProvider, "AuthenticatorKey", "LEGACYKEY"));
        Assert.Null(protector.Protect(owner, UserTokenProtector.IdentityProvider, "RecoveryCodes", null));
    }
}
