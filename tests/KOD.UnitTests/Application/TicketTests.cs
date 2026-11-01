using KOD.Application.Common.Files;
using KOD.Application.Common.Results;
using KOD.Application.Features.Tickets;
using KOD.Domain.Notifications;
using KOD.Domain.Tickets;

namespace KOD.UnitTests.Application;

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

        Assert.Equal(id, (await _world.As(_world.EmployeeA).Tickets.GetAsync(id, CancellationToken.None)).Ok().Id);
        Assert.True((await _world.As(_world.HelpDeskB).Tickets.GetAsync(id, CancellationToken.None)).Ok().CanManage);
        Assert.True((await _world.As(_world.Admin).Tickets.GetAsync(id, CancellationToken.None)).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeB).Tickets.GetAsync(id, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).Tickets.ListAllAsync(new TicketQuery(), CancellationToken.None)).Failure());

        Assert.Single((await _world.As(_world.EmployeeA).Tickets.ListMineAsync(new TicketQuery(), CancellationToken.None)).Items);
        Assert.Empty((await _world.As(_world.EmployeeB).Tickets.ListMineAsync(new TicketQuery(), CancellationToken.None)).Items);
        Assert.Single((await _world.As(_world.HelpDeskB).Tickets.ListAllAsync(new TicketQuery(), CancellationToken.None)).Ok().Items);
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
    public async Task The_first_answer_of_a_handler_takes_the_request_and_reaches_the_author()
    {
        var id = await SubmitAsync();

        Assert.True((await _world.As(_world.HelpDeskB).Tickets.CommentAsync(id, "Перевіряємо", [], CancellationToken.None)).IsSuccess);

        var ticket = (await _world.As(_world.EmployeeA).Tickets.GetAsync(id, CancellationToken.None)).Ok();
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(_world.HelpDeskB.Id, ticket.Assignee?.Id);
        Assert.Equal(2, ticket.Messages.Count(static m => m.Kind == TicketMessageKind.Comment));
        Assert.Contains(_world.NotificationEvents.Received, r => r.UserId == _world.EmployeeA.Id && r.Notification.Kind == NotificationKind.TicketReply);
    }

    [Fact]
    public async Task An_answer_of_the_author_opens_a_resolved_request_again_for_its_handler()
    {
        var id = await SubmitAsync();
        var handler = _world.As(_world.HelpDeskB).Tickets;
        await handler.CommentAsync(id, "Виправлено", [], CancellationToken.None);
        Assert.True((await handler.ChangeStatusAsync(id, TicketStatus.Resolved, CancellationToken.None)).IsSuccess);
        _world.NotificationEvents.Received.Clear();

        Assert.True((await _world.As(_world.EmployeeA).Tickets.CommentAsync(id, "Помилка повторюється", [], CancellationToken.None)).IsSuccess);

        Assert.Equal(TicketStatus.InProgress, (await handler.GetAsync(id, CancellationToken.None)).Ok().Status);
        var told = Assert.Single(_world.NotificationEvents.Received);
        Assert.Equal(_world.HelpDeskB.Id, told.UserId);
    }

    [Fact]
    public async Task The_author_may_only_close_and_nobody_answers_a_closed_request()
    {
        var id = await SubmitAsync();
        var author = _world.As(_world.EmployeeA).Tickets;

        Assert.Equal(ErrorType.Validation, (await author.ChangeStatusAsync(id, TicketStatus.Resolved, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Forbidden, (await author.AssignAsync(id, _world.HelpDeskB.Id, CancellationToken.None)).Failure());
        Assert.True((await author.ChangeStatusAsync(id, TicketStatus.Closed, CancellationToken.None)).IsSuccess);

        Assert.Equal(ErrorType.Validation, (await author.CommentAsync(id, "Ще одне", [], CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Validation, (await _world.As(_world.HelpDeskB).Tickets.CommentAsync(id, "Відповідь", [], CancellationToken.None)).Failure());
        Assert.False((await author.GetAsync(id, CancellationToken.None)).Ok().CanReply);
    }

    [Fact]
    public async Task A_request_is_assigned_only_to_a_handler_who_hears_about_it()
    {
        var id = await SubmitAsync();
        var admin = _world.As(_world.Admin).Tickets;

        Assert.Equal(ErrorType.Validation, (await admin.AssignAsync(id, _world.EmployeeB.Id, CancellationToken.None)).Failure());
        Assert.True((await admin.AssignAsync(id, _world.HelpDeskB.Id, CancellationToken.None)).IsSuccess);

        Assert.Contains(_world.NotificationEvents.Received, r => r.UserId == _world.HelpDeskB.Id && r.Notification.Kind == NotificationKind.TicketAssigned);
        Assert.Equal(new[] { _world.Admin.Id, _world.HelpDeskB.Id }.Order(), (await admin.ListHandlersAsync(CancellationToken.None)).Ok().Select(static h => h.Id).Order());
    }

    [Fact]
    public async Task Files_of_a_request_are_checked_and_served_only_to_those_who_see_it()
    {
        var refused = await _world.As(_world.EmployeeA).Tickets.SubmitAsync(Request(File("run.exe")), CancellationToken.None);
        Assert.Equal(ErrorType.Validation, refused.Failure());
        Assert.Empty(_world.Files.Stored);

        var id = (await _world.As(_world.EmployeeA).Tickets.SubmitAsync(Request(File("log.txt")), CancellationToken.None)).Ok();
        var file = Assert.Single((await _world.As(_world.EmployeeA).Tickets.GetAsync(id, CancellationToken.None)).Ok().Messages[0].Attachments);

        Assert.True((await _world.As(_world.HelpDeskB).Tickets.OpenAttachmentAsync(id, file.Id, thumbnail: false, CancellationToken.None)).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeB).Tickets.OpenAttachmentAsync(id, file.Id, thumbnail: false, CancellationToken.None)).Failure());
    }

    /// <summary>
    /// Builds a request.
    /// </summary>
    /// <param name="files">The attached files.</param>
    /// <returns>The request.</returns>
    private static SubmitTicket Request(params FileUpload[] files)
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
    private async Task<Guid> SubmitAsync() => (await _world.As(_world.EmployeeA).Tickets.SubmitAsync(Request(), CancellationToken.None)).Ok();
}
