using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;

namespace ODC.IntegrationTests.Auth;

/// <summary>
/// The session after the sign-in: short access tokens renewed through the refresh cookie, the idle limit, the sign-out
/// and the antiforgery token.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class SessionTests(OdcApplication app)
{
    [Fact]
    public async Task An_expired_access_token_is_renewed_through_the_refresh_cookie()
    {
        using var client = await app.Accounts.SignInAsync(Demo.Deputy);

        app.Clock.Advance(TimeSpan.FromMinutes(6));

        using var response = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Demo.Deputy, (await client.RefreshContextAsync()).User?.Email);
    }

    [Fact]
    public async Task A_session_left_idle_for_over_an_hour_ends()
    {
        using var client = await app.Accounts.SignInAsync(Demo.HeadOfFinance);

        app.Clock.Advance(TimeSpan.FromMinutes(61));

        using var response = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signing_out_ends_the_session_for_good()
    {
        using var client = await app.Accounts.SignInAsync(Demo.Clerk);

        using var signOut = await client.PostAsync("/api/v1/auth/logout", null);
        await ApiClient.ExpectAsync(signOut, HttpStatusCode.NoContent, HttpStatusCode.OK);

        using var right = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, right.StatusCode);

        // The refresh token died with the sign-in: it cannot bring the session back later either.
        app.Clock.Advance(TimeSpan.FromMinutes(6));
        using var later = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, later.StatusCode);
        Assert.Null((await client.RefreshContextAsync()).User);
    }

    [Fact]
    public async Task Changes_without_the_antiforgery_token_are_refused()
    {
        using var client = await app.Accounts.SignInAsync(Demo.Director);

        using var response = await client.SendJsonAsync(HttpMethod.Put, "/api/v1/notifications/read", null, antiforgery: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
