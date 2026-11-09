namespace ODC.Web.Shared.Domain.Users;

/// <summary>
/// The part of an account that organizations keep a copy of (<see cref="Member"/>).
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic.</param>
/// <param name="PhoneNumber">The contact phone number.</param>
/// <param name="PhotoId">The profile photo.</param>
/// <param name="IsActive">Whether the account may sign in.</param>
/// <param name="AwaitsActivation">Whether the account waits for its owner to activate it.</param>
/// <param name="LastSignInAt">The last sign-in (lists sort by it).</param>
public sealed record MemberProfile(
    Guid Id,
    string Email,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? PhoneNumber,
    Guid? PhotoId,
    bool IsActive,
    bool AwaitsActivation,
    DateTimeOffset? LastSignInAt)
{
    /// <summary>
    /// Takes the profile of an account.
    /// </summary>
    /// <param name="user">The account.</param>
    /// <returns>The profile.</returns>
    public static MemberProfile Of(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new MemberProfile(user.Id, user.Email ?? string.Empty, user.LastName, user.FirstName, user.MiddleName, user.PhoneNumber, user.PhotoId, user.IsActive, user.AwaitsActivation, user.LastSignInAt);
    }
}
