using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Documents;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Chats;
using ODC.Web.Features.Notifications;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Paging;

namespace ODC.IntegrationTests.Tenancy;

/// <summary>
/// The isolation matrix: people of the first organization (an employee, an HR specialist, the director) try every way to
/// read or change the fund's work — its document, the file, a chat of two fund employees, a unit, a person, a
/// notification — at their own addresses and at the fund's (<c>/o/FUND/…</c>, where they would be guests). Nothing is
/// answered and nothing changes. Requests of two organizations through the same pools never mix, and an organization's
/// address without a sign-in opens nothing.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class IsolationMatrixTests(OdcApplication app)
{
    /// <summary>
    /// The text of the fund's document (must never reach anyone of the first organization).
    /// </summary>
    private const string SecretDocument = "Конфіденційний кошторис фонду на зиму";

    /// <summary>
    /// The text of the fund's chat.
    /// </summary>
    private const string SecretMessage = "Пароль від складу фонду — у сейфі";

    [Fact]
    public async Task People_of_one_organization_neither_read_nor_change_the_work_of_another()
    {
        var fund = await FundWorkAsync();
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var hr = await app.Accounts.SignInAsync(Demo.Hr);
        using var director = await app.Accounts.SignInAsync(Demo.Director);
        var attempts = new List<string>();

        foreach (var (who, client) in new[] { ("engineer", engineer), ("hr", hr), ("director", director) })
        {
            foreach (var prefix in new[] { string.Empty, "/o/FUND" })
            {
                foreach (var (method, path, body) in Attempts(fund))
                {
                    using var response = body is null
                        ? await client.SendAsync(method, prefix + path)
                        : await client.SendJsonAsync(method, prefix + path, body);
                    var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                    if (response.IsSuccessStatusCode || text.Contains(SecretDocument, StringComparison.Ordinal) || text.Contains(SecretMessage, StringComparison.Ordinal))
                    {
                        attempts.Add($"{who} {method} {prefix}{path} → {(int)response.StatusCode}");
                    }
                }
            }

            // Their own lists know nothing of the fund's work either.
            Assert.DoesNotContain(fund.DocumentId.ToString(), await TextAsync(client, "/api/v1/documents?PageSize=100"), StringComparison.Ordinal);
            Assert.DoesNotContain(fund.ChatId.ToString(), await TextAsync(client, "/api/v1/chats"), StringComparison.Ordinal);
            Assert.DoesNotContain(fund.NotificationId.ToString(), await TextAsync(client, "/api/v1/notifications?PageSize=100"), StringComparison.Ordinal);
        }

        Assert.True(attempts.Count == 0, "Answered or leaked:" + Environment.NewLine + string.Join(Environment.NewLine, attempts));

        // Everything is as it was for the fund.
        using var head = await app.Accounts.SignInAsync(Demo.FundHead);
        using var employee = await app.Accounts.SignInAsync(Demo.FundEmployee);
        Assert.Contains(SecretDocument, await TextAsync(head, $"/api/v1/documents/{fund.DocumentId}"), StringComparison.Ordinal);
        Assert.Contains(SecretMessage, await TextAsync(head, $"/api/v1/chats/{fund.ChatId}/messages"), StringComparison.Ordinal);
        Assert.Contains("Відділ проєктів", await TextAsync(head, $"/api/v1/organization-units/{fund.UnitId}"), StringComparison.Ordinal);
        var unread = await employee.GetAsync<PagedResult<NotificationDto>>("/api/v1/notifications?UnreadOnly=true&PageSize=100");
        Assert.Contains(unread.Items, n => n.Id == fund.NotificationId);
    }

    [Fact]
    public async Task Requests_of_two_organizations_through_the_same_pools_never_mix()
    {
        using var admin = await app.Accounts.AdminAsync();
        var requests = Enumerable.Range(0, 40).Select(async i =>
        {
            var code = i % 2 == 0 ? "ORG" : "FUND";
            var units = await admin.GetAsync<JsonElement>($"/o/{code}/api/v1/organization-units");
            var root = units.EnumerateArray().Single();
            return (Asked: code, Got: root.GetProperty("code").GetString());
        });

        foreach (var (asked, got) in await Task.WhenAll(requests))
        {
            Assert.Equal(asked, got);
        }
    }

    [Fact]
    public async Task An_organization_address_without_a_sign_in_opens_nothing()
    {
        using var visitor = app.CreateApiClient();
        foreach (var path in new[] { "/o/FUND/api/v1/organization-units", "/o/FUND/api/v1/documents", "/o/FUND/api/v1/chats", "/o/ORG/api/v1/users" })
        {
            using var response = await visitor.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    /// <summary>
    /// The requests of the matrix (relative to an organization's address).
    /// </summary>
    /// <param name="fund">The fund's work.</param>
    /// <returns>The method, the address and a JSON body (null: none).</returns>
    private static IEnumerable<(HttpMethod Method, string Path, object? Body)> Attempts(FundWork fund) =>
    [
        (HttpMethod.Get, $"/api/v1/documents/{fund.DocumentId}", null),
        (HttpMethod.Get, $"/api/v1/documents/{fund.DocumentId}/file", null),
        (HttpMethod.Get, $"/api/v1/documents/{fund.DocumentId}/signatures", null),
        (HttpMethod.Get, $"/api/v1/documents/{fund.DocumentId}/edit", null),
        (HttpMethod.Post, $"/api/v1/documents/{fund.DocumentId}/signing-session", null),
        (HttpMethod.Post, $"/api/v1/documents/{fund.DocumentId}/rejections", new { reason = "Чужа відмова" }),
        (HttpMethod.Delete, $"/api/v1/documents/{fund.DocumentId}", null),
        (HttpMethod.Get, $"/api/v1/chats/{fund.ChatId}", null),
        (HttpMethod.Get, $"/api/v1/chats/{fund.ChatId}/messages", null),
        (HttpMethod.Put, $"/api/v1/chats/{fund.ChatId}/title", new { title = "Чужа назва" }),
        (HttpMethod.Put, $"/api/v1/chats/{fund.ChatId}/read", null),
        (HttpMethod.Post, $"/api/v1/chats/{fund.ChatId}/members", new { userIds = new[] { fund.EmployeeId } }),
        (HttpMethod.Delete, $"/api/v1/chats/{fund.ChatId}", null),
        (HttpMethod.Get, $"/api/v1/organization-units/{fund.UnitId}", null),
        (HttpMethod.Get, $"/api/v1/organization-units/{fund.UnitId}/staff", null),
        (HttpMethod.Put, $"/api/v1/organization-units/{fund.UnitId}", new { code = "FUND-PROJ", name = "Захоплений відділ", shortName = (string?)null, typeId = fund.UnitTypeId, parentId = fund.RootId, sortOrder = 1, isActive = true }),
        (HttpMethod.Get, $"/api/v1/users/{fund.EmployeeId}", null),
        (HttpMethod.Put, $"/api/v1/notifications/{fund.NotificationId}/read", null),
        (HttpMethod.Delete, $"/api/v1/notifications/{fund.NotificationId}", null),
    ];

    /// <summary>
    /// Reads an address as text (whatever the status).
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="path">The address.</param>
    /// <returns>The body.</returns>
    private static async Task<string> TextAsync(ApiClient client, string path)
    {
        using var response = await client.GetAsync(path);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates the fund's work: a document of its head for its employee to sign (and with it the employee's
    /// notification), a chat of the two with a message, and finds a unit and the employee.
    /// </summary>
    /// <returns>The identifiers.</returns>
    private async Task<FundWork> FundWorkAsync()
    {
        using var head = await app.Accounts.SignInAsync(Demo.FundHead);
        using var employee = await app.Accounts.SignInAsync(Demo.FundEmployee);
        var employeeId = employee.Context!.User!.Id;
        var document = await DocumentTests.CreateAsync(head, SecretDocument, SecretDocument, employeeId);
        var chat = await head.PostAsync<ChatIdResponse>("/api/v1/chats/direct", new OpenDirectChatRequest(employeeId));
        using (var sent = await head.SendFormAsync(HttpMethod.Post, $"/api/v1/chats/{chat.Id}/messages", [("ClientId", Guid.NewGuid().ToString()), ("Body", $"<p>{SecretMessage}</p>")]))
        {
            await ApiClient.ExpectAsync(sent, HttpStatusCode.OK, HttpStatusCode.Created);
        }

        var notifications = await employee.GetAsync<PagedResult<NotificationDto>>("/api/v1/notifications?UnreadOnly=true&PageSize=100");
        var notification = notifications.Items.First(n => n.EntityId == document);
        var root = (await app.Services.GetRequiredService<OrganizationRegistry>().FindByCodeAsync("FUND", TestContext.Current.CancellationToken))!.Id;
        var tree = await head.GetAsync<JsonElement>("/api/v1/organization-units");
        var unit = tree.EnumerateArray().Single().GetProperty("children").EnumerateArray().First(static u => u.GetProperty("code").GetString() == "FUND-PROJ");
        return new FundWork(document, chat.Id, notification.Id, root, unit.GetProperty("id").GetGuid(), unit.GetProperty("type").GetProperty("id").GetGuid(), employeeId);
    }

    /// <summary>
    /// The fund's work the matrix aims at.
    /// </summary>
    /// <param name="DocumentId">The document.</param>
    /// <param name="ChatId">The chat.</param>
    /// <param name="NotificationId">The employee's notification of the document.</param>
    /// <param name="RootId">The fund's root unit.</param>
    /// <param name="UnitId">A unit of the fund.</param>
    /// <param name="UnitTypeId">Its type.</param>
    /// <param name="EmployeeId">The fund's employee.</param>
    private sealed record FundWork(Guid DocumentId, Guid ChatId, Guid NotificationId, Guid RootId, Guid UnitId, Guid UnitTypeId, Guid EmployeeId);
}
