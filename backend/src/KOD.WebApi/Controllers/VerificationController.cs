using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Application.DTOs.Tokens;
using KOD.Application.DTOs.Users;
using KOD.Application.Results;
using KOD.WebApi.Extensions.Results;

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

    private readonly IJwtService _jwtService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationController"/> class with the specified services.
    /// </summary>
    /// <param name="identityService">The identity service.</param>
    /// <param name="otpService">The OTP service.</param>
    /// <param name="jwtService">The JWT service.</param>
    public VerificationController(IIdentityService identityService, IOtpService otpService, IJwtService jwtService)
    {
        _identityService = identityService;
        _otpService = otpService;
        _jwtService = jwtService;
    }

    #endregion

    [HttpGet("token")]
    public async Task<IActionResult> GenerateVerificationToken(string email)
    {
        var existsResult = await _identityService.CheckUserExistenceByEmailAsync(email);

        if (!existsResult.IsSuccess)
        {
            return existsResult.ToActionResult();
        }

        var verificationResult = await _identityService.CheckUserVerificationByEmailAsync(email);

        if (!verificationResult.IsSuccess)
        {
            return verificationResult.ToActionResult();
        }

        var verificationToken = _jwtService.GenerateVerificationToken();

        return Result<AccessTokenDto>.Success(verificationToken).ToActionResult();
    }

    /// <summary>
    /// Confirms a user's account using username, password, and OTP code.
    /// </summary>
    /// <param name="confirmUserDto">The DTO containing username, password, and OTP code.</param>
    /// <returns>An <see cref="IActionResult"/> indicating success or failure of user confirmation.</returns>
    /// <response code="200">Successful confirm, returns boolean true.</response>
    /// <response code="400">Unsuccessful confirm, otp code is invalid.</response>
    /// <response code="410">Unsuccessful confirm, otp code is expired.</response>
    /// <response code="404">Unsuccessful confirm, otp code or user not found.</response>
    /// <response code="409">Unsuccessful confirm, user already verified.</response>
    /// <response code="500">Unsuccessful confirm, internal server error.</response>
    [Authorize(Roles = "Verification")]
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyUserAsync(ConfirmUserDto confirmUserDto)
    {
        var userDetailsResult = await _identityService.GetUserOtpDetailsByEmailAsync(confirmUserDto.Email);

        if (!userDetailsResult.IsSuccess)
        {
            return userDetailsResult.ToActionResult();
        }

        var checkOtpResult = await _otpService.CheckOtpCodeAsync(confirmUserDto.OtpCode, userDetailsResult.Value!);

        if (!checkOtpResult.IsSuccess)
        {
            return checkOtpResult.ToActionResult();
        }

        return (await _identityService.VerifyUserAsync(confirmUserDto.Email, confirmUserDto.Password)).ToActionResult();
    }

    [HttpGet("admin-test")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnlyEndpoint() => Ok("You are Admin ✅");

    [HttpGet("user-test")]
    [Authorize(Roles = "User")]
    public IActionResult UserOnlyEndpoint() => Ok("You are User ✅");
}
