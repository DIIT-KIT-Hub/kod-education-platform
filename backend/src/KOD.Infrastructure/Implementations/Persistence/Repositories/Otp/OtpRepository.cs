using KOD.Domain.Entities.Otp;
using KOD.Domain.Repositories;
using KOD.Domain.ValueObjects.Otp;
using KOD.Infrastructure.Implementations.Persistence.Database;

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
    public async Task<OtpCodeDetails?> GetOtpCodeDetailsByUserIdAsync(Guid userId)
        => await _dbContext.OtpCodes
        .Where(o => o.UserId == userId)
        .Select(o => new OtpCodeDetails(o.Code, o.ExpiresAt))
        .FirstOrDefaultAsync();

    /// <inheritdoc />
    public async Task DeleteOtpCodeByUserIdAsync(Guid userId)
    {
        var otp = await _dbContext.OtpCodes.FirstOrDefaultAsync(o => o.UserId == userId);

        if (otp is not null)
        {
            _dbContext.OtpCodes.Remove(otp);

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<DateTime> AddOtpCodeAsync(string otpCode, Guid userId)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(1);
        var otp = new OtpCode()
        {
            Id = Guid.NewGuid(),
            Code = otpCode,
            UserId = userId,
            ExpiresAt = expiresAt
        };

        await _dbContext.OtpCodes.AddAsync(otp);

        await _dbContext.SaveChangesAsync();

        return expiresAt;
    }

    #endregion
}
