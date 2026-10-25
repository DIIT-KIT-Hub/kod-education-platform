using KOD.Application.Features.Profile;
using KOD.Web.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The signed-in user's profile and sessions.
/// </summary>
/// <param name="profile">The profile service.</param>
[Route("api/v1/profile")]
public sealed class ProfileController(IProfileService profile) : ApiControllerBase
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
    /// Lists active sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sessions.</returns>
    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> Sessions(CancellationToken cancellationToken)
        => FromResult(await profile.GetSessionsAsync(cancellationToken));

    /// <summary>
    /// Ends a session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("sessions/{id:guid}")]
    public async Task<ActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
        => FromResult(await profile.RevokeSessionAsync(id, cancellationToken));

    /// <summary>
    /// Ends all sessions except the current one.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("sessions")]
    public async Task<ActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
    {
        var result = await profile.RevokeOtherSessionsAsync(cancellationToken);
        return result.IsSuccess ? NoContent() : ProblemFor(result.Error);
    }
}
