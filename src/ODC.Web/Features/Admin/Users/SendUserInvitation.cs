using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Sends the invitation to activate an account again.
/// </summary>
public static class SendUserInvitation
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    [RequirePermission(Permissions.Users.Security)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Sends the invitation to an active account that awaits activation.
    /// </summary>
    /// <param name="directory">The user administration rules.</param>
    internal sealed class Handler(UserDirectory directory) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await directory.LoadForSecurityAsync(request.Id, cancellationToken);
            if (!user.IsSuccess)
            {
                return user.Error;
            }

            if (!user.Value.IsActive || !user.Value.AwaitsActivation)
            {
                return Error.Validation(Texts.T("Запрошення надсилається лише активним обліковим записам, які ще не активовано."));
            }

            if (!directory.CanSendEmail)
            {
                return new Error("email_unavailable", Texts.T("Надсилання листів не налаштовано."), ErrorType.Unavailable);
            }

            directory.Invite(user.Value);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST /api/v1/users/{id}/invitations</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => UserDirectory.MapUsers(api).MapPost("{id:guid}/invitations", HandleAsync).RequirePermission(Permissions.Users.Security);

        /// <summary>
        /// Sends the invitation to activate the account again.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
