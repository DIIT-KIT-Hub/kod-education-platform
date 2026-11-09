namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The tests that run on PostgreSQL: one test database for all of them (creating the schema takes seconds), and the
/// classes run one after another because every test empties the database first.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabase>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "PostgreSQL";
}
