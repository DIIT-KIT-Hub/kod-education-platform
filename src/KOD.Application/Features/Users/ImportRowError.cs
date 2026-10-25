namespace KOD.Application.Features.Users;

/// <summary>
/// A validation problem of an imported row.
/// </summary>
/// <param name="Row">The 1-based row number.</param>
/// <param name="Email">The e-mail of the row.</param>
/// <param name="Message">The user-facing message.</param>
public sealed record ImportRowError(int Row, string Email, string Message);
