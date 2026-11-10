using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.UnitTests.Application;

/// <summary>
/// Creates organizations in the one in-memory organizations' database of the tests (production creates a database for
/// each).
/// </summary>
/// <param name="db">The organizations' database.</param>
internal sealed class FakeProvisioning(OrganizationDbContext db) : IOrganizationProvisioning
{
    /// <summary>
    /// Gets the codes the organizations were renamed to.
    /// </summary>
    public List<string> Renamed { get; } = [];

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(string code, string name, string? shortName, Guid typeId, int sortOrder, CancellationToken cancellationToken)
    {
        var root = OrganizationUnit.Root(Guid.CreateVersion7(), code, name, typeId);
        if (await db.OrganizationUnits.AnyAsync(u => u.ParentId == null && u.Code == root.Code, cancellationToken))
        {
            return Error.Conflict(Texts.T("Організація з кодом «{0}» вже існує.", root.Code));
        }

        root.Update(code, name, shortName, typeId, sortOrder);
        db.OrganizationUnits.Add(root);
        await db.SaveChangesAsync(cancellationToken);
        return root.Id;
    }

    /// <inheritdoc />
    public Task RenameAsync(Guid id, string code, string name, string? shortName, CancellationToken cancellationToken)
    {
        Renamed.Add(code);
        return Task.CompletedTask;
    }
}
