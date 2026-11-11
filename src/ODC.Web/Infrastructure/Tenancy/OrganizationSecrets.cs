using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Encrypts the secrets of the registry with Data Protection, a purpose for every organization: the passwords of its
/// PostgreSQL roles and the key of its files. The Data Protection keys are protected outside the database (a certificate
/// or DPAPI), so a dump of the master database opens neither the organizations' databases nor their files.
/// </summary>
/// <param name="protection">Data Protection.</param>
public sealed class OrganizationSecrets(IDataProtectionProvider protection)
{
    /// <summary>
    /// The owner of the platform's files (requests to the support, images).
    /// </summary>
    public const string Platform = "platform";

    /// <summary>
    /// The owner name of an organization's files.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The owner name.</returns>
    public static string OwnerOf(Guid organizationId) => organizationId.ToString("N", CultureInfo.InvariantCulture);

    /// <summary>
    /// Encrypts the password of a role of an organization.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="password">The password.</param>
    /// <returns>The encrypted password.</returns>
    public string ProtectPassword(Guid organizationId, string password) => Passwords(organizationId).Protect(password);

    /// <summary>
    /// Decrypts the password of a role of an organization.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="protectedPassword">The encrypted password.</param>
    /// <returns>The password.</returns>
    public string UnprotectPassword(Guid organizationId, string protectedPassword) => Passwords(organizationId).Unprotect(protectedPassword);

    /// <summary>
    /// Encrypts the key of an owner's files.
    /// </summary>
    /// <param name="owner">The organization's identifier or <see cref="Platform"/>.</param>
    /// <param name="key">The key.</param>
    /// <returns>The encrypted key.</returns>
    public string ProtectFileKey(string owner, byte[] key) => Convert.ToBase64String(FileKeys(owner).Protect(key));

    /// <summary>
    /// Decrypts the key of an owner's files.
    /// </summary>
    /// <param name="owner">The organization's identifier or <see cref="Platform"/>.</param>
    /// <param name="protectedKey">The encrypted key.</param>
    /// <returns>The key.</returns>
    public byte[] UnprotectFileKey(string owner, string protectedKey) => FileKeys(owner).Unprotect(Convert.FromBase64String(protectedKey));

    /// <summary>
    /// The protector of an organization's passwords.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The protector.</returns>
    private IDataProtector Passwords(Guid organizationId) => protection.CreateProtector("ODC.Organizations.Database", OwnerOf(organizationId));

    /// <summary>
    /// The protector of an owner's file key.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns>The protector.</returns>
    private IDataProtector FileKeys(string owner) => protection.CreateProtector("ODC.Files", owner);
}
