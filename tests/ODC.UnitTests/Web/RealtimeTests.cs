using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ODC.Web.Features.Chats;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.News.Discussion;
using ODC.Web.Features.Notifications;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Messaging;
using ODC.Web.Realtime;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Models;

namespace ODC.UnitTests.Web;

/// <summary>
/// The SignalR hub (own-origin connections only, discussion groups only for readers of the article, typing signals only
/// to the other members of a chat and not too often) and the senders that push changes to the open pages without ever
/// failing or delaying the request that made them.
/// </summary>
public sealed class RealtimeTests
{
    /// <summary>
    /// The signed-in user of the connection.
    /// </summary>
    private static readonly Guid Me = Guid.NewGuid();

    /// <summary>
    /// What the clients received (<c>target: method(arguments)</c>).
    /// </summary>
    private readonly List<string> _sent = [];

    /// <summary>
    /// The group changes of the hub.
    /// </summary>
    private readonly List<string> _groups = [];

    [Theory]
    [InlineData("https://cabinet.example.com", false)]
    [InlineData("HTTPS://CABINET.EXAMPLE.COM", false)]
    [InlineData("", false)]
    [InlineData("https://evil.example.com", true)]
    [InlineData("http://cabinet.example.com", true)]
    [InlineData("https://cabinet.example.com:8443", true)]
    public async Task Only_connections_from_the_cabinet_itself_stay_open(string origin, bool aborted)
    {
        var context = Caller(Permissions.Chats.Use);
        context.Http!.Request.Headers.Origin = origin;
        var hub = Hub(context);

        await hub.OnConnectedAsync();

        Assert.Equal(aborted, context.Aborted);
    }

    [Fact]
    public async Task A_connection_without_an_HTTP_request_is_closed()
    {
        var context = Caller(Permissions.Chats.Use);
        context.Http = null;

        await Hub(context).OnConnectedAsync();

        Assert.True(context.Aborted);
    }

    [Fact]
    public async Task A_reader_of_the_article_joins_its_discussion_group_and_leaves_it()
    {
        var article = Guid.NewGuid();
        var hub = Hub(Caller(Permissions.News.View), canRead: true);

        Assert.True(await hub.JoinNewsDiscussion(article));
        await hub.LeaveNewsDiscussion(article);

        Assert.Equal([$"+ conn-1 news:{article:N}", $"- conn-1 news:{article:N}"], _groups);
    }

    [Fact]
    public async Task A_user_who_cannot_read_the_article_is_not_added()
    {
        Assert.False(await Hub(Caller(Permissions.News.View), canRead: false).JoinNewsDiscussion(Guid.NewGuid()));
        Assert.Empty(_groups);
    }

    [Fact]
    public async Task A_user_without_news_access_is_not_added_even_if_the_article_is_readable()
    {
        Assert.False(await Hub(Caller(Permissions.Chats.Use), canRead: true).JoinNewsDiscussion(Guid.NewGuid()));
        Assert.Empty(_groups);
    }

    [Fact]
    public async Task Typing_is_signalled_to_the_other_members_only_with_the_typist()
    {
        var chat = Guid.NewGuid();
        var other = Guid.NewGuid();

        await Hub(Caller(Permissions.Chats.Use), members: [Me, other]).Typing(chat);

        Assert.Equal([$"users {other}: ChatTyping({chat}, {Me}, Тест Користувач)"], _sent);
    }

    [Fact]
    public async Task Typing_is_signalled_at_most_every_two_seconds_per_connection()
    {
        var hub = Hub(Caller(Permissions.Chats.Use), members: [Me, Guid.NewGuid()]);

        await hub.Typing(Guid.NewGuid());
        await hub.Typing(Guid.NewGuid());

        Assert.Single(_sent);
    }

    [Fact]
    public async Task Typing_in_a_chat_the_user_is_not_a_member_of_signals_nothing()
    {
        await Hub(Caller(Permissions.Chats.Use), members: null).Typing(Guid.NewGuid());

        Assert.Empty(_sent);
    }

