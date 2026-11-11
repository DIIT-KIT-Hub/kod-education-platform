using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// The life of an organization (OWASP: tenant onboarding and offboarding): creation (the registry row in the state
/// <see cref="OrganizationState.Creating"/> with new secrets, the roles, the database, the migrations, the root unit,
/// then <see cref="OrganizationState.Active"/>), suspension, resumption, rotation of the passwords and deletion (the
/// database, its roles, its files and its key). A failed creation removes what was created; every step can run again,
/// so a start finishes an interrupted creation or deletion.
/// </summary>
/// <param name="master">The master database.</param>
/// <param name="databases">Where the databases of organizations are.</param>
/// <param name="admin">Creates and drops databases and roles.</param>
/// <param name="secrets">Encrypts the passwords.</param>
/// <param name="keys">The keys of the files.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="scopes">Runs work in an organization.</param>
/// <param name="cache">The access cache.</param>
/// <param name="identity">Ends sessions.</param>
/// <param name="storage">The storage settings.</param>
/// <param name="environment">The host environment.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class OrganizationProvisioning(
    MasterDbContext master,
    OrganizationDatabases databases,
    OrganizationDatabaseAdmin admin,
    OrganizationSecrets secrets,
    FileKeys keys,
    OrganizationRegistry registry,
    OrganizationScopes scopes,
    AccessCache cache,
    IIdentityService identity,
    IOptions<StorageOptions> storage,
    IHostEnvironment environment,
    TimeProvider timeProvider) : IOrganizationProvisioning
{
    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(string code, string name, string? shortName, Guid typeId, int sortOrder, CancellationToken cancellationToken)
    {
        var normalized = Guard.Code(code, OrganizationUnit.CodeMaxLength);
        var databaseName = databases.NameFor(normalized);
        if (await master.Organizations.AnyAsync(o => o.Code == normalized || o.DatabaseName == databaseName, cancellationToken))
        {
            return Error.Conflict(Texts.T("Організація з кодом «{0}» вже існує.", normalized));
        }

        var organization = new Organization(Guid.CreateVersion7(), normalized, name, databaseName, shortName);
        organization.BeginCreation(
            databases.SeparateRoles ? secrets.ProtectPassword(organization.Id, NewPassword()) : null,
            databases.SeparateRoles ? secrets.ProtectPassword(organization.Id, NewPassword()) : null,
            keys.NewFor(organization.Id));
        master.Organizations.Add(organization);
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();

        try
        {
            await CompleteCreationAsync(organization, typeId, sortOrder, cancellationToken);
        }
        catch
        {
            await RemoveAsync(organization, CancellationToken.None);
            throw;
        }

        return organization.Id;
    }

    /// <summary>
    /// Finishes the creation of an organization: the roles, the database, the migrations and the root unit, whatever of
    /// them is missing, then opens it.
    /// </summary>
    /// <param name="organization">The organization (tracked by the master context of this scope).</param>
    /// <param name="typeId">The unit type of the root.</param>
    /// <param name="sortOrder">The display order of the root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task CompleteCreationAsync(Organization organization, Guid typeId, int sortOrder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        await admin.CreateAsync(organization, cancellationToken);
        organization.Migrated(await admin.MigrateAsync(organization, cancellationToken), timeProvider.GetUtcNow());
        await scopes.RunAsync(organization, async services =>
        {
            var db = services.GetRequiredService<OrganizationDbContext>();
            if (!await db.OrganizationUnits.AnyAsync(u => u.Id == organization.Id, cancellationToken))
            {
                var root = OrganizationUnit.Root(organization.Id, organization.Code, organization.Name, typeId);
                root.Update(organization.Code, organization.Name, organization.ShortName, typeId, sortOrder);
                db.OrganizationUnits.Add(root);
                await db.SaveChangesAsync(cancellationToken);
            }
        });

        organization.Activate();
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
    }

    /// <inheritdoc />
    public async Task RenameAsync(Guid id, string code, string name, string? shortName, CancellationToken cancellationToken)
    {
        if (await master.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) is not { } organization)
        {
            return;
        }

        organization.Rename(code, name, shortName);
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
    }

    /// <summary>
    /// Suspends an organization: no requests, its database refuses the application role, and people who work only there
    /// are signed out everywhere.
    /// </summary>
    /// <param name="id">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or an error.</returns>
    public async Task<Result<Organization>> SuspendAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await master.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) is not { } organization)
        {
            return Error.NotFound(Texts.T("Організацію не знайдено."));
        }

        if (await master.Organizations.CountAsync(static o => o.State == OrganizationState.Active, cancellationToken) == 1 && organization.IsActive)
        {
            return Error.Validation(Texts.T("Не можна призупинити останню активну організацію."));
        }

        organization.Suspend();
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
        cache.Changed();

        var people = await master.Memberships.Where(m => m.OrganizationId == id).Select(static m => m.UserId).ToListAsync(cancellationToken);
        var elsewhere = await master.Memberships
            .Where(m => people.Contains(m.UserId) && m.OrganizationId != id)
            .Join(master.Organizations.Where(static o => o.State == OrganizationState.Active), static m => m.OrganizationId, static o => o.Id, static (m, _) => m.UserId)
            .ToListAsync(cancellationToken);
        foreach (var user in await master.Users.Where(u => people.Contains(u.Id) && !elsewhere.Contains(u.Id)).ToListAsync(cancellationToken))
        {
            await identity.InvalidateSessionsAsync(user);
        }

        await admin.LockAsync(organization, cancellationToken);
        return organization;
    }

    /// <summary>
    /// Opens a suspended organization again.
    /// </summary>
    /// <param name="id">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or an error.</returns>
    public async Task<Result<Organization>> ResumeAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await master.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) is not { } organization)
        {
            return Error.NotFound(Texts.T("Організацію не знайдено."));
        }

        if (organization.State != OrganizationState.Suspended)
        {
            return Error.Validation(Texts.T("Відновити можна лише призупинену організацію."));
        }

        await admin.UnlockAsync(organization, cancellationToken);
        organization.Activate();
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
        cache.Changed();

        // Profiles changed while it was suspended were not copied there.
        var people = await master.Memberships.Where(m => m.OrganizationId == id).Select(static m => m.UserId)
            .Union(master.Participations.Where(p => p.OrganizationId == id).Select(static p => p.UserId))
            .ToListAsync(cancellationToken);
        var profiles = (await master.Users.AsNoTracking().Where(u => people.Contains(u.Id)).ToListAsync(cancellationToken)).Select(MemberProfile.Of).ToList();
        await scopes.RunAsync(organization, services => MemberSync.CopyAsync(services.GetRequiredService<OrganizationDbContext>(), profiles, cancellationToken));
        return organization;
    }

    /// <summary>
    /// Deletes a suspended organization for good: its database, roles, files and key, its memberships, participations
    /// and news in the index. Backups made before stay with the operator.
    /// </summary>
    /// <param name="id">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deleted organization, or an error.</returns>
    public async Task<Result<Organization>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await master.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) is not { } organization)
        {
            return Error.NotFound(Texts.T("Організацію не знайдено."));
        }

        organization.BeginDeletion();
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
        await RemoveAsync(organization, cancellationToken);
        return organization;
    }

    /// <summary>
    /// Finishes the deletion of an organization (also of one whose creation failed): whatever of it still exists.
    /// </summary>
    /// <param name="organization">The organization (tracked by the master context of this scope).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task RemoveAsync(Organization organization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        await admin.DropAsync(organization, cancellationToken);
        var folder = StorageOptions.OrganizationFolder(StorageOptions.RootOf(storage.Value, environment), organization.Id);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        await master.GeneralArticles.Where(a => a.OrganizationId == organization.Id).ExecuteDeleteAsync(cancellationToken);
        master.Organizations.Remove(organization);
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
        cache.Changed();
    }

    /// <summary>
    /// Gives the roles of an organization new random passwords.
    /// </summary>
    /// <param name="id">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization, or an error.</returns>
    public async Task<Result<Organization>> RotatePasswordsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await master.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken) is not { } organization)
        {
            return Error.NotFound(Texts.T("Організацію не знайдено."));
        }

        if (organization.OwnerPassword is null)
        {
            return Error.Validation(Texts.T("База цієї організації працює з обліковим записом майстер-бази."));
        }

        string owner = NewPassword(), app = NewPassword();
        await admin.ChangePasswordsAsync(organization, owner, app, cancellationToken);
        organization.ChangePasswords(secrets.ProtectPassword(organization.Id, owner), secrets.ProtectPassword(organization.Id, app));
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();
        return organization;
    }

    /// <summary>
    /// A random password of a role (256 bits, URL-safe).
    /// </summary>
    /// <returns>The password.</returns>
    private static string NewPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
