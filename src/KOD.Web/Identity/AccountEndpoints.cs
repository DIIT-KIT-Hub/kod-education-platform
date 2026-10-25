using KOD.Application.Abstractions;
using KOD.Domain.Audit;
using KOD.Domain.Users;
using KOD.Web.Security;
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
            group.MapPost("/logout", static async (HttpContext context, SignInManager<User> signInManager, IAuditLogger audit) =>
            {
                var user = context.User;
                await audit.WriteAsync(AuditActions.SignOut, user.UserId, user.FullName, null, context.RequestAborted);
                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect("~/account/login");
            });

            return group;
        }
    }
}