    [Fact]
    public async Task Typing_without_the_chat_permission_or_a_user_id_signals_nothing()
    {
        await Hub(Caller(Permissions.News.View), members: [Me, Guid.NewGuid()]).Typing(Guid.NewGuid());
        var anonymous = Caller(Permissions.Chats.Use);
        anonymous.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppClaimTypes.Permission, Permissions.Chats.Use)], "test"));
        await Hub(anonymous, members: [Guid.NewGuid()]).Typing(Guid.NewGuid());

        Assert.Empty(_sent);
    }

    [Fact]
    public void Chat_changes_go_to_the_given_members_with_the_full_message()
    {
        var events = new LiveChatEvents(Context(), NullLogger<LiveChatEvents>.Instance);
        var chat = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var message = Message(chat);

        events.MessageAdded([a, b], message);
        events.MessageChanged([a], message);
        events.ChatChanged([a], chat);
        events.ChatRemoved([a], chat);
        events.HistoryCleared([a], chat);
        events.Read([b], chat, a, 42);

        Assert.Equal(
            [
                $"users {a},{b}: ChatMessageAdded({message.Id})",
                $"users {a}: ChatMessageChanged({message.Id})",
                $"users {a}: ChatChanged({chat})",
                $"users {a}: ChatRemoved({chat})",
                $"users {a}: ChatHistoryCleared({chat})",
                $"users {b}: ChatRead({chat}, {a}, 42)",
            ],
            _sent);
    }

    [Fact]
    public void A_chat_change_without_recipients_sends_nothing()
    {
        new LiveChatEvents(Context(), NullLogger<LiveChatEvents>.Instance).ChatChanged([], Guid.NewGuid());

        Assert.Empty(_sent);
    }

    [Fact]
    public void Notifications_go_to_their_user_with_the_unread_count()
    {
        var events = new LiveNotificationEvents(Context(), NullLogger<LiveNotificationEvents>.Instance);
        var user = Guid.NewGuid();
        var notification = new NotificationDto(Guid.NewGuid(), NotificationKind.TicketReply, null, Guid.NewGuid(), "Запит № 5", null, null, "/requests/x", 1, DateTimeOffset.UnixEpoch, null);

        events.Received(user, notification, 3);
        events.Changed(user, 0);

        Assert.Equal([$"user {user}: NotificationReceived({notification.Id}, 3)", $"user {user}: NotificationsChanged(0)"], _sent);
    }

    [Fact]
    public void A_discussion_change_goes_to_the_article_group()
    {
        var article = Guid.NewGuid();

        new LiveNewsDiscussionEvents(Context(), NullLogger<LiveNewsDiscussionEvents>.Instance).Publish(article);

        Assert.Equal([$"group news:{article:N}: NewsDiscussionChanged({article})"], _sent);
    }

    [Fact]
    public void A_failing_connection_never_fails_the_request_that_made_the_change()
    {
        var hub = Context(fail: true);

        new LiveChatEvents(hub, NullLogger<LiveChatEvents>.Instance).MessageAdded([Guid.NewGuid()], Message(Guid.NewGuid()));
        new LiveNotificationEvents(hub, NullLogger<LiveNotificationEvents>.Instance).Changed(Guid.NewGuid(), 1);
        new LiveNewsDiscussionEvents(hub, NullLogger<LiveNewsDiscussionEvents>.Instance).Publish(Guid.NewGuid());

        Assert.Empty(_sent);
    }

    /// <summary>
    /// A chat message to send.
    /// </summary>
    /// <param name="chat">The chat.</param>
    /// <returns>The message.</returns>
    private static ChatMessageDto Message(Guid chat)
        => new(Guid.NewGuid(), chat, 1, new UserRef(Me, "Тест Користувач", null), ChatMessageKind.Text, null, "<p>Привіт</p>", "Привіт", DateTimeOffset.UnixEpoch, null, false, null, [], []);

    /// <summary>
    /// The connection of the signed-in user with the given permission, from the cabinet's own origin.
    /// </summary>
    /// <param name="permission">The permission of the user.</param>
    /// <returns>The connection.</returns>
    private static FakeCallerContext Caller(string permission)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("cabinet.example.com");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.Subject, Me.ToString()),
                new Claim(AppClaimTypes.FullName, "Тест Користувач"),
                new Claim(AppClaimTypes.Permission, permission),
            ],
            "test"));
        return new FakeCallerContext(user, http);
    }

    /// <summary>
    /// The hub for a connection.
    /// </summary>
    /// <param name="context">The connection.</param>
    /// <param name="canRead">Whether the user may read the discussion asked about.</param>
    /// <param name="members">The members of the chat asked about (null: the user is not a member).</param>
    /// <returns>The hub.</returns>
    private LiveHub Hub(FakeCallerContext context, bool canRead = false, IReadOnlyList<Guid>? members = null)
        => new(new ServiceCollection()
            .AddSingleton<IRequestHandler<CanReadNewsDiscussion.Query, bool>>(new Answer<CanReadNewsDiscussion.Query, bool>(canRead))
            .AddSingleton<IRequestHandler<GetChatMemberIds.Query, IReadOnlyList<Guid>?>>(new Answer<GetChatMemberIds.Query, IReadOnlyList<Guid>?>(members))
            .BuildServiceProvider())
        {
            Context = context,
            Clients = new FakeClients(_sent, fail: false),
            Groups = new FakeGroups(_groups),
        };

    /// <summary>
    /// The hub context the senders use.
    /// </summary>
    /// <param name="fail">Whether every send fails (a dropped connection).</param>
    /// <returns>The hub context.</returns>
    private FakeHubContext Context(bool fail = false) => new(new FakeClients(_sent, fail), new FakeGroups(_groups));

    /// <summary>
    /// A handler with a fixed answer.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="answer">The answer.</param>
    private sealed class Answer<TRequest, TResponse>(TResponse answer) : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        /// <inheritdoc />
        public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken) => Task.FromResult(answer);
    }

    /// <summary>
    /// A connection of the hub.
    /// </summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="http">The HTTP request that opened the connection.</param>
    private sealed class FakeCallerContext(ClaimsPrincipal user, HttpContext? http) : HubCallerContext
    {
        /// <summary>
        /// Gets or sets the HTTP request that opened the connection.
        /// </summary>
        public HttpContext? Http { get; set; } = http;

        /// <summary>
        /// Gets a value indicating whether the connection was closed.
        /// </summary>
        public bool Aborted { get; private set; }

        /// <inheritdoc />
        public override string ConnectionId => "conn-1";

        /// <inheritdoc />
        public override string? UserIdentifier => User?.FindFirst(AppClaimTypes.Subject)?.Value;

        /// <inheritdoc />
        public override ClaimsPrincipal? User => CurrentUser;

        /// <summary>
        /// Gets or sets the signed-in user.
        /// </summary>
        public ClaimsPrincipal CurrentUser { get; set; } = user;

        /// <inheritdoc />
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        /// <inheritdoc />
        public override IFeatureCollection Features
        {
            get
            {
                var features = new FeatureCollection();
                features.Set<IHttpContextFeature>(new HttpContextFeature { HttpContext = Http });
                return features;
            }
        }

        /// <inheritdoc />
        public override CancellationToken ConnectionAborted => TestContext.Current.CancellationToken;

        /// <inheritdoc />
        public override void Abort() => Aborted = true;
    }

    /// <summary>
    /// The HTTP request of a connection.
    /// </summary>
    private sealed class HttpContextFeature : IHttpContextFeature
    {
        /// <inheritdoc />
        public HttpContext? HttpContext { get; set; }
    }

    /// <summary>
    /// The group manager, recording the changes.
    /// </summary>
    /// <param name="log">The record.</param>
    private sealed class FakeGroups(List<string> log) : IGroupManager
    {
        /// <inheritdoc />
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            log.Add($"+ {connectionId} {groupName}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            log.Add($"- {connectionId} {groupName}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The hub context of the senders.
    /// </summary>
    /// <param name="clients">The clients.</param>
    /// <param name="groups">The groups.</param>
    private sealed class FakeHubContext(FakeClients clients, IGroupManager groups) : IHubContext<LiveHub, ILiveClient>
    {
        /// <inheritdoc />
        public IHubClients<ILiveClient> Clients => clients;

        /// <inheritdoc />
        public IGroupManager Groups => groups;
    }

    /// <summary>
    /// The clients of the hub: every target is a recording client.
    /// </summary>
    /// <param name="log">The record.</param>
    /// <param name="fail">Whether every send fails.</param>
    private sealed class FakeClients(List<string> log, bool fail) : IHubCallerClients<ILiveClient>
    {
        /// <inheritdoc />
        public ILiveClient All => To("all");

        /// <inheritdoc />
        public ILiveClient Caller => To("caller");

        /// <inheritdoc />
        public ILiveClient Others => To("others");

        /// <inheritdoc />
        public ILiveClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => To("all-except");

        /// <inheritdoc />
        public ILiveClient Client(string connectionId) => To($"client {connectionId}");

        /// <inheritdoc />
        public ILiveClient Clients(IReadOnlyList<string> connectionIds) => To($"clients {string.Join(',', connectionIds)}");

        /// <inheritdoc />
        public ILiveClient Group(string groupName) => To($"group {groupName}");

        /// <inheritdoc />
        public ILiveClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => To($"group-except {groupName}");

        /// <inheritdoc />
        public ILiveClient Groups(IReadOnlyList<string> groupNames) => To($"groups {string.Join(',', groupNames)}");

        /// <inheritdoc />
        public ILiveClient OthersInGroup(string groupName) => To($"others-in {groupName}");

        /// <inheritdoc />
        public ILiveClient User(string userId) => To($"user {userId}");

        /// <inheritdoc />
        public ILiveClient Users(IReadOnlyList<string> userIds) => To($"users {string.Join(',', userIds)}");

        /// <summary>
        /// A recording client for a target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>The client.</returns>
        private RecordingClient To(string target) => new(log, target, fail);
    }

    /// <summary>
    /// A client that records the calls (or fails them).
    /// </summary>
    /// <param name="log">The record.</param>
    /// <param name="target">The target of the calls.</param>
    /// <param name="fail">Whether every call fails.</param>
    private sealed class RecordingClient(List<string> log, string target, bool fail) : ILiveClient
    {
        /// <inheritdoc />
        public Task NewsDiscussionChanged(Guid articleId) => Record($"NewsDiscussionChanged({articleId})");

        /// <inheritdoc />
        public Task ChatMessageAdded(ChatMessageDto message) => Record($"ChatMessageAdded({message.Id})");

        /// <inheritdoc />
        public Task ChatMessageChanged(ChatMessageDto message) => Record($"ChatMessageChanged({message.Id})");

        /// <inheritdoc />
        public Task ChatChanged(Guid chatId) => Record($"ChatChanged({chatId})");

        /// <inheritdoc />
        public Task ChatRemoved(Guid chatId) => Record($"ChatRemoved({chatId})");

        /// <inheritdoc />
        public Task ChatHistoryCleared(Guid chatId) => Record($"ChatHistoryCleared({chatId})");

        /// <inheritdoc />
        public Task ChatRead(Guid chatId, Guid userId, long sequence) => Record($"ChatRead({chatId}, {userId}, {sequence})");

        /// <inheritdoc />
        public Task ChatTyping(Guid chatId, UserRef user) => Record($"ChatTyping({chatId}, {user.Id}, {user.FullName})");

        /// <inheritdoc />
        public Task NotificationReceived(NotificationDto notification, int unreadCount) => Record($"NotificationReceived({notification.Id}, {unreadCount})");

        /// <inheritdoc />
        public Task NotificationsChanged(int unreadCount) => Record($"NotificationsChanged({unreadCount})");

        /// <summary>
        /// Records a call or fails it.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The completed or failed task.</returns>
        private Task Record(string call)
        {
            if (fail)
            {
                return Task.FromException(new IOException("The connection is gone."));
            }

            log.Add($"{target}: {call}");
            return Task.CompletedTask;
        }
    }
}
