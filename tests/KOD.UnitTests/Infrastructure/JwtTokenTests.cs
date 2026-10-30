using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KOD.Infrastructure.Identity;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace KOD.UnitTests.Infrastructure;

public sealed class JwtTokenTests : IDisposable
{
    /// <summary>
    /// The security stamp of the test user.
    /// </summary>
    private const string Stamp = "Z3XKQH7TL5WC2M4QJ6T5ECKBMFZ2TF7Q";

    /// <summary>
    /// The Data Protection provider shared by the keys of one test (a stand-in for the persisted key ring).
    /// </summary>
    private static readonly IDataProtectionProvider DataProtection = new EphemeralDataProtectionProvider();

    /// <summary>
    /// The content root that holds the generated signing key.
    /// </summary>
    private readonly string _root = Directory.CreateTempSubdirectory("kod-jwt-").FullName;

    /// <summary>
    /// The test user.
    /// </summary>
    private readonly Guid _userId = Guid.CreateVersion7();

    /// <summary>
    /// The signing key.
    /// </summary>
    private readonly JwtSigningKey _key;

    /// <summary>
    /// The token service.
    /// </summary>
    private readonly JwtTokenService _tokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenTests"/> class.
    /// </summary>
    public JwtTokenTests()
    {
        _key = NewKey(_root);
        _tokens = NewService(_key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _key.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Access_token_is_es256_typed_and_carries_the_user_and_sign_in_but_not_the_stamp()
    {
        var signIn = new SignInContext(Guid.CreateVersion7(), DateTimeOffset.UtcNow.AddMinutes(-10), "mfa", IsPersistent: true);
        var issued = _tokens.IssueAccessToken(Claims(), signIn, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);

        var token = new JsonWebToken(issued.Token);
        Assert.Equal("ES256", token.Alg);
        Assert.Equal(JwtDefaults.AccessTokenType, token.Typ);
        Assert.False(string.IsNullOrEmpty(token.Kid));
        Assert.Equal(_userId.ToString(), token.Subject);
        Assert.Equal(["documents.view", "news.view"], token.Claims.Where(static c => c.Type == AppClaimTypes.Permission).Select(static c => c.Value).Order());
        Assert.DoesNotContain(Stamp, Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(issued.Token.Split('.')[1])), StringComparison.Ordinal);

        var result = await _tokens.ValidateAccessTokenAsync(issued.Token);
        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(JwtDefaults.AuthenticationScheme, result.ClaimsIdentity.AuthenticationType);

        // The identity of the issuing request equals the validated one, including the claim issuer (antiforgery binds to it).
        Assert.Equal(result.ClaimsIdentity.FindFirst(AppClaimTypes.Subject)!.Issuer, issued.Identity.FindFirst(AppClaimTypes.Subject)!.Issuer);
        Assert.Equal(result.ClaimsIdentity.FindFirst(AppClaimTypes.Subject)!.Value, issued.Identity.FindFirst(AppClaimTypes.Subject)!.Value);

        var read = JwtTokenService.ReadSignIn(result.ClaimsIdentity);
        Assert.NotNull(read);
        Assert.Equal(signIn.Id, read.Id);
        Assert.Equal(signIn.AuthenticatedAt.ToUnixTimeSeconds(), read.AuthenticatedAt.ToUnixTimeSeconds());
        Assert.Equal("mfa", read.AuthenticationMethod);
        Assert.True(read.IsPersistent);
    }

    [Fact]
    public async Task Token_kinds_cannot_stand_in_for_each_other()
    {
        var access = Issue(DateTimeOffset.UtcNow).Token;
        var (twoFactor, _) = _tokens.IssueTwoFactor([new Claim(ClaimTypes.Name, _userId.ToString())], DateTimeOffset.UtcNow);

        Assert.True((await _tokens.ValidateAccessTokenAsync(access)).IsValid);
        Assert.True((await _tokens.ValidateTwoFactorTokenAsync(twoFactor)).IsValid);
        Assert.False((await _tokens.ValidateAccessTokenAsync(twoFactor)).IsValid);
        Assert.False((await _tokens.ValidateTwoFactorTokenAsync(access)).IsValid);
    }

    [Fact]
    public async Task Tampered_unsigned_and_foreign_tokens_are_rejected()
    {
        var parts = Issue(DateTimeOffset.UtcNow).Token.Split('.');

        // A payload with an extra permission keeps the old signature.
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1])).Replace("news.view", "roles.manage", StringComparison.Ordinal);
        Assert.False((await _tokens.ValidateAccessTokenAsync($"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}")).IsValid);

        // "alg": "none" (RFC 8725 §2.1).
        var header = Base64UrlEncoder.Encode("""{"alg":"none","typ":"at+jwt"}""");
        Assert.False((await _tokens.ValidateAccessTokenAsync($"{header}.{parts[1]}.")).IsValid);

        // Signed by another key.
        var otherRoot = Directory.CreateTempSubdirectory("kod-jwt-").FullName;
        try
        {
            using var otherKey = NewKey(otherRoot);
            var foreign = NewService(otherKey).IssueAccessToken(Claims(), SignIn(DateTimeOffset.UtcNow), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow);
            Assert.False((await _tokens.ValidateAccessTokenAsync(foreign.Token)).IsValid);
        }
        finally
        {
            Directory.Delete(otherRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Expired_tokens_are_rejected_and_never_outlive_the_sign_in()
    {
        var result = await _tokens.ValidateAccessTokenAsync(Issue(DateTimeOffset.UtcNow.AddHours(-1)).Token);
        Assert.False(result.IsValid);
        Assert.IsType<SecurityTokenExpiredException>(result.Exception, exactMatch: false);

        // An access token ends with its sign-in, even when that is sooner than its own lifetime (5 minutes).
        var now = DateTimeOffset.UtcNow;
        var late = _tokens.IssueAccessToken(Claims(), SignIn(now), now.AddMinutes(2), now);
        Assert.True(late.ExpiresAt <= now.AddMinutes(2));
        Assert.Throws<InvalidOperationException>(() => _tokens.IssueAccessToken(Claims(), SignIn(now), now.AddSeconds(-1), now));
    }

    [Fact]
    public void The_generated_key_is_kept_encrypted_and_reused()
    {
        var file = Path.Combine(_root, "App_Data", "keys", "jwt-signing-key.bin");
        Assert.True(File.Exists(file));
        Assert.DoesNotContain("PRIVATE KEY", File.ReadAllText(file), StringComparison.Ordinal);

        // The same Data Protection keys read the same signing key: tokens survive a restart.
        using var again = NewKey(_root);
        Assert.Equal(_key.SecurityKey.KeyId, again.SecurityKey.KeyId);
    }

    [Fact]
    public void A_pem_key_must_be_p256()
    {
        var path = Path.Combine(_root, "rsa.pem");
        using (var rsa = RSA.Create(2048))
        {
            File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
        }

        Assert.ThrowsAny<Exception>(() => NewKey(_root, path).Dispose());

        using var p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        File.WriteAllText(path, p384.ExportPkcs8PrivateKeyPem());
        Assert.Throws<InvalidOperationException>(() => NewKey(_root, path).Dispose());

        using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        File.WriteAllText(path, p256.ExportEncryptedPkcs8PrivateKeyPem("secret", new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000)));
        using var loaded = NewKey(_root, path, "secret");
        Assert.False(string.IsNullOrEmpty(loaded.SecurityKey.KeyId));
    }

    /// <summary>
    /// Creates a signing key.
    /// </summary>
    /// <param name="root">The content root.</param>
    /// <param name="pemPath">A PEM file, or <see langword="null"/> to generate a key.</param>
    /// <param name="password">The password of an encrypted PEM file.</param>
    /// <returns>The key.</returns>
    private static JwtSigningKey NewKey(string root, string? pemPath = null, string? password = null)
        => new(
            Options.Create(new JwtOptions { SigningKeyPath = pemPath, SigningKeyPassword = password }),
            new TestEnvironment(root),
            DataProtection,
            NullLogger<JwtSigningKey>.Instance);

    /// <summary>
    /// Creates a token service.
    /// </summary>
    /// <param name="key">The signing key.</param>
    /// <returns>The service.</returns>
    private static JwtTokenService NewService(JwtSigningKey key)
        => new(key, Options.Create(new JwtOptions()), Options.Create(new IdentityOptions
        {
            ClaimsIdentity = { UserIdClaimType = AppClaimTypes.Subject, UserNameClaimType = AppClaimTypes.Name },
        }));

    /// <summary>
    /// Creates a sign-in that happened at a moment.
    /// </summary>
    /// <param name="at">The moment.</param>
    /// <returns>The sign-in.</returns>
    private static SignInContext SignIn(DateTimeOffset at) => new(Guid.CreateVersion7(), at, "mfa", IsPersistent: false);

    /// <summary>
    /// Builds the claims the Identity claims factory produces for the test user.
    /// </summary>
    /// <returns>The claims.</returns>
    private List<Claim> Claims() =>
    [
        new(AppClaimTypes.Subject, _userId.ToString()),
        new(AppClaimTypes.Name, "user@example.com"),
        new(new IdentityOptions().ClaimsIdentity.SecurityStampClaimType, Stamp),
        new(AppClaimTypes.FullName, "Шевченко Тарас Григорович"),
        new(AppClaimTypes.Permission, "documents.view"),
        new(AppClaimTypes.Permission, "news.view"),
    ];

    /// <summary>
    /// Issues an access token for a sign-in that happened at <paramref name="now"/>.
    /// </summary>
    /// <param name="now">The moment.</param>
    /// <returns>The token.</returns>
    private IssuedAccessToken Issue(DateTimeOffset now) => _tokens.IssueAccessToken(Claims(), SignIn(now), now.AddHours(8), now);

    /// <summary>
    /// A host environment with a content root.
    /// </summary>
    /// <param name="root">The content root.</param>
    private sealed class TestEnvironment(string root) : IHostEnvironment
    {
        /// <inheritdoc />
        public string EnvironmentName { get; set; } = Environments.Development;

        /// <inheritdoc />
        public string ApplicationName { get; set; } = "KOD.Tests";

        /// <inheritdoc />
        public string ContentRootPath { get; set; } = root;

        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
