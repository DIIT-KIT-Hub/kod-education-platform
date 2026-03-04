using System.Globalization;
using System.Security.Cryptography;

using KOD.Application.Abstractions.Persitence.Repositories.Otp;
using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Application.Abstractions.Services.Emails;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Application.DTOs.Otp;
using KOD.Application.Result;

using Microsoft.AspNetCore.Http;

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

    #region Public fields

    /// <inheritdoc />
    public async Task<ApiResult<bool>> RequestOtpCodeAsync(UserOtpDto user)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync();

        var otpCode = GenerateOtp();

        var deleteOtpsResult = await _otpRepository.DeleteAllOtpCodesByUserIdAsync(user.Id);
        if (!deleteOtpsResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(deleteOtpsResult.StatusCode, deleteOtpsResult.Message!);
        }

        var otpAddResult = await _otpRepository.AddOtpCodeAsync(otpCode, user.Id);
        if (!otpAddResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(otpAddResult.StatusCode, otpAddResult.Message!);
        }

        var otpSendResult = await _emailService.SendEmailAsync(user.Email, otpCode);
        if (!otpSendResult.IsSuccess)
        {
            await transaction.RollbackAsync();
            return ApiResult<bool>.Failure(otpSendResult.StatusCode, otpSendResult.Message!);
        }

        await transaction.CommitAsync();
        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> CheckOtpCodeAsync(string otpCode, UserOtpDto user)
    {
        var otpResult = await _otpRepository.GetOtpCodeByUserIdAsync(user.Id);
        if (!otpResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(otpResult.StatusCode, otpResult.Message!);
        }

        if (otpResult.Data!.ExpiresAt <= DateTime.UtcNow)
        {
            var otpDeletionResult = await _otpRepository.DeleteOtpCodeByUserIdAsync(user.Id);
            if (!otpDeletionResult.IsSuccess)
            {
                return ApiResult<bool>.Failure(otpDeletionResult.StatusCode, otpDeletionResult.Message!);
            }

            return ApiResult<bool>.Failure(StatusCodes.Status401Unauthorized, "Otp code has expired.");
        }

        if (otpCode != otpResult.Data!.Code)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status400BadRequest, "Otp code is not valid.");
        }

        return ApiResult<bool>.Success(true);
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
