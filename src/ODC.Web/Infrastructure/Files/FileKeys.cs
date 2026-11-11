using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Infrastructure.Files;

/// <summary>
/// The keys of the files: one per organization (in its registry row) and one of the platform (requests to the support,
/// images; <see cref="PlatformSecret"/>), each encrypted with Data Protection and kept decrypted in memory once used.
/// </summary>
/// <param name="scopes">Creates the scope of the master database.</param>
/// <param name="secrets">Decrypts the keys.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class FileKeys(IServiceScopeFactory scopes, OrganizationSecrets secrets, TimeProvider timeProvider) : IDisposable
{
    /// <summary>
    /// The decrypted keys of organizations by their encrypted form (a new key is a new entry).
    /// </summary>
    private readonly ConcurrentDictionary<string, byte[]> _organizations = new(StringComparer.Ordinal);

    /// <summary>
    /// Serializes the creation of the platform's key.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// The decrypted key of the platform.
    /// </summary>
    private byte[]? _platform;

    /// <summary>
    /// Gets the key of an organization's files.
    /// </summary>
    /// <param name="organization">The organization.</param>
    /// <returns>The key.</returns>
    /// <exception cref="InvalidOperationException">When the organization has no key.</exception>
    public byte[] Of(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        var stored = organization.FileKey ?? throw new InvalidOperationException($"The organization {organization.Code} has no key of its files.");
        return _organizations.GetOrAdd(stored, static (value, state) => state.Secrets.UnprotectFileKey(OrganizationSecrets.OwnerOf(state.Id), value), (Secrets: secrets, organization.Id));
    }

    /// <summary>
    /// Gets the key of the platform's files, creating it the first time.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The key.</returns>
    public async Task<byte[]> PlatformAsync(CancellationToken cancellationToken)
    {
        if (_platform is { } known)
        {
            return known;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_platform is { } again)
            {
                return again;
            }

            await using var scope = scopes.CreateAsyncScope();
            var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var stored = await master.PlatformSecrets.AsNoTracking().FirstOrDefaultAsync(static s => s.Name == PlatformSecret.FileKeysName, cancellationToken);
            if (stored is null)
            {
                var key = EncryptedFile.NewKey();
                master.PlatformSecrets.Add(new PlatformSecret(PlatformSecret.FileKeysName, secrets.ProtectFileKey(OrganizationSecrets.Platform, key), timeProvider.GetUtcNow()));
                try
                {
                    await master.SaveChangesAsync(cancellationToken);
                    return _platform = key;
                }
                catch (DbUpdateException)
                {
                    // Another server created it first: use theirs.
                    master.ChangeTracker.Clear();
                    stored = await master.PlatformSecrets.AsNoTracking().FirstAsync(static s => s.Name == PlatformSecret.FileKeysName, cancellationToken);
                }
            }

            return _platform = secrets.UnprotectFileKey(OrganizationSecrets.Platform, stored.Value);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Creates the encrypted key of a new organization.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The encrypted key.</returns>
    public string NewFor(Guid organizationId) => secrets.ProtectFileKey(OrganizationSecrets.OwnerOf(organizationId), EncryptedFile.NewKey());

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
