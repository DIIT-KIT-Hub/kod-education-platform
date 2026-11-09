using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Auth;

namespace ODC.IntegrationTests.Auth;

/// <summary>
/// The sign-in through the real endpoints and cookies: password, the mandatory authenticator, single-use codes, the
/// lockout and the photo requirement.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class SignInTests(OdcApplication app)
{
    [Fact]
    public async Task First_sign_in_enrolls_an_authenticator_and_then_asks_for_its_codes()
    {
        using var first = await app.Accounts.SignInAsync(Demo.Engineer);
        Assert.Equal(Demo.Engineer, first.Context?.User?.Email);
        Assert.NotNull(first.Context?.User?.PhotoId);

        using var second = app.CreateApiClient();
        var login = await second.PostAsync<LoginResponse>("/api/v1/auth/login", new LoginRequest(Demo.Engineer, app.DemoPassword, RememberMe: false));
        Assert.Equal(LoginResponse.TwoFactor, login.Next);
        await app.Accounts.CompleteSecondFactorAsync(second, Demo.Engineer, login.Next);
        Assert.Equal(Demo.Engineer, second.Context?.User?.Email);
    }

    [Fact]
    public async Task An_authenticator_code_works_only_once()
    {
        using var first = await app.Accounts.SignInAsync(Demo.HeadOfIt);

        using var second = app.CreateApiClient();
        await second.PostAsync<LoginResponse>("/api/v1/auth/login", new LoginRequest(Demo.HeadOfIt, app.DemoPassword, RememberMe: false));
        using var replayed = await second.PostAsync("/api/v1/auth/login/two-factor", new CodeRequest(app.Accounts.LastCode(Demo.HeadOfIt), RememberMe: false));

        Assert.Equal(HttpStatusCode.BadRequest, replayed.StatusCode);
        Assert.Null((await second.RefreshContextAsync()).User);
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_address_get_the_same_answer()
    {
        using var client = app.CreateApiClient();

        using var wrong = await client.PostAsync("/api/v1/auth/login", new LoginRequest(Demo.Clerk, "Not-The-Password-1", RememberMe: false));
        using var unknown = await client.PostAsync("/api/v1/auth/login", new LoginRequest("nobody@odc.local", "Not-The-Password-1", RememberMe: false));

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(await Detail(wrong), await Detail(unknown));
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_without_saying_so()
    {
        using var client = app.CreateApiClient();
        string? refusal = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var wrong = await client.PostAsync("/api/v1/auth/login", new LoginRequest(Demo.LockedOut, "Not-The-Password-1", RememberMe: false));
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
            refusal = await Detail(wrong);
        }

        // The right password is refused with the same message while the lockout lasts (Identity counts it in real
        // time, 15 minutes, so this account stays locked for the rest of the run).
        using var locked = await client.PostAsync("/api/v1/auth/login", new LoginRequest(Demo.LockedOut, app.DemoPassword, RememberMe: false));
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
        Assert.Equal(refusal, await Detail(locked));
        Assert.Null((await client.RefreshContextAsync()).User);
    }

    [Fact]
    public async Task The_api_waits_for_the_profile_photo()
    {
        // A new account: the demo ones may already have a photo from another test.
        const string email = "no.photo.yet@odc.local";
        using (var admin = await app.Accounts.AdminAsync())
        {
            await People.CreateUserAsync(admin, email, "HR", ODC.Web.Shared.Domain.Access.SystemRoles.Employee);
            await app.Mailbox.WaitForAsync(email);
        }

        using var client = await app.Accounts.ActivateAsync(email, "Photo-Comes-Later-7", withPhoto: false);

        using var blocked = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        using var signInStillWorks = await client.GetAsync("/api/v1/auth/context");
        Assert.Equal(HttpStatusCode.OK, signInStillWorks.StatusCode);

        await Accounts.EnsurePhotoAsync(client);
        using var allowed = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Requests_without_a_sign_in_are_refused()
    {
        using var client = app.CreateApiClient();

        using var response = await client.GetAsync("/api/v1/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Reads the <c>detail</c> of a problem details answer.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <returns>The detail.</returns>
    private static async Task<string> Detail(HttpResponseMessage response)
    {
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }
}
