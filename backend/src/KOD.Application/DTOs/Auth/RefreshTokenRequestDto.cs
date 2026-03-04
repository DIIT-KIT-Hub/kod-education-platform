using System.ComponentModel.DataAnnotations;

namespace KOD.Application.DTOs.Auth;

/// <summary>
/// Data transfer object representing a refresh token request.
/// </summary>
/// <param name="RefreshToken">The refresh token value.</param>
public sealed record RefreshTokenRequestDto(string RefreshToken);

