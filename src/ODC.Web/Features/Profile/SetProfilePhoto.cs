using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Media;

namespace ODC.Web.Features.Profile;

/// <summary>
/// Replaces the profile photo (mandatory: until it is set the API answers 403 to everything but the sign-in and this).
/// </summary>
public static class SetProfilePhoto
{
    /// <summary>
    /// The request.
    /// </summary>
    /// <param name="File">The photo.</param>
    /// <param name="Crop">The crop square as fractions of the image; the centre square when missing.</param>
    public sealed record Command(FileUpload File, ImageCrop? Crop) : ICommand<Result<Guid>>;

    /// <summary>
    /// Stores the avatar (AVIF, 320 px square) and removes the previous one after the save.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="account">The current user's account.</param>
    /// <param name="media">The image library.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, ProfileAccount account, MediaLibrary media, IAuditLogger audit) : IRequestHandler<Command, Result<Guid>>
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> HandleAsync(Command request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await account.LoadAsync(cancellationToken);
            if (user is null)
            {
                return Error.Unauthorized();
            }

            var added = await media.AddAsync(request.File, MediaPurpose.Avatar, user.Id, request.Crop, cancellationToken);
            if (!added.IsSuccess)
            {
                return added.Error;
            }

            var previous = user.PhotoId is { } old ? await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == old, cancellationToken) : null;
            user.SetPhoto(added.Value.Id);
            if (previous is not null)
            {
                media.Remove(previous);
            }

            audit.Add(AuditActions.ProfilePhotoChanged, nameof(User), user.Id.ToString(), null);
            await media.SaveChangesAsync(cancellationToken);
            return added.Value.Id;
        }
    }

    /// <summary>
    /// <c>PUT /api/v1/profile/photo</c> (multipart).
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
            => ProfileAccount.MapProfile(api).MapPut("photo", HandleAsync).WithMetadata(new RequestSizeLimitAttribute(FileRules.MaxImageSize + (64 * 1024)));

        /// <summary>
        /// Replaces the profile photo (JPEG, PNG, WebP, AVIF or GIF up to 15 MB, stored as AVIF): the chosen square, or
        /// the centre square when no crop is sent.
        /// </summary>
        /// <param name="file">The photo (required; optional only in the document because the client builds the form itself).</param>
        /// <param name="crop">The crop square as fractions of the image (optional).</param>
        /// <param name="handler">The handler.</param>
        /// <param name="principal">The signed-in user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="signInManager">The sign-in manager.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identifier of the new photo (its URL is <c>/media/{id}</c>).</returns>
        private static async Task<Results<Ok<Guid>, ProblemHttpResult>> HandleAsync(
            IFormFile? file,
            [FromForm] PhotoCropForm crop,
            IRequestHandler<Command, Result<Guid>> handler,
            ClaimsPrincipal principal,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            CancellationToken cancellationToken)
        {
            if (file is null)
            {
                return Error.Validation(Texts.T("Оберіть фото.")).ToProblem();
            }

            await using var content = file.OpenReadStream();
            var result = await handler.HandleAsync(new Command(new FileUpload(content, file.FileName, file.ContentType, file.Length), crop.ToCrop()), cancellationToken);
            if (result.IsSuccess)
            {
                // The access token carries the photo: without a new one the cabinet would keep asking for it.
                await ProfileAccount.RefreshSignInAsync(principal, userManager, signInManager);
            }

            return result.ToOk();
        }
    }
}
