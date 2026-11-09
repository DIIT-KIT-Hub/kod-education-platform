using ODC.Web.Features.Notifications;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Features.Tickets;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application;

/// <summary>
/// Requests: who sees them, the flow of states, the notifications of the people concerned and the attachments.
/// </summary>
public sealed class TicketTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B; HelpDeskB and Admin handle requests.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task A_request_is_seen_only_by_its_author_and_the_handlers()
    {
        var id = await SubmitAsync();

        Assert.Equal(id, (await _world.As(_world.EmployeeA).SendAsync(new GetTicket.Query(id))).Ok().Id);
        Assert.True((await _world.As(_world.HelpDeskB).SendAsync(new GetTicket.Query(id))).Ok().CanManage);
        Assert.True((await _world.As(_world.Admin).SendAsync(new GetTicket.Query(id))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeB).SendAsync(new GetTicket.Query(id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).SendAsync(new ListTickets.AllQuery())).Failure());

        Assert.Single((await _world.As(_world.EmployeeA).SendAsync(new ListTickets.MineQuery())).Items);
        Assert.Empty((await _world.As(_world.EmployeeB).SendAsync(new ListTickets.MineQuery())).Items);
        Assert.Single((await _world.As(_world.HelpDeskB).SendAsync(new ListTickets.AllQuery())).Ok().Items);
    }

    [Fact]
    public async Task Every_handler_hears_about_a_new_request()
    {
        await SubmitAsync();

        var told = _world.NotificationEvents.Received.Where(static r => r.Notification.Kind == NotificationKind.TicketCreated).Select(static r => r.UserId).ToHashSet();
        Assert.Equal(new[] { _world.HelpDeskB.Id, _world.Admin.Id }.Order(), told.Order());
        Assert.DoesNotContain(_world.EmployeeA.Id, told);
    }

    [Fact]
    public async Task Every_handler_but_the_author_gets_an_email_in_their_language()
    {
        _world.HelpDeskB.SetLanguage("en");
        _world.Db.SaveChanges();

        var id = await SubmitAsync();

        var emails = _world.Emails.Sent.ToDictionary(static e => e.To);
        Assert.Equal(new[] { _world.Admin.Email, _world.HelpDeskB.Email }.Order(), emails.Keys.Order());
        var english = emails[_world.HelpDeskB.Email!];
        var ukrainian = emails[_world.Admin.Email!];
        Assert.StartsWith("New request No.", english.Subject, StringComparison.Ordinal);
        Assert.Contains("Open the request", english.Html, StringComparison.Ordinal);
        Assert.StartsWith("Нове звернення №", ukrainian.Subject, StringComparison.Ordinal);
        Assert.EndsWith("Не відкривається документ", ukrainian.Subject, StringComparison.Ordinal);

        // A link to the request and the beginning of its description, in both bodies.
        Assert.Contains($"https://odc.test/requests/{id}", ukrainian.Html, StringComparison.Ordinal);
        Assert.Contains($"https://odc.test/requests/{id}", ukrainian.Text, StringComparison.Ordinal);
        Assert.Contains("Після натискання нічого не відбувається.", ukrainian.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_first_answer_of_a_handler_takes_the_request_and_reaches_the_author()
    {
        var id = await SubmitAsync();

        Assert.True((await _world.As(_world.HelpDeskB).SendAsync(new CommentOnTicket.Command(id, "Перевіряємо", []))).IsSuccess);

        var ticket = (await _world.As(_world.EmployeeA).SendAsync(new GetTicket.Query(id))).Ok();
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(_world.HelpDeskB.Id, ticket.Assignee?.Id);
        Assert.Equal(2, ticket.Messages.Count(static m => m.Kind == TicketMessageKind.Comment));
        Assert.Contains(_world.NotificationEvents.Received, r => r.UserId == _world.EmployeeA.Id && r.Notification.Kind == NotificationKind.TicketReply);
    }

    [Fact]
    public async Task An_answer_of_the_author_opens_a_resolved_request_again_for_its_handler()
    {
        var id = await SubmitAsync();
        var handler = _world.As(_world.HelpDeskB);
        await handler.SendAsync(new CommentOnTicket.Command(id, "Виправлено", []));
        Assert.True((await handler.SendAsync(new ChangeTicketStatus.Command(id, TicketStatus.Resolved))).IsSuccess);
        _world.NotificationEvents.Received.Clear();

        Assert.True((await _world.As(_world.EmployeeA).SendAsync(new CommentOnTicket.Command(id, "Помилка повторюється", []))).IsSuccess);

        Assert.Equal(TicketStatus.InProgress, (await handler.SendAsync(new GetTicket.Query(id))).Ok().Status);
        var told = Assert.Single(_world.NotificationEvents.Received);
        Assert.Equal(_world.HelpDeskB.Id, told.UserId);
    }

    [Fact]
    public async Task The_author_may_only_close_and_nobody_answers_a_closed_request()
    {
        var id = await SubmitAsync();
        var author = _world.As(_world.EmployeeA);

        Assert.Equal(ErrorType.Validation, (await author.SendAsync(new ChangeTicketStatus.Command(id, TicketStatus.Resolved))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await author.SendAsync(new AssignTicket.Command(id, _world.HelpDeskB.Id))).Failure());
        Assert.True((await author.SendAsync(new ChangeTicketStatus.Command(id, TicketStatus.Closed))).IsSuccess);

        Assert.Equal(ErrorType.Validation, (await author.SendAsync(new CommentOnTicket.Command(id, "Ще одне", []))).Failure());
        Assert.Equal(ErrorType.Validation, (await _world.As(_world.HelpDeskB).SendAsync(new CommentOnTicket.Command(id, "Відповідь", []))).Failure());
        Assert.False((await author.SendAsync(new GetTicket.Query(id))).Ok().CanReply);
    }

    [Fact]
    public async Task A_request_is_assigned_only_to_a_handler_who_hears_about_it()
    {
        var id = await SubmitAsync();
        var admin = _world.As(_world.Admin);

        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new AssignTicket.Command(id, _world.EmployeeB.Id))).Failure());
        Assert.True((await admin.SendAsync(new AssignTicket.Command(id, _world.HelpDeskB.Id))).IsSuccess);

        Assert.Contains(_world.NotificationEvents.Received, r => r.UserId == _world.HelpDeskB.Id && r.Notification.Kind == NotificationKind.TicketAssigned);
        Assert.Equal(new[] { _world.Admin.Id, _world.HelpDeskB.Id }.Order(), (await admin.SendAsync(new ListTickets.HandlersQuery())).Ok().Select(static h => h.Id).Order());
    }

    [Fact]
    public async Task Opening_a_request_reads_its_notifications_and_a_refused_change_tells_nobody()
    {
        var id = await SubmitAsync();
        var handler = _world.As(_world.HelpDeskB);
        Assert.Equal(1, await handler.SendAsync(new CountUnreadNotifications.Query()));

        (await handler.SendAsync(new GetTicket.Query(id))).Ok();
        Assert.Equal(0, await _world.As(_world.HelpDeskB).SendAsync(new CountUnreadNotifications.Query()));

        _world.NotificationEvents.Received.Clear();
        Assert.Equal(ErrorType.Validation, (await handler.SendAsync(new ChangeTicketStatus.Command(id, TicketStatus.New))).Failure());
        Assert.Empty(_world.NotificationEvents.Received);
    }

    [Fact]
    public async Task Files_of_a_request_are_checked_and_served_only_to_those_who_see_it()
    {
        var refused = await _world.As(_world.EmployeeA).SendAsync(Request(File("run.exe")));
        Assert.Equal(ErrorType.Validation, refused.Failure());
        Assert.Empty(_world.Files.Stored);

        var id = (await _world.As(_world.EmployeeA).SendAsync(Request(File("log.txt")))).Ok();
        var file = Assert.Single((await _world.As(_world.EmployeeA).SendAsync(new GetTicket.Query(id))).Ok().Messages[0].Attachments);

        Assert.True((await _world.As(_world.HelpDeskB).SendAsync(new DownloadTicketAttachment.Query(id, file.Id, false))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeB).SendAsync(new DownloadTicketAttachment.Query(id, file.Id, false))).Failure());
    }

    [Fact]
    public async Task A_handler_changes_the_category_and_priority_of_a_request()
    {
        var id = await SubmitAsync();

        Assert.True((await _world.As(_world.HelpDeskB).SendAsync(new ClassifyTicket.Command(id, TicketCategory.Access, TicketPriority.Critical))).IsSuccess);
        Assert.True((await _world.As(_world.HelpDeskB).SendAsync(new ClassifyTicket.Command(id, TicketCategory.Access, TicketPriority.Critical))).IsSuccess);

        var ticket = _world.Master.Tickets.Single(t => t.Id == id);
        Assert.Equal(TicketCategory.Access, ticket.Category);
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
    }

    [Fact]
    public async Task Unknown_categories_or_priorities_are_refused()
    {
        var id = await SubmitAsync();
        var handler = _world.As(_world.HelpDeskB);

        Assert.Equal(ErrorType.Validation, (await handler.SendAsync(new ClassifyTicket.Command(id, (TicketCategory)42, TicketPriority.Low))).Failure());
        Assert.Equal(ErrorType.Validation, (await handler.SendAsync(new ClassifyTicket.Command(id, TicketCategory.Bug, (TicketPriority)(-1)))).Failure());
        Assert.Equal(TicketPriority.High, _world.Master.Tickets.Single(t => t.Id == id).Priority);
    }

    [Fact]
    public async Task Only_handlers_classify_requests_and_an_unknown_one_is_not_found()
    {
        var id = await SubmitAsync();

        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).SendAsync(new ClassifyTicket.Command(id, TicketCategory.Other, TicketPriority.Low))).Failure());
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.HelpDeskB).SendAsync(new ClassifyTicket.Command(Guid.NewGuid(), TicketCategory.Other, TicketPriority.Low))).Failure());
        Assert.Equal(TicketCategory.Bug, _world.Master.Tickets.Single(t => t.Id == id).Category);
    }

    /// <summary>
    /// Builds a request.
    /// </summary>
    /// <param name="files">The attached files.</param>
    /// <returns>The request.</returns>
    private static SubmitTicket.Command Request(params FileUpload[] files)
        => new(TicketCategory.Bug, TicketPriority.High, "Не відкривається документ", "Після натискання нічого не відбувається.", files);

    /// <summary>
    /// Builds an uploaded file of a few bytes.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <returns>The upload.</returns>
    private static FileUpload File(string name) => new(new MemoryStream([1, 2, 3]), name, "application/octet-stream", 3);

    /// <summary>
    /// Submits a request of EmployeeA.
    /// </summary>
    /// <returns>The request.</returns>
    private async Task<Guid> SubmitAsync() => (await _world.As(_world.EmployeeA).SendAsync(Request())).Ok();
}
