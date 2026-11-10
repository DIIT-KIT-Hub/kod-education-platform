using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Admin.Users;
using ODC.Web.Shared.Domain.Access;

namespace ODC.IntegrationTests.Admin;

/// <summary>
/// User administration end to end: the invitation, the activation of the new account, and the organization scope of an
/// administrator of one organization.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class UserAdministrationTests(OdcApplication app)
{
    [Fact]
    public async Task A_new_employee_is_invited_activates_the_account_and_starts_working()
    {
        const string email = "new.employee@odc.local";
        using var admin = await app.Accounts.AdminAsync();

        var id = await People.CreateUserAsync(admin, email, "FUND-PROJ", SystemRoles.Employee, "FUND");
        var invitation = await app.Mailbox.WaitForAsync(email);
        var waiting = await People.FindAsync(admin, email, "FUND");
        Assert.True(waiting?.AwaitsActivation);

        using var employee = await app.Accounts.ActivateAsync(email, "New-Employee-Pass-42");

        Assert.Equal(id, employee.Context?.User?.Id);
        Assert.Contains("/account/activate", invitation.TextBody, StringComparison.Ordinal);
        using var documents = await employee.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, documents.StatusCode);
        var active = await People.FindAsync(admin, email, "FUND");
        Assert.False(active?.AwaitsActivation);
        Assert.True(active?.TwoFactorEnabled);
    }

    [Fact]
    public async Task An_hr_specialist_sees_and_manages_only_the_people_of_their_organization()
    {
        using var admin = await app.Accounts.AdminAsync();
        using var hr = await app.Accounts.SignInAsync(Demo.Hr);

        var fundHead = await People.FindAsync(admin, Demo.FundHead);
        Assert.NotNull(fundHead);
        Assert.NotNull(await People.FindAsync(hr, Demo.Engineer));
        Assert.Null(await People.FindAsync(hr, Demo.FundHead));

        using var card = await hr.GetAsync($"/api/v1/users/{fundHead.Id}");
        Assert.Equal(HttpStatusCode.NotFound, card.StatusCode);

        var units = await hr.GetAsync<List<ODC.Web.Shared.Models.UnitOption>>("/api/v1/users/units");
        Assert.DoesNotContain(units, u => u.Code.StartsWith("FUND", StringComparison.Ordinal));
    }
}
