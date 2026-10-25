using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Identity;

/// <summary>
/// Minimal API endpoints used by the account pages.
/// </summary>
internal static class AccountEndpoints
{
    /// <param name="endpoints">The endpoint route builder.</param>
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>POST /account/logout</c> (antiforgery-protected).
        /// </summary>
        /// <returns>The endpoint convention builder.</returns>
        public IEndpointConventionBuilder MapAccountEndpoints()
        {
            var group = endpoints.MapGroup("/account");
            group.MapPost("/logout", static async (HttpContext context, IAntiforgery antiforgery, SignInManager<User> signInManager, IAuditLogger audit) =>
            {
                // The endpoint binds no form fields, so the token is validated explicitly (a cross-site form cannot sign users out).
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    return Results.BadRequest();
                }

                var user = context.User;
                await audit.WriteAsync(AuditActions.SignOut, user.UserId, user.FullName, null, context.RequestAborted);
                await signInManager.SignOutAsync();
                return Results.LocalRedirect("~/account/login");
            })
            .DisableAntiforgery();

            return group;
        }
    }
}
