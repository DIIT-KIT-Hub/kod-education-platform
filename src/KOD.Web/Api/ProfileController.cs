using KOD.Application.Common.Files;
using KOD.Application.Features.Profile;
using KOD.Domain.Users;
using KOD.Web.Api.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The signed-in user's profile, photo and sessions.
/// </summary>
/// <param name="profile">The profile service.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="signInManager">The sign-in manager.</param>
[Route("api/v1/profile")]
public sealed class ProfileController(IProfileService profile, UserManager<User> userManager, SignInManager<User> signInManager) : ApiControllerBase
{
    /// <summary>
    /// Gets the profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The profile.</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken cancellationToken)
        => FromResult(await profile.GetAsync(cancellationToken));

    /// <summary>
    /// Updates the contact phone number.
    /// </summary>
    /// <param name="request">The changes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPatch]
    public async Task<ActionResult> Update(ProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await profile.UpdatePhoneAsync(request.PhoneNumber, cancellationToken));
    }

    /// <summary>
    /// Replaces the profile photo (JPEG, PNG, WebP, AVIF or GIF up to 15 MB; the centre square is used, stored as AVIF).
    /// </summary>
    /// <param name="file">The photo.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifier of the new photo (its URL is <c>/media/{id}</c>).</returns>
    [HttpPut("photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxImageSize + (64 * 1024))]
    public async Task<ActionResult<Guid>> SetPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using var content = file.OpenReadStream();
        var result = await profile.SetPhotoAsync(new FileUpload(content, file.FileName, file.ContentType, file.Length), crop: null, cancellationToken);
        if (result.IsSuccess && await userManager.GetUserAsync(User) is { } user)
        {
            // The sign-in cookie carries the photo: without it the cabinet would keep asking for one.
            await signInManager.RefreshSignInAsync(user);
        }

        return FromResult(result);
    }

    /// <summary>
    /// Signs out on every other device: tokens issued before are no longer renewed, this browser gets new ones.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("sign-out-everywhere")]
    public async Task<ActionResult> SignOutEverywhere(CancellationToken cancellationToken)
    {
        var result = await profile.SignOutEverywhereAsync(cancellationToken);
        if (result.IsSuccess && await userManager.GetUserAsync(User) is { } user)
        {
            await signInManager.RefreshSignInAsync(user);
        }

        return result.IsSuccess ? NoContent() : ProblemFor(result.Error);
    }
}
