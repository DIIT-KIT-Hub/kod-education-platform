using System.ComponentModel.DataAnnotations;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>PATCH /api/v1/profile</c>.
/// </summary>
/// <param name="PhoneNumber">The contact phone number.</param>
public sealed record ProfileUpdateRequest([property: Phone][property: MaxLength(32)] string? PhoneNumber);
