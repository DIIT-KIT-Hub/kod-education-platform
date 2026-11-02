namespace KOD.Application.Features.Profile;

/// <summary>
/// Changes of the current user's personal data and contacts. The name and the e-mail are applied only for users who
/// administer their whole account (<see cref="ProfileDto.CanEditPersonalData"/>); the others may change the phone only.
/// </summary>
/// <param name="LastName">The last name, or <see langword="null"/> to keep it.</param>
/// <param name="FirstName">The first name, or <see langword="null"/> to keep it.</param>
/// <param name="MiddleName">The patronymic, or <see langword="null"/> to keep it (an empty value removes it).</param>
/// <param name="Email">The e-mail address (login), or <see langword="null"/> to keep it.</param>
/// <param name="PhoneNumber">The phone number.</param>
public sealed record ProfileUpdate(string? LastName, string? FirstName, string? MiddleName, string? Email, string? PhoneNumber);
