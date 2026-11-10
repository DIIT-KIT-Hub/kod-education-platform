using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Creates an organization: the registry row, its database (created by the migrations) and the root unit there. All or
/// nothing: when a step fails, what was created is removed again (OWASP: tenant onboarding).
/// </summary>
/// <param name="master">The master database.</param>
/// <param name="databases">Where the databases of organizations are.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="scopes">Runs work in an organization.</param>
internal sealed class OrganizationProvisioning(
    MasterDbContext master,
    OrganizationDatabases databases,
    OrganizationRegistry registry,
    OrganizationScopes scopes) : IOrganizationProvisioning
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
        master.Organizations.Add(organization);
        await master.SaveChangesAsync(cancellationToken);
        registry.Invalidate();

        try
        {
            await scopes.RunAsync(organization, async services =>
            {
                var db = services.GetRequiredService<OrganizationDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
                var root = OrganizationUnit.Root(organization.Id, normalized, name, typeId);
                root.Update(normalized, name, shortName, typeId, sortOrder);
                db.OrganizationUnits.Add(root);
                await db.SaveChangesAsync(cancellationToken);
            });
        }
        catch
        {
            await scopes.RunAsync(organization, services => services.GetRequiredService<OrganizationDbContext>().Database.EnsureDeletedAsync(CancellationToken.None));
            master.Organizations.Remove(organization);
            await master.SaveChangesAsync(CancellationToken.None);
            registry.Invalidate();
            throw;
        }

        return organization.Id;
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
}
