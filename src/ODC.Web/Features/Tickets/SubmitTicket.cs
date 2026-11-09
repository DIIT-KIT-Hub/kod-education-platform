using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ODC.Web.Endpoints;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// Submits a request (bug, suggestion, question) with files; the handlers get a notification and an e-mail.
/// </summary>
public static class SubmitTicket
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Category">The kind of request.</param>
    /// <param name="Priority">The urgency.</param>
    /// <param name="Subject">The subject.</param>
    /// <param name="Description">The description (plain text).</param>
    /// <param name="Files">The attached files.</param>
    public sealed record Command(TicketCategory Category, TicketPriority Priority, string Subject, string Description, IReadOnlyList<FileUpload> Files)
        : ICommand<Result<Guid>>;

    /// <summary>
    /// Stores the files, saves the request and tells the handlers. Stored files are deleted when anything fails.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="desk">The shared request rules.</param>
    /// <param name="timeProvider">The clock.</param>
    internal sealed class Handler(
        MasterDbContext db,
        ICurrentUser currentUser,
        TicketDesk desk,
        TimeProvider timeProvider) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (currentUser.Id is not { } me)
            {
                return Error.Unauthorized();
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

                var (ticket, description) = Ticket.Submit(me, request.Category, request.Priority, request.Subject, request.Description, attachments.Value, timeProvider.GetUtcNow());
                db.Tickets.Add(ticket);
                db.TicketMessages.Add(description);
                await db.SaveChangesAsync(cancellationToken);
                return ticket.Id;
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
    /// <c>POST /api/v1/tickets</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => TicketDesk.MapTickets(api).MapPost(string.Empty, HandleAsync).WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxChatMessageSize + (1024 * 1024)));

        /// <summary>
        /// Submits a request (multipart: the fields and up to ten files).
        /// </summary>
        /// <param name="form">The request.</param>
        /// <param name="files">The attached files.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the request.</returns>
        private static async Task<Results<Created<TicketIdResponse>, ProblemHttpResult>> HandleAsync(
            [FromForm] SubmitTicketForm form,
            IFormFileCollection? files,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
        {
            var result = await FormFiles.WithUploadsAsync(files, uploads => handler.HandleAsync(
                new Command(form.Category, form.Priority, form.Subject, form.Description, uploads),
                cancellationToken));
            return result.IsSuccess
                ? TypedResults.Created($"{ApiEndpoints.Base}/tickets/{result.Value}", new TicketIdResponse(result.Value))
                : result.Error.ToProblem();
        }
    }
}
