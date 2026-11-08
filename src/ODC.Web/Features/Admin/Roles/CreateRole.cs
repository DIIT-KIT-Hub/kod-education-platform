using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Roles;

/// <summary>
/// Creates a custom role with its permissions.
/// </summary>
public static class CreateRole
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Role">The new role.</param>
    [RequirePermission(Permissions.Roles.Manage)]
    public sealed record Command(SaveRoleCommand Role) : ICommand<Result<Guid>>;

    /// <summary>
    /// Validates the code and the name and saves the role.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, IAuditLogger audit) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var data = request.Role;
            var code = data.Code?.Trim().ToLowerInvariant() ?? string.Empty;
            if (code.Length == 0 || !code.All(static c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            {
                return Error.Validation(Texts.T("Код ролі може містити лише малі латинські літери, цифри та «-»."));
            }

            if (await db.Roles.AnyAsync(r => r.Code == code, cancellationToken))
            {
                return Error.Conflict(Texts.T("Роль із кодом «{0}» вже існує.", code));
            }

            if (await RoleRules.NameTakenAsync(db, data.Name, null, cancellationToken))
            {
                return RoleRules.NameTaken(data.Name);
            }

            return await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var role = new Role(code, data.Name, data.Description, isSystem: false);
                role.Update(data.Name, data.Description, data.IsLeadership);
                role.SetPermissions(data.Permissions);
                db.Roles.Add(role);
                audit.Add(AuditActions.RoleChanged, nameof(Role), role.Id.ToString(), $"Створено роль «{role.Name}»: {string.Join(", ", data.Permissions)}");
                await db.SaveChangesAsync(cancellationToken);
                return role.Id;
            });
        }
    }

    /// <summary>
    /// <c>POST /api/v1/roles</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapRoles().MapPost(string.Empty, HandleAsync);

        /// <summary>
        /// Creates a custom role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>201 with the new identifier.</returns>
        private static async Task<Results<Created<CreatedId>, ProblemHttpResult>> HandleAsync(
            SaveRoleCommand role,
            IRequestHandler<Command, Result<Guid>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(role), cancellationToken)).ToCreated(static id => $"{ApiEndpoints.Base}/roles/{id}");
    }
}
