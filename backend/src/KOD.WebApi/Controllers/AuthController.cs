using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.DTOs.Auth;
using KOD.Application.DTOs.Tokens;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Controllers;

/// <summary>
/// Provides endpoints for authentication operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    #region Private fields

    /// <summary>
    /// The authentication service used to handle authentication operations.
    /// </summary>
    private readonly IAuthService _authService;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class with the specified authentication service.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public AuthController(IAuthService authService) => _authService = authService;

    #endregion

    #region Endpoints

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    /// <param name="request">The login request containing username and password.</param>
    /// <returns>An <see cref="IActionResult"/> containing the authentication result.</returns>
    /// <response code="200">Successful login, returns access and refresh tokens.</response>
    /// <response code="400">Unsuccessful login, invalid login credentials.</response>
    /// <response code="401">Unsuccessful login, user is not verified.</response>
    /// <response code="404">Unsuccessful login, user or his roles not found.</response>
    /// <response code="500">Unsuccessful login, internal server error.</response>
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequestDto request)
        => Ok(await _authService.LoginAsync(request));

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>An <see cref="IActionResult"/> containing the new access and refresh tokens.</returns>
    /// <response code="200">Successful refresh, returns access and refresh tokens.</response>
    /// <response code="401">Unsuccessful refresh, refresh token is invalid.</response>
    /// <response code="404">Unsuccessful refresh, refresh token or user not found.</response>
    /// <response code="500">Unsuccessful refresh, internal server error.</response>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync(RefreshTokenRequestDto request)
        => Ok(await _authService.RefreshTokenAsync(request));

    /// <summary>
    /// Logs out a user by invalidating the provided refresh token.
    /// </summary>
    /// <param name="request">The refresh token request to logout.</param>
    /// <returns>An <see cref="IActionResult"/> indicating success or failure of logout.</returns>
    /// <response code="200">Successful logout, returns boolean true.</response>
    /// <response code="404">Unsuccessful logout, refresh token not found.</response>
    /// <response code="500">Unsuccessful logout, internal server error.</response>
    [Authorize]
    [HttpPost("logout")]  
    public async Task<IActionResult> LogoutAsync(RefreshTokenRequestDto request)
    {
        await _authService.LogoutAsync(request);

        return Ok();
    }

    #endregion
}
