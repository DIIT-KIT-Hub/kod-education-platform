using KOD.Application.Features.Profile;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using KOD.Web.Identity;
using KOD.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Localization;

/// <summary>
/// The endpoint behind the language switch.
/// </summary>
internal static class LanguageEndpoints
{
    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>POST /account/language</c> (antiforgery-protected form): remembers the language in this browser and, for a
        /// signed-in user, saves it in the profile and reissues the sign-in cookie so the new language applies at once.
        /// </summary>
        /// <returns>The endpoint convention builder.</returns>
        public IEndpointConventionBuilder MapLanguageEndpoint()
            => endpoints.MapPost("/account/language", static async (
                    [FromForm] string language,
                    [FromForm] string? returnUrl,
                    HttpContext context,
                    IProfileService profile,
                    UserManager<User> userManager,
                    SignInManager<User> signInManager) =>
                {
                    if (Languages.Normalize(language) is not { } code)
                    {
                        return Results.BadRequest();
                    }

                    if (context.User.UserId is not null)
                    {
                        var saved = await profile.SetLanguageAsync(code, context.RequestAborted);
                        if (saved.IsSuccess && await userManager.GetUserAsync(context.User) is { } user)
                        {
                            await signInManager.RefreshSignInAsync(user);
                        }
                    }

                    LanguageCultureProvider.Remember(context.Response, code);
                    return Results.LocalRedirect(IdentityRedirectManager.SafeLocal(returnUrl));
                })
                .AllowAnonymous();
    }
}
