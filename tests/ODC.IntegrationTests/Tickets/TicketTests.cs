using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Tickets;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Shared.Paging;

namespace ODC.IntegrationTests.Tickets;

/// <summary>
/// Requests to the platform support through the API: the author, the handlers and everybody else.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class TicketTests(OdcApplication app)
{
    [Fact]
    public async Task Support_answers_a_request_and_only_the_author_and_support_see_it()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var admin = await app.Accounts.AdminAsync();
        using var fundHead = await app.Accounts.SignInAsync(Demo.FundHead);

        using var submitted = await engineer.SendFormAsync(
            HttpMethod.Post,
            "/api/v1/tickets",
            [
                ("Category", nameof(TicketCategory.Access)),
                ("Priority", nameof(TicketPriority.High)),
                ("Subject", "Немає доступу до архіву"),
                ("Description", "Після оновлення не відкривається архів документів."),
            ]);
        var id = (await ApiClient.ReadAsync<TicketIdResponse>(submitted)).Id;

        var all = await admin.GetAsync<PagedResult<TicketListItem>>("/api/v1/tickets?pageSize=100");
        Assert.Contains(all.Items, t => t.Id == id && t.Status == TicketStatus.New);
        using var answered = await admin.SendFormAsync(HttpMethod.Post, $"/api/v1/tickets/{id}/messages", [("Body", "Доступ відновлено, перевірте, будь ласка.")]);
        await ApiClient.ExpectAsync(answered, HttpStatusCode.NoContent);

        var details = await engineer.GetAsync<TicketDetails>($"/api/v1/tickets/{id}");
        Assert.Equal(TicketStatus.InProgress, details.Status);
        Assert.Contains(details.Messages, m => !m.FromAuthor && m.Body.Contains("Доступ відновлено", StringComparison.Ordinal));
        var mine = await engineer.GetAsync<PagedResult<TicketListItem>>("/api/v1/tickets/mine?pageSize=100");
        Assert.Contains(mine.Items, t => t.Id == id);

        using var foreign = await fundHead.GetAsync($"/api/v1/tickets/{id}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var list = await fundHead.GetAsync("/api/v1/tickets");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }
}
