using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Audit;

namespace ODC.UnitTests.Architecture;

/// <summary>
/// Every entity is placed in the master database or in the organizations' one (<see cref="DataPlacement"/>), each
/// database's model holds exactly its own entities, and nothing in one database refers to a table of the other: the
/// organizations' databases must work without joining the master database, and a copy of one of them must not reveal
/// the others (OWASP: classify every tenant-owned table).
/// </summary>
public sealed class DataPlacementTests
{
    /// <summary>
    /// The entities placed in both databases.
    /// </summary>
    private static readonly HashSet<Type> InBoth = [typeof(AuditEntry)];

    [Fact]
    public void Every_entity_configuration_is_placed_in_a_database()
    {
        var configurations = typeof(MasterDbContext).Assembly.GetTypes()
            .Where(static t => t is { IsClass: true, IsAbstract: false } && DataPlacement.ConfiguredEntities(t).Any())
            .ToList();

        Assert.NotEmpty(configurations);
        Assert.All(configurations, configuration => Assert.True(
            DataPlacement.Configures(configuration, DataPlacement.Master) || DataPlacement.Configures(configuration, DataPlacement.Organization),
            $"{configuration.Name} configures {string.Join(", ", DataPlacement.ConfiguredEntities(configuration).Select(static e => e.Name))}: place them in DataPlacement (one database per configuration class)."));
    }

    [Fact]
    public void The_master_model_holds_exactly_the_master_entities()
        => AssertModelHolds(Model(new MasterDbContext(Options<MasterDbContext>())), DataPlacement.Master);

    [Fact]
    public void The_organization_model_holds_exactly_the_organization_entities()
        => AssertModelHolds(Model(new OrganizationDbContext(Options<OrganizationDbContext>())), DataPlacement.Organization);

    [Fact]
    public void Only_the_audit_trail_is_kept_in_both_databases()
        => Assert.Equal(InBoth, DataPlacement.Master.Intersect(DataPlacement.Organization).ToHashSet());

    [Fact]
    public void No_table_name_is_used_by_both_databases_except_the_audit_trail()
    {
        var master = Model(new MasterDbContext(Options<MasterDbContext>())).GetEntityTypes().Where(static t => !InBoth.Contains(t.ClrType)).Select(static t => t.GetTableName()).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var organization = Model(new OrganizationDbContext(Options<OrganizationDbContext>())).GetEntityTypes().Where(static t => !InBoth.Contains(t.ClrType)).Select(static t => t.GetTableName()).OfType<string>().ToHashSet(StringComparer.Ordinal);

        Assert.Empty(master.Intersect(organization));
    }

    /// <summary>
    /// Checks that a model has the placed entities and nothing else (owned and complex types aside), and that every
    /// relationship stays inside it.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="placed">The entities placed in its database.</param>
    private static void AssertModelHolds(IModel model, IReadOnlySet<Type> placed)
    {
        var entities = model.GetEntityTypes().Where(static t => !t.IsOwned() && !t.HasSharedClrType).Select(static t => t.ClrType).ToHashSet();

        Assert.Empty(entities.Except(placed).Select(static t => t.Name));
        Assert.Empty(placed.Except(entities).Select(static t => t.Name));
        Assert.All(
            model.GetEntityTypes().SelectMany(static t => t.GetForeignKeys()),
            key => Assert.Contains(key.PrincipalEntityType.ClrType, placed));
    }

    /// <summary>
    /// Builds a context's model (no connection is opened).
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The model.</returns>
    private static IModel Model(DbContext context)
    {
        using (context)
        {
            return context.Model;
        }
    }

    /// <summary>
    /// The options of a context on PostgreSQL, never connected.
    /// </summary>
    /// <typeparam name="TContext">The context.</typeparam>
    /// <returns>The options.</returns>
    private static DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>().UseNpgsql("Host=localhost;Database=odc_never_opened").UseSnakeCaseNamingConvention().Options;
}
