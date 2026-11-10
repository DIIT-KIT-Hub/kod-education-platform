using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Models;

namespace ODC.IntegrationTests.Tenancy;

/// <summary>
/// Every organization in a database of its own: what one organization's database holds, the addresses of organizations
/// (<c>/o/{code}/…</c>) and what a person of one organization gets in another.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class OrganizationIsolationTests(OdcApplication app)
{
    [Fact]
    public async Task Each_organization_keeps_its_structure_in_its_own_database()
    {
        var registry = app.Services.GetRequiredService<OrganizationRegistry>();
        var scopes = app.Services.GetRequiredService<OrganizationScopes>();
        var organizations = await registry.AllAsync(TestContext.Current.CancellationToken);
        var org = Assert.Single(organizations, o => o.Code == "ORG");
        var fund = Assert.Single(organizations, o => o.Code == "FUND");
        Assert.NotEqual(org.DatabaseName, fund.DatabaseName);

        var orgUnits = await scopes.RunAsync(org, services => services.GetRequiredService<OrganizationDbContext>().OrganizationUnits.Select(static u => u.Code).ToListAsync(TestContext.Current.CancellationToken));
        var fundUnits = await scopes.RunAsync(fund, services => services.GetRequiredService<OrganizationDbContext>().OrganizationUnits.Select(static u => u.Code).ToListAsync(TestContext.Current.CancellationToken));

        Assert.Contains("ORG", orgUnits);
        Assert.DoesNotContain(orgUnits, c => c.StartsWith("FUND", StringComparison.Ordinal));
        Assert.Contains("FUND", fundUnits);
        Assert.DoesNotContain("ORG", fundUnits);
    }

    [Fact]
    public async Task An_unknown_organization_answers_not_found()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);

        using var response = await engineer.GetAsync("/o/NOWHERE/api/v1/documents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("organization_not_found", await ApiClient.ProblemCodeAsync(response));
    }

    [Fact]
    public async Task A_person_of_one_organization_gets_nothing_of_another()
    {
        using var hr = await app.Accounts.SignInAsync(Demo.Hr);
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);

        // At home the HR specialist manages people; in the fund they are nobody's colleague.
        using var home = await hr.GetAsync("/api/v1/users/units");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        using var users = await hr.GetAsync("/o/FUND/api/v1/users/units");
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);

        // A guest only signs and chats there: registering documents is not theirs to do.
        using var units = await engineer.GetAsync("/o/FUND/api/v1/documents/author-units");
        Assert.Equal(HttpStatusCode.Forbidden, units.StatusCode);
        var mine = await engineer.GetAsync<List<UnitOption>>("/api/v1/documents/author-units");
        Assert.NotEmpty(mine);
    }
}
