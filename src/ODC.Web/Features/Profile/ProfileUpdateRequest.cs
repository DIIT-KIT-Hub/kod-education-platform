using System.ComponentModel.DataAnnotations;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Features.Profile;

/// <summary>
/// Body of <c>PATCH /api/v1/profile</c>. The name and the e-mail are optional: only users who administer their whole
/// account may change them (omitted values stay as they are).
/// </summary>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic (empty to remove it).</param>
/// <param name="Email">The e-mail address (login).</param>
/// <param name="PhoneNumber">The contact phone number.</param>
public sealed record ProfileUpdateRequest(
    [MaxLength(User.NamePartMaxLength)] string? LastName,
    [MaxLength(User.NamePartMaxLength)] string? FirstName,
    [MaxLength(User.NamePartMaxLength)] string? MiddleName,
    [EmailAddress][MaxLength(User.EmailMaxLength)] string? Email,
    [Phone][MaxLength(32)] string? PhoneNumber);
