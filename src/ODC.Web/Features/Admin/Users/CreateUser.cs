using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Admin.Users;

/// <summary>
/// Creates an account without a password (nobody hands out passwords); the owner gets the invitation to activate it.
/// </summary>
public static class CreateUser
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="User">The new account.</param>
    [RequirePermission(Permissions.Users.Manage)]
    public sealed record Command(SaveUserCommand User) : ICommand<Result<Guid>>;

    /// <summary>
    /// Validates the address and positions, creates the account and sends the invitation.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(
        MasterDbContext db,
        ICurrentUser currentUser,
        IIdentityService identity,
        IAccessScope accessScope,
        UserDirectory directory,
        IAuditLogger audit) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var command = request.User;
            var email = EmailAddress.Normalize(command.Email);
            if (email is null)
            {
                return Error.Validation(Texts.T("Вкажіть коректну адресу електронної пошти."));
            }

            var normalizedEmail = email.ToUpperInvariant();
            if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                return Error.Conflict(Texts.T("Користувач із такою електронною поштою вже існує."));
            }

            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
            if (command.Positions.Any(p => !scope.Contains(p.UnitId)))
            {
                return Error.Forbidden(Texts.T(UserDirectory.OutsideScopeMessage));
            }

            if (!scope.IsCabinetWide && command.Positions.Count == 0)
            {
                return Error.Validation(Texts.T(UserDirectory.NoPositionsMessage));
            }

            if (await directory.ValidatePositionsAsync(command.Positions, cancellationToken) is { } positionError)
            {
                return positionError;
            }

            return await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var user = new User(email, command.LastName, command.FirstName, command.MiddleName);
                user.UpdatePersonalData(command.LastName, command.FirstName, command.MiddleName, command.PhoneNumber);
                var positions = command.Positions.Select(static p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary)).ToList();
                var adjustsPermissions = currentUser.Has(Permissions.Roles.Manage);

                // The rules of positions and permissions are checked before the account exists.
                Apply(new Member(MemberProfile.Of(user)), positions, adjustsPermissions ? command : null);
                var created = await identity.CreateAsync(user);
                if (!created.IsSuccess)
                {
                    return created.Error;
                }

                Apply(await directory.MemberOfAsync(user, cancellationToken), positions, adjustsPermissions ? command : null);
                await directory.SaveMembersAsync(cancellationToken);

                await audit.WriteAsync(AuditActions.UserCreated, currentUser.Id, currentUser.DisplayName, $"Створено {user.Email} ({user.FullName})", cancellationToken);
                directory.Invite(user);
                return user.Id;
            });
        }

        /// <summary>
        /// Gives a member the positions and, when the administrator may adjust them, the individual permissions.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="positions">The positions.</param>
        /// <param name="permissions">The command with the individual permissions, or null to keep none.</param>
        private static void Apply(Member member, List<(Guid UnitId, Guid RoleId, string? Title, bool IsPrimary)> positions, SaveUserCommand? permissions)
        {
            member.SetPositions(positions);
            if (permissions is not null)
            {
                member.SetPermissionOverrides(permissions.GrantedPermissions, permissions.DeniedPermissions);
            }
        }
    }

    /// <summary>
    /// <c>POST /api/v1/users</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => UserDirectory.MapUsers(api).MapPost(string.Empty, HandleAsync).RequirePermission(Permissions.Users.Manage);

        /// <summary>
        /// Creates an account without a password; the owner gets the invitation to activate it.
        /// </summary>
        /// <param name="user">The account.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<UserIdResponse>, ProblemHttpResult>> HandleAsync(
            SaveUserCommand user,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(user), cancellationToken);
            return result.IsSuccess
                ? TypedResults.Created($"{ApiEndpoints.Base}/users/{result.Value}", new UserIdResponse(result.Value))
                : result.Error.ToProblem();
        }
    }
}
