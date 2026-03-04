using KOD.Application.Abstractions.Persitence.Repositories.Auth;
using KOD.Application.Abstractions.Persitence.Repositories.Identity;
using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.DTOs.Auth;
using KOD.Application.Result;
using KOD.Domain.Entities.Users;

using Microsoft.AspNetCore.Http;

namespace KOD.Infrastructure.Implementations.Services.Auth;

/// <summary>
/// Implements <see cref="IAuthService"/> for handling authentication operations such as login, token refresh, and logout.
/// </summary>
internal sealed class AuthService : IAuthService
{
    #region Private fields

    /// <summary>
    /// The JWT service used to generate access and refresh tokens.
    /// </summary>
    private readonly IJwtService _jwtService;

    /// <summary>
    /// The repository for managing refresh tokens in the database.
    /// </summary>
    private readonly IAuthRepository _authRepository;

    /// <summary>
    /// The repository for accessing user identity data.
    /// </summary>
    private readonly IIdentityRepository _identityRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class with the specified services and repositories.
    /// </summary>
    /// <param name="jwtService">The JWT service.</param>
    /// <param name="authRepository">The authentication repository.</param>
    /// <param name="identityRepository">The identity repository.</param>
    public AuthService(IJwtService jwtService, IAuthRepository authRepository, IIdentityRepository identityRepository)
    {
        _jwtService = jwtService;
        _authRepository = authRepository;
        _identityRepository = identityRepository;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ApiResult<TokenResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var userResult = await _identityRepository.GetUserByUsernameAsync(request.Username);
        if (!userResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(userResult.StatusCode, userResult.Message!);
        }

        var user = userResult.Data!;
        if (!user.IsVerified)
        {
            return ApiResult<TokenResponseDto>.Failure(StatusCodes.Status403Forbidden, "User is not verified.");
        }

        var passwordResult = await _identityRepository.CheckUserPasswordAsync(user, request.Password);
        if (!passwordResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(passwordResult.StatusCode, passwordResult.Message!);
        }

        var userRolesResult = await _identityRepository.GetUserRolesAsync(user);
        if (!userResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(userResult.StatusCode, userResult.Message!);
        }

        var tokenResponseDto = await GenerateTokens(user, userRolesResult.Data!);
        if (!tokenResponseDto.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(tokenResponseDto.StatusCode, tokenResponseDto.Message!);
        }

        return ApiResult<TokenResponseDto>.Success(tokenResponseDto.Data);
    }

    /// <inheritdoc />
    public async Task<ApiResult<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshTokenResult = await _authRepository.GetRefreshTokenByValueAsync(request.RefreshToken);
        if (!refreshTokenResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(refreshTokenResult.StatusCode, refreshTokenResult.Message!);
        }

        var userRolesResult = await _identityRepository.GetUserRolesAsync(refreshTokenResult.Data!.User!);
        if (!userRolesResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(userRolesResult.StatusCode, userRolesResult.Message!);
        }

        return await GenerateTokens(refreshTokenResult.Data!.User!, userRolesResult.Data!);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> LogoutAsync(RefreshTokenRequestDto request)
    {
        var refreshTokenDeleteResult = await _authRepository.DeleteRefreshTokenAsync(request.RefreshToken);
        if (!refreshTokenDeleteResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(refreshTokenDeleteResult.StatusCode, refreshTokenDeleteResult.Message!);
        }

        return ApiResult<bool>.Success(true);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Generates access and refresh tokens for the specified user and updates the refresh token in the database.
    /// </summary>
    /// <param name="user">The user for whom the tokens are generated.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the generated access and refresh tokens, or a failure result if updating the refresh token fails.</returns>
    private async Task<ApiResult<TokenResponseDto>> GenerateTokens(ApplicationUser user, IEnumerable<string> roles)
    {
        var accessTokenResult = _jwtService.GenerateAccessToken(user, roles);
        var refreshTokenResult = _jwtService.GenerateRefreshToken(user);

        var refreshTokenUpdateResult = await _authRepository.UpdateUserRefreshTokenAsync(refreshTokenResult.Data!);
        if (!refreshTokenUpdateResult.IsSuccess)
        {
            return ApiResult<TokenResponseDto>.Failure(refreshTokenUpdateResult.StatusCode, refreshTokenUpdateResult.Message!);
        }

        return ApiResult<TokenResponseDto>.Success(new TokenResponseDto(
          accessTokenResult.Data!.Token,
          accessTokenResult.Data!.ExpiresAt,
          refreshTokenResult.Data!.Token,
          refreshTokenResult.Data!.ExpiresAt
          ));
    }

    #endregion
}
