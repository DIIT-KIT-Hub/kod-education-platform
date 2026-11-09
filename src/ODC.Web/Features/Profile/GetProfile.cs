using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Profile;

/// <summary>
/// The signed-in user's profile: personal data, positions, permissions and the state of the second factor.
/// </summary>
public static class GetProfile
{
    /// <summary>
    /// The request.
    /// </summary>
    public sealed record Query : IQuery<Result<ProfileDto>>;

    /// <summary>
    /// Reads the profile.
    /// </summary>
    /// <param name="account">The current user's account.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="permissions">Resolves effective permissions.</param>
    internal sealed class Handler(ProfileAccount account, IIdentityService identity, IPermissionResolver permissions) : IRequestHandler<Query, Result<ProfileDto>>
    {
        /// <inheritdoc />
        public async Task<Result<ProfileDto>> HandleAsync(Query request, CancellationToken cancellationToken)
        {
            var user = await account.LoadAsync(cancellationToken);
            if (user is null)
            {
                return Error.Unauthorized();
            }

            var positions = await account.PositionsAsync(cancellationToken);
            var effective = await permissions.GetEffectivePermissionsAsync(user.Id, cancellationToken);
            var definitions = Permissions.All.Where(p => effective.Contains(p.Code)).ToList();
            return new ProfileDto(
                user.Id,
                user.FullName,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.Email!,
                user.PhoneNumber,
                positions
                    .Select(static p => new PositionDto(p.UnitId, p.UnitName, p.RoleId, StoredText.Display(p.RoleName) ?? p.RoleName, p.Title, p.IsPrimary))
                    .ToList(),
                [.. definitions.Select(static d => d.Module).Distinct().Order()],
                definitions,
                user.TwoFactorEnabled,
                await identity.CountRecoveryCodesAsync(user),
                user.CreatedAt,
                user.LastSignInAt,
                user.Language,
                user.PhotoId,
                await account.CanEditPersonalDataAsync(positions, cancellationToken));
        }
    }

    /// <summary>
    /// <c>GET /api/v1/profile</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ProfileAccount.MapProfile(api).MapGet(string.Empty, HandleAsync);

        /// <summary>
        /// Gets the profile.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The profile.</returns>
        private static async Task<Results<Ok<ProfileDto>, ProblemHttpResult>> HandleAsync(IRequestHandler<Query, Result<ProfileDto>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new Query(), cancellationToken)).ToOk();
    }
}
