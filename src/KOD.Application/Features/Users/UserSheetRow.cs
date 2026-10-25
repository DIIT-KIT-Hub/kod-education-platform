namespace KOD.Application.Features.Users;

/// <summary>
/// A row of the user import/export spreadsheet. A user with several positions spans several rows with the same e-mail.
/// </summary>
/// <param name="RowNumber">The 1-based row number in the source file (0 for exported rows).</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="UnitCode">The unit code.</param>
/// <param name="RoleCode">The role code.</param>
/// <param name="PositionTitle">The official position title.</param>
/// <param name="Status">The account state: «активний» / «заблокований» (or active / blocked).</param>
public sealed record UserSheetRow(
    int RowNumber,
    string Email,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? PhoneNumber,
    string? UnitCode,
    string? RoleCode,
    string? PositionTitle,
    string? Status);
