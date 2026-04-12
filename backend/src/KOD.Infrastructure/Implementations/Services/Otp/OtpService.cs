using System.Globalization;
using System.Security.Cryptography;

using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Application.Abstractions.Services.Emails;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Application.DTOs.Users;
using KOD.Application.Results;
using KOD.Domain.Repositories;

namespace KOD.Infrastructure.Implementations.Services.Otp;

/// <summary>
/// Implements <see cref="IOtpService"/> for managing OTP codes and sending them via email.
/// </summary>
internal sealed class OtpService : IOtpService
{
    #region Private fields

    /// <summary>
    /// The repository used to manage OTP codes in the database.
    /// </summary>

    private readonly IOtpRepository _otpRepository;

    /// <summary>
    /// The email service used to send OTP codes to users.
    /// </summary>
    private readonly IEmailService _emailService;

    /// <summary>
    /// The transaction manager used to handle database transactions.
    /// </summary>
    private readonly ITransactionManager _transactionManager;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="OtpService"/> class with the specified repository, email service, and transaction manager.
    /// </summary>
    /// <param name="otpRepository">The OTP repository.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="transactionManager">The transaction manager.</param>
    public OtpService(IOtpRepository otpRepository, IEmailService emailService, ITransactionManager transactionManager)
    {
        _otpRepository = otpRepository;
        _emailService = emailService;
        _transactionManager = transactionManager;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<Result<DateTime>> SendOtpCodeAsync(UserOtpDetailsDto user)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync();

        try
        {
            var otpCode = GenerateOtp();

            await _otpRepository.DeleteOtpCodeByUserIdAsync(user.Id);

            var expiresAt = await _otpRepository.AddOtpCodeAsync(otpCode, user.Id);

            await _emailService.SendEmailAsync(user.Email, "Verification code", otpCode);

            await transaction.CommitAsync();

            return Result<DateTime>.Success(expiresAt);
        }
        catch
        {
            return Result<DateTime>.Failure(Errors.Failure("Failed to send OTP code."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CheckOtpCodeAsync(string otpCode, UserOtpDetailsDto user)
    {
        var otpDetails = await _otpRepository.GetOtpCodeDetailsByUserIdAsync(user.Id);

        if (otpDetails is null)
        {
            return Result<bool>.Failure(Errors.NotFound("Otp details"));
        }

        if (otpDetails.ExpiresAt <= DateTime.UtcNow)
        {
            await _otpRepository.DeleteOtpCodeByUserIdAsync(user.Id);

            return Result<bool>.Failure(Errors.Gone("OTP code has expired."));
        }

        if (otpDetails.Code != otpCode)
        {
            return Result<bool>.Failure(Errors.Validation("Invalid OTP code."));
        }

        return Result<bool>.Success(true);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP (One-Time Password).
    /// </summary>
    /// <returns>A string representing a 6-digit OTP.</returns>
    private static string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();

        byte[] bytes = new byte[4];
        rng.GetBytes(bytes);
        int number = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
        int otp = number % 1000000;

        return otp.ToString("D6", CultureInfo.InvariantCulture);
    }

    #endregion
}
