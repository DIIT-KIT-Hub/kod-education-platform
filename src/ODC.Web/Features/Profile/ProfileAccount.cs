using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;

namespace ODC.Web.Features.Profile;

/// <summary>
/// The current user's account as the profile slices load and change it.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The administration scopes.</param>
internal sealed class ProfileAccount(ApplicationDbContext db, ICurrentUser currentUser, IAccessScope accessScope)
{
    /// <summary>
    /// The route group of the profile endpoints.
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group <c>/api/v1/profile</c>.</returns>
    public static RouteGroupBuilder MapProfile(IEndpointRouteBuilder api) => api.MapGroup("profile").WithTags("Profile");

    /// <summary>
    /// Re-issues the token cookies of this browser after a change the access token carries (name, e-mail, photo,
    /// security stamp). It keeps <c>auth_time</c>, so the absolute session lifetime is not extended.
    /// </summary>
    /// <param name="principal">The signed-in user.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <returns>A task that completes when the cookies are written.</returns>
    public static async Task RefreshSignInAsync(System.Security.Claims.ClaimsPrincipal principal, UserManager<User> userManager, SignInManager<User> signInManager)
    {
        if (await userManager.GetUserAsync(principal) is { } user)
        {
            await signInManager.RefreshSignInAsync(user);
        }
    }

    /// <summary>
    /// Loads the tracked current user with positions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user or <see langword="null"/>.</returns>
    public async Task<User?> LoadWithPositionsAsync(CancellationToken cancellationToken)
        => currentUser.Id is { } me
            ? await db.Users
                .Include(static u => u.Positions).ThenInclude(static p => p.OrganizationUnit)
                .Include(static u => u.Positions).ThenInclude(static p => p.Role)
                .FirstOrDefaultAsync(u => u.Id == me, cancellationToken)
            : null;

    /// <summary>
    /// Loads the tracked current user without related data.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user or <see langword="null"/>.</returns>
    public async Task<User?> LoadAsync(CancellationToken cancellationToken)
        => currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;

    /// <summary>
    /// Determines whether the user may change own name and e-mail: like in the user administration, only an
    /// administrator of users whose scope covers every position of the account (a system administrator always).
    /// </summary>
    /// <param name="user">The current user with positions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public async Task<bool> CanEditPersonalDataAsync(User user, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Users.Manage))
        {
            return false;
        }

        var scope = await accessScope.GetAdministrationScopeAsync(Permissions.Users.Manage, cancellationToken);
        return scope.Covers([.. user.Positions.Select(static p => p.OrganizationUnitId)]);
    }
}
