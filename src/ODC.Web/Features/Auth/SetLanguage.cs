using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Auth;

/// <summary>
/// Changes the interface language: remembered in this browser and, for a signed-in user, saved in the profile with the
/// tokens re-issued so the new language applies at once.
/// </summary>
public static class SetLanguage
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Language">A supported language code (<see cref="Languages"/>).</param>
    public sealed record Command(string Language) : ICommand<Result>;

    /// <summary>
    /// Saves the language in the current user's profile.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="currentUser">The current user.</param>
    internal sealed class Handler(MasterDbContext db, ICurrentUser currentUser) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = currentUser.Id is { } me ? await db.Users.FirstOrDefaultAsync(u => u.Id == me, cancellationToken) : null;
            if (user is null)
            {
                return Error.Unauthorized();
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                user.SetLanguage(request.Language);
                await db.SaveChangesAsync(cancellationToken);
                return Result.Success();
            });
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/auth/language</c> (also before signing in).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => AuthApi.MapAuth(api).MapPut("language", HandleAsync).AllowAnonymous();

        /// <summary>
        /// Changes the interface language: remembered in this browser and, for a signed-in user, saved in the profile with
        /// the tokens re-issued so the new language applies at once.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="context">The HTTP context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            LanguageRequest language,
            IRequestHandler<Command, Result> handler,
            HttpContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            CancellationToken cancellationToken)
        {
            if (Languages.Normalize(language.Language) is not { } code)
            {
                return Error.Validation(Texts.T("Непідтримувана мова інтерфейсу.")).ToProblem();
            }

            if (context.User.UserId is not null)
            {
                var saved = await handler.HandleAsync(new Command(code), cancellationToken);
                if (!saved.IsSuccess)
                {
                    return saved.Error.ToProblem();
                }

                if (await userManager.GetUserAsync(context.User) is { } user)
                {
                    await signInManager.RefreshSignInAsync(user);
                }
            }

            LanguageCultureProvider.Remember(context.Response, code);
            return TypedResults.NoContent();
        }
    }
}
