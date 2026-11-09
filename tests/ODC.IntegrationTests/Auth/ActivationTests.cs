using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Auth.Activation;

namespace ODC.IntegrationTests.Auth;

/// <summary>
/// The account activation by e-mail: nobody hands out passwords, the first administrator included.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class ActivationTests(OdcApplication app)
{
    [Fact]
    public async Task The_first_administrator_activates_the_account_by_email_and_holds_every_permission()
    {
        using var admin = await app.Accounts.AdminAsync();

        var user = admin.Context?.User;
        Assert.Equal(OdcApplication.AdminEmail, user?.Email);
        Assert.Contains("administration.all", user?.Permissions ?? []);
        Assert.Contains("tickets.manage", user?.Permissions ?? []);
    }

    [Fact]
    public async Task A_wrong_code_is_refused_and_the_right_one_still_works()
    {
        const string email = "activation.code@odc.local";
        await CreateAccountAsync(email);

        using var client = app.CreateApiClient();
        await ApiClient.ExpectAsync(await client.PostAsync("/api/v1/auth/activation", new ActivationStartRequest(email)), HttpStatusCode.OK, HttpStatusCode.NoContent);
        var code = await app.Mailbox.WaitForCodeAsync(email);
        var wrong = code == "000000" ? "111111" : "000000";

        using var refused = await client.PostAsync("/api/v1/auth/activation/code", new ActivationCodeRequest(wrong));
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("activation_invalid", await ApiClient.ProblemCodeAsync(refused));

        using var accepted = await client.PostAsync("/api/v1/auth/activation/code", new ActivationCodeRequest(code));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task An_address_without_an_account_gets_the_same_answer_and_no_email()
    {
        using var client = app.CreateApiClient();

        using var unknown = await client.PostAsync("/api/v1/auth/activation", new ActivationStartRequest("ghost@odc.local"));
        using var active = await app.CreateApiClient().PostAsync("/api/v1/auth/activation", new ActivationStartRequest(Demo.Engineer));

        Assert.Equal(active.StatusCode, unknown.StatusCode);
        Assert.Equal(
            await unknown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            (await active.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Replace(Demo.Engineer, "ghost@odc.local", StringComparison.Ordinal));
        await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.False(app.Mailbox.HasMailTo("ghost@odc.local"));
        Assert.False(app.Mailbox.HasMailTo(Demo.Engineer));
    }

    /// <summary>
    /// Creates an account waiting for activation in the first organization (the administrator's form) and takes its
    /// invitation out of the mailbox.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>A task.</returns>
    private async Task CreateAccountAsync(string email)
    {
        using var admin = await app.Accounts.AdminAsync();
        await People.CreateUserAsync(admin, email, "ORG", ODC.Web.Shared.Domain.Access.SystemRoles.Employee);
        await app.Mailbox.WaitForAsync(email);
    }
}
