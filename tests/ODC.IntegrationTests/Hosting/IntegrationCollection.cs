namespace ODC.IntegrationTests.Hosting;

/// <summary>
/// Every integration test: one application and one database for all of them (starting the host, migrating and seeding
/// take seconds), the classes one after another. Tests create the data they look at and never rely on totals.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<OdcApplication>
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Application";
}
