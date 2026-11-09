using System.Net;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Features.Chats;

namespace ODC.IntegrationTests.Chats;

/// <summary>
/// Chats through the API: a one-to-one chat between people of two organizations (which the move to a database per
/// organization has to keep working) and its privacy.
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class ChatTests(OdcApplication app)
{
    [Fact]
    public async Task People_of_two_organizations_talk_in_one_private_chat()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var fundEmployee = await app.Accounts.SignInAsync(Demo.FundEmployee);
        using var clerk = await app.Accounts.SignInAsync(Demo.Clerk);

        var chat = await engineer.PostAsync<ChatIdResponse>("/api/v1/chats/direct", new OpenDirectChatRequest(fundEmployee.Context!.User!.Id));
        using var sent = await engineer.SendFormAsync(
            HttpMethod.Post,
            $"/api/v1/chats/{chat.Id}/messages",
            [("ClientId", Guid.NewGuid().ToString()), ("Body", "<p>Добрий день! Надсилаю договір.</p>")]);
        var message = await ApiClient.ReadAsync<ChatMessageDto>(sent);

        var again = await fundEmployee.PostAsync<ChatIdResponse>("/api/v1/chats/direct", new OpenDirectChatRequest(engineer.Context!.User!.Id));
        Assert.Equal(chat.Id, again.Id);
        var page = await fundEmployee.GetAsync<ChatMessagePage>($"/api/v1/chats/{chat.Id}/messages");
        Assert.Contains(page.Messages, m => m.Id == message.Id && m.Text.Contains("Надсилаю договір", StringComparison.Ordinal));

        using var foreign = await clerk.GetAsync($"/api/v1/chats/{chat.Id}/messages");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var intrusion = await clerk.SendFormAsync(HttpMethod.Post, $"/api/v1/chats/{chat.Id}/messages", [("ClientId", Guid.NewGuid().ToString()), ("Body", "<p>Привіт</p>")]);
        Assert.Equal(HttpStatusCode.NotFound, intrusion.StatusCode);
    }

    [Fact]
    public async Task A_resent_message_with_the_same_client_id_is_stored_once()
    {
        using var director = await app.Accounts.SignInAsync(Demo.Director);
        using var deputy = await app.Accounts.SignInAsync(Demo.Deputy);
        var chat = await director.PostAsync<ChatIdResponse>("/api/v1/chats/direct", new OpenDirectChatRequest(deputy.Context!.User!.Id));
        var clientId = Guid.NewGuid().ToString();

        using var first = await director.SendFormAsync(HttpMethod.Post, $"/api/v1/chats/{chat.Id}/messages", [("ClientId", clientId), ("Body", "<p>Нарада о 10:00</p>")]);
        using var second = await director.SendFormAsync(HttpMethod.Post, $"/api/v1/chats/{chat.Id}/messages", [("ClientId", clientId), ("Body", "<p>Нарада о 10:00</p>")]);

        var one = await ApiClient.ReadAsync<ChatMessageDto>(first);
        var two = await ApiClient.ReadAsync<ChatMessageDto>(second);
        Assert.Equal(one.Id, two.Id);
        var page = await deputy.GetAsync<ChatMessagePage>($"/api/v1/chats/{chat.Id}/messages");
        Assert.Single(page.Messages, m => m.ClientId == Guid.Parse(clientId));
    }
}
