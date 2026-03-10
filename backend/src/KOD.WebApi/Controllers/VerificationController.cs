using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Application.DTOs.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Controllers;

/// <summary>
/// Provides endpoints for user verification.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class VerificationController : ControllerBase
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
    /// Initializes a new instance of the <see cref="VerificationController"/> class with the specified services.
    /// </summary>
    /// <param name="identityService">The identity service.</param>
    /// <param name="otpService">The OTP service.</param>
    public VerificationController(IIdentityService identityService, IOtpService otpService)
    {
        _identityService = identityService;
        _otpService = otpService;
    }

    #endregion

    /// <summary>
    /// Requests an OTP code for the specified username.
    /// </summary>
    /// <param name="email">The email of the user requesting the OTP.</param>
    /// <returns>An <see cref="IActionResult"/> containing the result of the OTP request.</returns>
    /// <response code="200">Successful otp request, returns boolean true.</response>
    /// <response code="404">Unsuccessful otp request, user not found.</response>
    /// <response code="409">Unsuccessful otp request, user already verified.</response>
    /// <response code="500">Unsuccessful otp request, internal server error.</response>
    [HttpPost("otp")]
    public async Task<IActionResult> RequestOtpAsync(string email)
    {
        var userDetails = await _identityService.GetUserOtpDetailsByEmailAsync(email);

        await _otpService.RequestOtpCodeAsync(userDetails);

        return Ok();
    }

    /// <summary>
    /// Confirms a user's account using username, password, and OTP code.
    /// </summary>
    /// <param name="confirmUserDto">The DTO containing username, password, and OTP code.</param>
    /// <returns>An <see cref="IActionResult"/> indicating success or failure of user confirmation.</returns>
    /// <response code="200">Successful confirm, returns boolean true.</response>
    /// <response code="400">Unsuccessful confirm, otp code is invalid.</response>
    /// <response code="401">Unsuccessful confirm, otp code is expired.</response>
    /// <response code="404">Unsuccessful confirm, otp code or user not found.</response>
    /// <response code="409">Unsuccessful confirm, user already verified.</response>
    /// <response code="500">Unsuccessful confirm, internal server error.</response>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmUserAsync(ConfirmUserDto confirmUserDto)
    {
        var userDetails = await _identityService.GetUserOtpDetailsByEmailAsync(confirmUserDto.Email);

        var checkOtpResult = await _otpService.CheckOtpCodeAsync(confirmUserDto.OtpCode, userDetails);

        if (!checkOtpResult)
        {
            return BadRequest("Otp code is invalid.");
        }

        await _identityService.ConfirmUserAsync(confirmUserDto.Email, confirmUserDto.Password);

        return Ok();
    }

    [HttpGet("admin-test")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnlyEndpoint() => Ok("You are Admin ✅");

    [HttpGet("user-test")]
    [Authorize(Roles = "User")]
    public IActionResult UserOnlyEndpoint() => Ok("You are User ✅");
}
