using System.Globalization;
using KOD.Application.Abstractions;
using KOD.Application.Common.Files;
using KOD.Application.Features.Profile;
using KOD.Domain.Audit;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using KOD.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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
        /// Maps <c>POST /account/logout</c> and <c>POST /account/photo</c> (antiforgery-protected).
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

            group.MapPost("/photo", UploadPhotoAsync)
                .WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxImageSize + (64 * 1024)));

            return group;
        }
    }

    /// <summary>
    /// Saves a new profile photo (a plain multipart form, so it works before any script loads) and reissues the sign-in
    /// cookie, which carries the photo; on an error the photo page shows the message.
    /// </summary>
    /// <param name="photo">The uploaded image.</param>
    /// <param name="form">The crop square (fractions) and the page to return to.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="profile">The profile service.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <returns>A redirect.</returns>
    private static async Task<IResult> UploadPhotoAsync(
        IFormFile? photo,
        [FromForm] PhotoForm form,
        HttpContext context,
        IProfileService profile,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        var returnUrl = IdentityRedirectManager.SafeLocal(form.ReturnUrl);
        string error;
        if (photo is null || photo.Length == 0)
        {
            error = Texts.T("Оберіть фото.");
        }
        else
        {
            await using var content = photo.OpenReadStream();
            var result = await profile.SetPhotoAsync(new FileUpload(content, photo.FileName, photo.ContentType, photo.Length), form.Crop, context.RequestAborted);
            if (result.IsSuccess)
            {
                if (await userManager.GetUserAsync(context.User) is { } user)
                {
                    await signInManager.RefreshSignInAsync(user);
                }

                return Results.LocalRedirect(QueryHelpers.AddQueryString(returnUrl, "toast", Texts.T("Фото профілю збережено")));
            }

            error = result.Error.Message;
        }

        return Results.LocalRedirect($"{AccountSetupMiddleware.PhotoPath}?returnUrl={Uri.EscapeDataString(returnUrl)}&error={Uri.EscapeDataString(error)}");
    }

    /// <summary>
    /// The text fields of the photo form.
    /// </summary>
    internal sealed class PhotoForm
    {
        /// <summary>
        /// Gets or sets the left edge of the crop square (a fraction, invariant culture).
        /// </summary>
        public string? CropLeft { get; set; }

        /// <summary>
        /// Gets or sets the top edge of the crop square.
        /// </summary>
        public string? CropTop { get; set; }

        /// <summary>
        /// Gets or sets the width of the crop square.
        /// </summary>
        public string? CropWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the crop square.
        /// </summary>
        public string? CropHeight { get; set; }

        /// <summary>
        /// Gets or sets the local page to return to.
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// Gets the crop square, or <see langword="null"/> when the browser sent none (no script): the centre is used.
        /// </summary>
        public ImageCrop? Crop
            => Parse(CropLeft) is { } left && Parse(CropTop) is { } top && Parse(CropWidth) is { } width && Parse(CropHeight) is { } height
                ? new ImageCrop(left, top, width, height)
                : null;

        /// <summary>
        /// Parses a fraction.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <returns>The number or <see langword="null"/>.</returns>
        private static double? Parse(string? value)
            => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) ? number : null;
    }
}
