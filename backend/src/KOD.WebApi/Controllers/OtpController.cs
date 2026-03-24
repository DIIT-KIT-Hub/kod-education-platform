using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;
using KOD.WebApi.Extensions.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Controllers;

/// <summary>
/// Provides endpoints for OTP (One-Time Password) operations.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    #region Private fields

    /// <summary>
    /// The identity service used to access and confirm user data.
    /// </summary>
    private readonly IIdentityService _identityService;

    /// <summary>
    /// The OTP service used to request and check one-time passwords.
    /// </summary>
    private readonly IOtpService _otpService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpController"/> class.
    /// </summary>
    /// <param name="identityService">Service responsible for retrieving user information required for OTP generation.</param>
    /// <param name="otpService">Service responsible for generating and sending OTP codes.</param>
    public OtpController(IIdentityService identityService, IOtpService otpService)
    {
        _identityService = identityService;
        _otpService = otpService;
    }

    #endregion

    #region Endpoints

    /// <summary>
    /// Sends an OTP code to the specified user's email.
    /// </summary>
    /// <param name="email">The email address of the user to whom the OTP code will be sent.</param>
    /// <returns>Returns a result indicating whether the OTP code was successfully sent.</returns>
    /// <response code="200">OTP code successfully sent.</response>
    /// <response code="404">User with the specified email was not found.</response>
    /// <response code="409">User is already verified and does not require OTP.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Roles = "Verification")]
    [HttpGet("send")]
    public async Task<IActionResult> SendOtpAsync(string email)
    {
        var userDetailsResult = await _identityService.GetUserOtpDetailsByEmailAsync(email);

        if (!userDetailsResult.IsSuccess)
        {
            return userDetailsResult.ToActionResult();
        }

        var sentOtpResult = await _otpService.SendOtpCodeAsync(userDetailsResult.Value!);

        return sentOtpResult.ToActionResult();
    }

    #endregion
}
