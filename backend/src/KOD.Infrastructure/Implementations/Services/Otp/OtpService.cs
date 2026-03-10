using System.Globalization;
using System.Security.Cryptography;

using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Application.Abstractions.Services.Emails;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Application.DTOs.Users;
using KOD.Application.Exceptions.Statuses;
using KOD.Domain.Exceptions.Auth;
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
    public async Task RequestOtpCodeAsync(UserOtpDetailsDto user)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync();

        var otpCode = GenerateOtp();

        await _otpRepository.DeleteOtpCodeByUserIdAsync(user.Id);

        await _otpRepository.AddOtpCodeAsync(otpCode, user.Id);

        await _emailService.SendEmailAsync(user.Email, "Verification code", otpCode);

        await transaction.CommitAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CheckOtpCodeAsync(string otpCode, UserOtpDetailsDto user)
    {
        try
        {
            var otp = await _otpRepository.GetOtpCodeDetailsByUserIdAsync(user.Id);

            if (otp is null)
            {
                throw new NotFoundException(nameof(otp));
            }

            return otpCode == otp.Code;
        }
        catch (CredentialsException)
        {
            await _otpRepository.DeleteOtpCodeByUserIdAsync(user.Id);

            throw;
        }
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
