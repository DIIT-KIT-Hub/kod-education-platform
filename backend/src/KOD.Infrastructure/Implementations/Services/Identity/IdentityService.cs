using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.DTOs.Users;
using KOD.Application.Exceptions.Statuses;
using KOD.Application.Mappings;
using KOD.Domain.Repositories;

namespace KOD.Infrastructure.Implementations.Services.Identity;

/// <summary>
/// Implements <see cref="IIdentityService"/> for managing user identity operations such as confirmation and OTP retrieval.
/// </summary>
internal sealed class IdentityService : IIdentityService
{
    #region Private fields

    /// <summary>
    /// The repository used to access user identity data.
    /// </summary>
    private readonly IIdentityRepository _identityRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityService"/> class with the specified identity repository.
    /// </summary>
    /// <param name="identityRepository">The identity repository.</param>
    public IdentityService(IIdentityRepository identityRepository) => _identityRepository = identityRepository;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task ConfirmUserAsync(string email, string password)
    {
        var user = await _identityRepository.GetUserByEmailAsync(email);

        if (user is null)
        {
            throw new NotFoundException(nameof(user));
        }

        await _identityRepository.ConfirmUserAsync(user, password);
    }

    /// <inheritdoc />
    public async Task<UserOtpDetailsDto> GetUserOtpDetailsByEmailAsync(string email)
    {
        var userOtpDetails = await _identityRepository.GetUserOtpDetailsByEmailAsync(email);

        if (userOtpDetails is null)
        {
            throw new NotFoundException(nameof(userOtpDetails));
        }

        return userOtpDetails.ToDto();
    }

    #endregion
}
