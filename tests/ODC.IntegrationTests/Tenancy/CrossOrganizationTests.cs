using System.Net;
using System.Text.Json;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Documents;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Admin.Audit;
using ODC.Web.Features.Chats;
using ODC.Web.Features.Home;
using ODC.Web.Features.News;
using ODC.Web.Features.Notifications;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.IntegrationTests.Tenancy;

/// <summary>
/// A person's work gathered from every organization they work in or take part in (the home page, notifications, chats),
/// people of other organizations in the pickers, the news of the whole cabinet in every organization and the double
/// audit of a platform administrator's work in another organization.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class CrossOrganizationTests(OdcApplication app)
{
    [Fact]
    public async Task The_start_context_names_the_organization_and_where_the_user_may_go()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var admin = await app.Accounts.AdminAsync();

        var context = await engineer.RefreshContextAsync();
        Assert.Equal("ORG", context.Organization?.Code);
        Assert.Contains(context.Organizations, o => o.Code == "ORG" && o.Access == OrganizationAccess.Member);

        // The administrator works in the first organization (a member there) and uses the platform's powers in the fund.
        Assert.Equal(OrganizationAccess.Member, (await admin.RefreshContextAsync()).Organization?.Access);
        var fund = await admin.GetAsync<ODC.Web.Features.Auth.AuthContextResponse>("/o/FUND/api/v1/auth/context");
        Assert.Equal("FUND", fund.Organization?.Code);
        Assert.Equal(OrganizationAccess.Platform, fund.Organization?.Access);
        Assert.Contains(fund.Organizations, o => o.Code == "ORG" && o.Access == OrganizationAccess.Member);
    }

    [Fact]
    public async Task A_signer_from_another_organization_finds_the_document_on_the_home_page_and_in_the_notifications()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var fundHead = await app.Accounts.SignInAsync(Demo.FundHead);
        var id = await DocumentTests.CreateAsync(engineer, "Спільний план заходів", "План", fundHead.Context!.User!.Id);

        // At home (the fund) the head sees what waits in the first organization.
        var home = await fundHead.GetAsync<DashboardSummary>("/api/v1/dashboard");
        Assert.Contains(home.Awaiting, d => d.Document.Id == id && d.OrganizationCode == "ORG");

        var notifications = await fundHead.GetAsync<PagedResult<NotificationDto>>("/api/v1/notifications?UnreadOnly=true");
        var notification = Assert.Single(notifications.Items, n => n.EntityId == id);
        Assert.Equal("ORG", notification.OrganizationCode);
        var unread = await fundHead.GetAsync<UnreadCountResponse>("/api/v1/notifications/unread-count");
        Assert.True(unread.Count >= 1);

        // The guest's organizations now include the first one, where it is read.
        var context = await fundHead.RefreshContextAsync();
        Assert.Contains(context.Organizations, o => o.Code == "ORG" && o.Access == OrganizationAccess.Guest);
        using var read = await fundHead.PutAsync($"/o/ORG/api/v1/notifications/{notification.Id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        var after = await fundHead.GetAsync<PagedResult<NotificationDto>>("/api/v1/notifications?UnreadOnly=true");
        Assert.DoesNotContain(after.Items, n => n.Id == notification.Id);
    }

    [Fact]
    public async Task A_chat_with_a_person_of_another_organization_is_in_both_lists()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var fundEmployee = await app.Accounts.SignInAsync(Demo.FundEmployee);
        var chat = await engineer.PostAsync<ChatIdResponse>("/api/v1/chats/direct", new OpenDirectChatRequest(fundEmployee.Context!.User!.Id));
        using var sent = await engineer.SendFormAsync(HttpMethod.Post, $"/api/v1/chats/{chat.Id}/messages", [("ClientId", Guid.NewGuid().ToString()), ("Body", "<p>Вітаю!</p>")]);
        await ApiClient.ExpectAsync(sent, HttpStatusCode.OK, HttpStatusCode.Created);

        var theirs = await fundEmployee.GetAsync<List<ChatSummary>>("/api/v1/chats");
        var summary = Assert.Single(theirs, c => c.Id == chat.Id);
        Assert.Equal("ORG", summary.OrganizationCode);
        Assert.True(summary.UnreadCount >= 1);
        Assert.Contains(await engineer.GetAsync<List<ChatSummary>>("/api/v1/chats"), c => c.Id == chat.Id && c.OrganizationCode == "ORG");
    }

    [Fact]
    public async Task People_of_other_organizations_are_found_as_signers_and_chat_partners()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var fundHead = await app.Accounts.SignInAsync(Demo.FundHead);
        var fundHeadId = fundHead.Context!.User!.Id;

        var signers = await engineer.GetAsync<List<UserLookupItem>>($"/api/v1/documents/signers?term={Uri.EscapeDataString(Demo.FundHead)}");
        var signer = Assert.Single(signers, p => p.Id == fundHeadId);
        Assert.False(string.IsNullOrEmpty(signer.Position));

        var people = await engineer.GetAsync<List<UserLookupItem>>($"/api/v1/chats/people?term={Uri.EscapeDataString(Demo.FundHead)}");
        Assert.Contains(people, p => p.Id == fundHeadId);
    }

    [Fact]
    public async Task The_news_of_the_whole_cabinet_reach_every_organization_and_the_news_of_one_stay_there()
    {
        using var fundHead = await app.Accounts.SignInAsync(Demo.FundHead);

        var news = await fundHead.GetAsync<PagedResult<NewsListItem>>("/api/v1/news?PageSize=50");
        var general = Assert.Single(news.Items, a => a.Slug == "zapusk-kabinetu-orhanizatsiinykh-dokumentiv");
        Assert.Equal("ORG", general.OrganizationCode);
        Assert.DoesNotContain(news.Items, a => a.Slug == "novyi-poriadok-pohodzhennia-dohovoriv");

        using var article = await fundHead.GetAsync($"/o/ORG/api/v1/news/{general.Slug}");
        Assert.Equal(HttpStatusCode.OK, article.StatusCode);
        using var own = await fundHead.GetAsync("/o/ORG/api/v1/news/novyi-poriadok-pohodzhennia-dohovoriv");
        Assert.Equal(HttpStatusCode.NotFound, own.StatusCode);
    }

    [Fact]
    public async Task What_a_platform_administrator_does_in_another_organization_is_audited_in_both_trails()
    {
        using var admin = await app.Accounts.AdminAsync();
        var fund = (await admin.GetAsync<JsonElement>("/o/FUND/api/v1/organization-units")).EnumerateArray().First();
        var types = await admin.GetAsync<JsonElement>("/api/v1/unit-types");
        var division = (types.ValueKind == JsonValueKind.Array ? types : types.GetProperty("items")).EnumerateArray().First(t => !t.GetProperty("isOrganization").GetBoolean());
        var code = $"FUND-A{Random.Shared.Next(100, 999)}";

        using var created = await admin.PostAsync(
            "/o/FUND/api/v1/organization-units",
            new { code, name = "Відділ аудиту", shortName = (string?)null, typeId = division.GetProperty("id").GetGuid(), parentId = fund.GetProperty("id").GetGuid(), sortOrder = 9 });
        await ApiClient.ExpectAsync(created, HttpStatusCode.Created);

        var trail = await admin.GetAsync<PagedResult<AuditEntryDto>>($"/api/v1/audit-entries?Search={Uri.EscapeDataString(code)}&PageSize=20");
        Assert.Contains(trail.Items, e => e.OrganizationCode == "FUND" && e.Details!.Contains(code, StringComparison.Ordinal));
        Assert.Contains(trail.Items, e => e.OrganizationCode == null && e.Details!.StartsWith("[FUND]", StringComparison.Ordinal));
    }
}
