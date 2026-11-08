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
/// Replaces the account data and positions. An administrator of some of the account's organizations changes only the
/// positions there; the rest of the account belongs to whoever manages all of them.
/// </summary>
public static class UpdateUser
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The account.</param>
    /// <param name="User">The new data.</param>
    [RequirePermission(Permissions.Users.Manage)]
    public sealed record Command(Guid Id, SaveUserCommand User) : ICommand<Result>;

    /// <summary>
    /// Validates the change against the scope and saves it.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="accessScope">The administration scopes.</param>
    /// <param name="directory">The user administration rules.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        IIdentityService identity,
        IAccessScope accessScope,
        UserDirectory directory,
        IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var (id, command) = (request.Id, request.User);
            var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
            var user = await directory.LoadForManagementAsync(id, scope, requireWholeAccount: false, cancellationToken);
            if (!user.IsSuccess)
            {
                return user.Error;
            }

            var email = EmailAddress.Normalize(command.Email);
            if (email is null)
            {
                return Error.Validation(Texts.T("Вкажіть коректну адресу електронної пошти."));
            }

            var normalizedEmail = email.ToUpperInvariant();
            if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != id, cancellationToken))
            {
                return Error.Conflict(Texts.T("Користувач із такою електронною поштою вже існує."));
            }

            // Positions outside the managed organizations stay as they are; the command may only repeat them.
            var entity = user.Value;
            var foreign = entity.Positions.Where(p => !scope.Contains(p.OrganizationUnitId)).ToList();
            if (command.Positions.Any(p => !scope.Contains(p.UnitId) && !foreign.Exists(f => f.OrganizationUnitId == p.UnitId && f.RoleId == p.RoleId)))
            {
                return Error.Forbidden(Texts.T(UserDirectory.OutsideScopeMessage));
            }

            var managed = command.Positions.Where(p => scope.Contains(p.UnitId)).ToList();
            if (!scope.IsCabinetWide && foreign.Count + managed.Count == 0)
            {
                return Error.Validation(Texts.T(UserDirectory.NoPositionsMessage));
            }

            var wholeAccount = foreign.Count == 0;
            if (!wholeAccount && !directory.IsAccountUnchanged(entity, email, command))
            {
                return Error.Forbidden(Texts.T(UserDirectory.SharedAccountMessage));
            }

            if (await directory.ValidatePositionsAsync(managed, cancellationToken) is { } positionError)
            {
                return positionError;
            }

            var foreignPrimary = foreign.Exists(static p => p.IsPrimary);
            var positions = foreign
                .Select(static p => (p.OrganizationUnitId, p.RoleId, p.Title, p.IsPrimary))
                .Concat(managed.Select(p => (p.UnitId, p.RoleId, p.Title, p.IsPrimary && !foreignPrimary)))
                .ToList();

            return await DomainErrors.GuardAsync(async () =>
            {
                if (wholeAccount)
                {
                    entity.Email = email;
                    entity.UserName = email;
                    entity.UpdatePersonalData(command.LastName, command.FirstName, command.MiddleName, command.PhoneNumber);
                    if (currentUser.Has(Permissions.Roles.Manage))
                    {
                        entity.SetPermissionOverrides(command.GrantedPermissions, command.DeniedPermissions);
                    }
                }

                entity.SetPositions(positions);
                audit.Add(AuditActions.UserUpdated, nameof(User), entity.Id.ToString(), $"Змінено {entity.Email}");
                return await identity.UpdateAsync(entity);
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/users/{id}</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => UserDirectory.MapUsers(api).MapPut("{id:guid}", HandleAsync).RequirePermission(Permissions.Users.Manage);

        /// <summary>
        /// Replaces the account data and positions.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="user">The account.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id,
            SaveUserCommand user,
            IRequestHandler<Command, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id, user), cancellationToken)).ToNoContent();
    }
}
