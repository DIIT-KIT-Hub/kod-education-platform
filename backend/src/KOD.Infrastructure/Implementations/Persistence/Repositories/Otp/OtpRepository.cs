using KOD.Application.Abstractions.Persitence.Repositories.Otp;
using KOD.Application.DTOs.Otp;
using KOD.Application.Result;
using KOD.Domain.Entities.Otp;
using KOD.Infrastructure.Implementations.Persistence.Database;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Implementations.Persistence.Repositories.Otp;

/// <summary>
/// Implements <see cref="IOtpRepository"/> for managing OTP codes in the database.
/// </summary>
internal sealed class OtpRepository : IOtpRepository
{
    #region Private fields

    /// <summary>
    /// The database context used to access authentication-related entities.
    /// </summary>
    private readonly ApplicationDbContext _dbContext;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpRepository"/> class with the specified database context.
    /// </summary>
    /// <param name="dbContext">The application's database context.</param>
    public OtpRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ApiResult<OtpCodeDto>> GetOtpCodeByUserIdAsync(Guid userId)
    {
        var otpCode = await _dbContext.OtpCodes
            .Where(o => o.UserId == userId)
            .Select(o => new OtpCodeDto(o.Code, o.ExpiresAt))
            .FirstOrDefaultAsync();

        if (otpCode is null)
        {
            return ApiResult<OtpCodeDto>.Failure(StatusCodes.Status404NotFound, "Otp code not found.");
        }

        return ApiResult<OtpCodeDto>.Success(otpCode);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> DeleteAllOtpCodesByUserIdAsync(Guid userId)
    {
        var otps = await _dbContext.OtpCodes
            .Where(o => o.UserId == userId)
            .ToListAsync();

        if (otps.Count != 0)
        {
            _dbContext.OtpCodes.RemoveRange(otps);

            var result = await _dbContext.SaveChangesAsync();
            if(result == 0)
            {
                return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during all otp codes deletion.");
            }
        }

        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> DeleteOtpCodeByUserIdAsync(Guid userId)
    {
        var otp = await _dbContext.OtpCodes.FirstOrDefaultAsync(o => o.UserId == userId);

        if (otp is null)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status404NotFound, "Otp code not found.");
        }

        _dbContext.OtpCodes.Remove(otp);

        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during otp code deletion.");
        }

        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> AddOtpCodeAsync(string otpCode, Guid userId)
    {
        var otp = new OtpCode()
        {
            Id = Guid.NewGuid(),
            Code = otpCode,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        await _dbContext.OtpCodes.AddAsync(otp);

        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during otp code insert.");
        }

        return ApiResult<bool>.Success(true);
    }

    #endregion
}
