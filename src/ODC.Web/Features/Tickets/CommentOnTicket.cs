using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Writes a message in an open request. The first answer of a handler takes the request into work; an answer of the
/// author to a waiting or resolved request brings it back.
/// </summary>
public static class CommentOnTicket
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The ticket.</param>
    /// <param name="Body">The text.</param>
    /// <param name="Files">The attached files.</param>
    public sealed record Command(Guid Id, string? Body, IReadOnlyList<FileUpload> Files) : ICommand<Result>;

    /// <summary>
    /// Stores the files, adds the message (and the automatic changes) and tells the other side.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(MasterDbContext db, ICurrentUser currentUser, TicketDesk desk, TimeProvider timeProvider)
        : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return TicketDesk.NotFound();
            }

            var ticket = await desk.VisibleAsync(request.Id, me, cancellationToken);
            if (ticket is null)
            {
                return TicketDesk.NotFound();
            }

            if (!ticket.IsOpen)
            {
                return Error.Validation(Texts.T("Звернення закрито: відповісти на нього не можна."));
            }

            var stored = new List<string>();
            try
            {
                var attachments = await desk.StoreAsync(request.Files, stored, cancellationToken);
                if (!attachments.IsSuccess)
                {
                    await desk.DeleteAsync(stored, cancellationToken);
                    return attachments.Error;
                }

                var entries = ticket.Comment(me, await desk.NameOfAsync(me, cancellationToken), desk.IsHandler, request.Body, attachments.Value, timeProvider.GetUtcNow());
                db.TicketMessages.AddRange(entries);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (DomainException exception)
            {
                await desk.DeleteAsync(stored, cancellationToken);
                return Error.Validation(exception.Message);
            }
            catch
            {
                await desk.DeleteAsync(stored, CancellationToken.None);
                throw;
            }
        }
    }

    /// <summary>
    /// <c>POST /api/v1/tickets/{id}/messages</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => TicketDesk.MapTickets(api).MapPost("{id:guid}/messages", HandleAsync).WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxChatMessageSize + (1024 * 1024)));

        /// <summary>
        /// Writes a message in a request (multipart: the text and up to ten files).
        /// </summary>
        /// <param name="id">The ticket.</param>
        /// <param name="form">The message.</param>
        /// <param name="files">The attached files.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            [FromForm] TicketMessageForm form,
            IFormFileCollection? files,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await FormFiles.WithUploadsAsync(files, uploads => handler.HandleAsync(new Command(id, form.Body, uploads), cancellationToken))).ToNoContent();
    }
}
