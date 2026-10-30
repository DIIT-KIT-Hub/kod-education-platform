using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KOD.Infrastructure.Identity.Tokens;

/// <summary>
/// The ES256 (ECDSA P-256, RFC 7518 §3.4) private key that signs access tokens. It comes from the PEM file in
/// <see cref="JwtOptions.SigningKeyPath"/> or, without one, is generated once and stored encrypted with Data Protection
/// (whose keys are themselves encrypted at rest). The key identifier (<c>kid</c>) is its RFC 7638 JWK thumbprint.
/// </summary>
public sealed partial class JwtSigningKey : IDisposable
{
    /// <summary>
    /// The only accepted signing algorithm (RFC 8725 §3.1: the verifier fixes the algorithm, never the token).
    /// </summary>
    public const string Algorithm = SecurityAlgorithms.EcdsaSha256;

    /// <summary>
    /// The file with the generated key, relative to the content root.
    /// </summary>
    private const string GeneratedKeyFile = "App_Data/keys/jwt-signing-key.bin";

    /// <summary>
    /// The object identifier of the NIST P-256 curve.
    /// </summary>
    private const string P256Oid = "1.2.840.10045.3.1.7";

    /// <summary>
    /// The Data Protection purpose of the generated key.
    /// </summary>
    private const string ProtectorPurpose = "KOD.Authentication.JwtSigningKey.v1";

    /// <summary>
    /// The key pair.
    /// </summary>
    private readonly ECDsa _ecdsa;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtSigningKey"/> class.
    /// </summary>
    /// <param name="options">The token options.</param>
    /// <param name="environment">The host environment (content root).</param>
    /// <param name="dataProtection">Protects the generated key at rest.</param>
    /// <param name="logger">The logger.</param>
    public JwtSigningKey(IOptions<JwtOptions> options, IHostEnvironment environment, IDataProtectionProvider dataProtection, ILogger<JwtSigningKey> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(dataProtection);

        var settings = options.Value;
        _ecdsa = string.IsNullOrWhiteSpace(settings.SigningKeyPath)
            ? LoadOrGenerate(Path.GetFullPath(GeneratedKeyFile, environment.ContentRootPath), dataProtection.CreateProtector(ProtectorPurpose), logger)
            : LoadPem(Path.GetFullPath(settings.SigningKeyPath, environment.ContentRootPath), settings.SigningKeyPassword);

        var key = new ECDsaSecurityKey(_ecdsa);
        key.KeyId = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
        SecurityKey = key;
        SigningCredentials = new SigningCredentials(key, Algorithm);
    }

    /// <summary>
    /// Gets the key (with its <c>kid</c>) that verifies access tokens.
    /// </summary>
    public SecurityKey SecurityKey { get; }

    /// <summary>
    /// Gets the credentials that sign access tokens.
    /// </summary>
    public SigningCredentials SigningCredentials { get; }

    /// <inheritdoc />
    public void Dispose() => _ecdsa.Dispose();

    /// <summary>
    /// Loads a P-256 private key from a PEM file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="password">The password of an encrypted PKCS#8 key.</param>
    /// <returns>The key.</returns>
    private static ECDsa LoadPem(string path, string? password)
    {
        var pem = File.ReadAllText(path);
        var ecdsa = ECDsa.Create();
        try
        {
            if (string.IsNullOrEmpty(password))
            {
                ecdsa.ImportFromPem(pem);
            }
            else
            {
                ecdsa.ImportFromEncryptedPem(pem, password);
            }

            EnsureP256PrivateKey(ecdsa, path);
            return ecdsa;
        }
        catch
        {
            ecdsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Loads the generated key, generating and storing it on first use (or when it can no longer be decrypted).
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="protector">The Data Protection protector.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The key.</returns>
    private static ECDsa LoadOrGenerate(string path, IDataProtector protector, ILogger logger)
    {
        if (File.Exists(path))
        {
            var ecdsa = ECDsa.Create();
            try
            {
                ecdsa.ImportPkcs8PrivateKey(protector.Unprotect(Convert.FromBase64String(File.ReadAllText(path).Trim())), out _);
                EnsureP256PrivateKey(ecdsa, path);
                return ecdsa;
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException)
            {
                // The Data Protection keys were lost: a new key only means every access token is renewed once.
                ecdsa.Dispose();
                LogKeyUnreadable(logger, path, exception);
            }
        }

        var generated = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Convert.ToBase64String(protector.Protect(generated.ExportPkcs8PrivateKey())));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        LogKeyGenerated(logger, path);
        return generated;
    }

    /// <summary>
    /// Rejects keys that are not P-256 private keys (ES256 is defined only for P-256, RFC 7518 §3.4).
    /// </summary>
    /// <param name="ecdsa">The key.</param>
    /// <param name="source">The file the key came from.</param>
    private static void EnsureP256PrivateKey(ECDsa ecdsa, string source)
    {
        var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
        var oid = parameters.Curve.Oid;
        var isP256 = parameters.Curve.IsNamed
            && (oid.Value == P256Oid || oid.FriendlyName is "nistP256" or "ECDSA_P256" or "secp256r1");
        CryptographicOperations.ZeroMemory(parameters.D);
        if (!isP256)
        {
            throw new InvalidOperationException($"The JWT signing key in '{source}' must be an ECDSA P-256 private key (ES256).");
        }
    }

    /// <summary>
    /// Logs the generation of a signing key.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The key file.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Generated a new JWT signing key (ES256) in {Path}")]
    private static partial void LogKeyGenerated(ILogger logger, string path);

    /// <summary>
    /// Logs a generated key that cannot be decrypted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">The key file.</param>
    /// <param name="exception">The exception.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "The JWT signing key in {Path} cannot be read; a new key is generated")]
    private static partial void LogKeyUnreadable(ILogger logger, string path, Exception exception);
}
