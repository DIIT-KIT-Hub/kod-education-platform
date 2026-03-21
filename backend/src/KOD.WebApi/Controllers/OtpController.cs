using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;
using KOD.WebApi.Extensions.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    /// <summary>
    /// The identity service used to access and confirm user data.
    /// </summary>
    private readonly IIdentityService _identityService;

    /// <summary>
    /// The OTP service used to request and check one-time passwords.
    /// </summary>
    private readonly IOtpService _otpService;

    public OtpController(IIdentityService identityService, IOtpService otpService)
    {
        _identityService = identityService;
        _otpService = otpService;
    }

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
}
