using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Profile;

/// <summary>
/// Updates the contact phone number and, for users who administer their whole account, the name and the e-mail.
/// </summary>
public static class UpdateProfile
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="Changes">The changes; a missing name part or e-mail keeps the stored one.</param>
    public sealed record Command(ProfileUpdateRequest Changes) : ICommand<Result>;

    /// <summary>
    /// Saves the phone number at once and the personal data only when allowed.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="account">The current user's account.</param>
    /// <param name="identity">The identity store.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, ProfileAccount account, IIdentityService identity, IAuditLogger audit) : IRequestHandler<Command, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var update = request.Changes;
            var user = await account.LoadWithPositionsAsync(cancellationToken);
            if (user is null)
            {
                return Error.Unauthorized();
            }

            var lastName = update.LastName ?? user.LastName;
            var firstName = update.FirstName ?? user.FirstName;
            var middleName = update.MiddleName ?? user.MiddleName;
            var email = update.Email is null ? user.Email! : EmailAddress.Normalize(update.Email);
            if (email is null)
            {
                return Error.Validation(Texts.T("Вкажіть коректну адресу електронної пошти."));
            }

            var personalDataChanged = !string.Equals(lastName.Trim(), user.LastName, StringComparison.Ordinal)
                || !string.Equals(firstName.Trim(), user.FirstName, StringComparison.Ordinal)
                || !string.Equals(string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim(), user.MiddleName, StringComparison.Ordinal)
                || !string.Equals(email, user.Email, StringComparison.Ordinal);
            if (!personalDataChanged)
            {
                return await DomainErrors.GuardAsync(async () =>
                {
                    user.UpdatePersonalData(user.LastName, user.FirstName, user.MiddleName, update.PhoneNumber);
                    await db.SaveChangesAsync(cancellationToken);
                    return Result.Success();
                });
            }

            if (!await account.CanEditPersonalDataAsync(user, cancellationToken))
            {
                return Error.Forbidden(Texts.T("ПІБ та електронну пошту змінює адміністратор Вашої організації або відділ кадрів."));
            }

            var normalizedEmail = email.ToUpperInvariant();
            if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != user.Id, cancellationToken))
            {
                return Error.Conflict(Texts.T("Користувач із такою електронною поштою вже існує."));
            }

            return await DomainErrors.GuardAsync(async () =>
            {
                user.Email = email;
                user.UserName = email;
                user.UpdatePersonalData(lastName, firstName, middleName, update.PhoneNumber);
                audit.Add(AuditActions.UserUpdated, nameof(User), user.Id.ToString(), $"Змінено {user.Email} (профіль)");
                return await identity.UpdateAsync(user);
            });
        }
    }

    /// <summary>
    /// <c>PATCH /api/v1/profile</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => ProfileAccount.MapProfile(api).MapPatch(string.Empty, HandleAsync);

        /// <summary>
        /// Updates the contact phone number and, for users who administer their whole account, the name and the e-mail.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="principal">The signed-in user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            ProfileUpdateRequest changes,
            IRequestHandler<Command, Result> handler,
            ClaimsPrincipal principal,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            CancellationToken cancellationToken)
        {
            var result = await handler.HandleAsync(new Command(changes), cancellationToken);
            if (result.IsSuccess && (changes.LastName ?? changes.FirstName ?? changes.MiddleName ?? changes.Email) is not null)
            {
                // The access token carries the name and the e-mail: re-issue it so the cabinet shows the new ones at once.
                await ProfileAccount.RefreshSignInAsync(principal, userManager, signInManager);
            }

            return result.ToNoContent();
        }
    }
}
