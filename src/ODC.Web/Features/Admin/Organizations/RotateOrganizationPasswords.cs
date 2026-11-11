using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Admin.Organizations;

/// <summary>
/// Gives the PostgreSQL roles of an organization's database new random passwords (the application uses them at once).
/// </summary>
public static class RotateOrganizationPasswords
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Id">The organization.</param>
    [RequirePermission(Permissions.Administration.AllOrganizations)]
    public sealed record Command(Guid Id) : ICommand<Result>;

    /// <summary>
    /// Rotates the passwords.
    /// </summary>
    /// <param name="registry">The registry of organizations.</param>
    /// <param name="lifecycle">The life of organizations.</param>
    /// <param name="audit">The audit logger (the platform's trail).</param>
    internal sealed class Handler(OrganizationRegistry registry, OrganizationProvisioning lifecycle, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (await registry.FindAsync(request.Id, cancellationToken) is not { } organization)
            {
                return Error.NotFound(Texts.T("Організацію не знайдено."));
            }

            // Written with the first save of the change (or dropped when it fails before one).
            audit.Add(AuditActions.OrganizationPasswordsRotated, nameof(Organization), organization.Id.ToString(), $"Змінено паролі бази організації {OrganizationRules.Describe(organization)}");
            var result = await DomainErrors.GuardAsync(() => lifecycle.RotatePasswordsAsync(request.Id, cancellationToken));
            return result.IsSuccess ? Result.Success() : result.Error;
        }
    }

    /// <summary>
    /// <c>POST /api/v1/organizations/{id:guid}/rotate-passwords</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapOrganizations().MapPost("{id:guid}/rotate-passwords", HandleAsync);

        /// <summary>
        /// Rotates the passwords of an organization's database.
        /// </summary>
        /// <param name="id">The organization.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(Guid id, IRequestHandler<Command, Result> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Command(id), cancellationToken)).ToNoContent();
    }
}
