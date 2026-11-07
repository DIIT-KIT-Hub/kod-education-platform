namespace ODC.Application.Features.Users;

/// <summary>
/// The outcome of a user import (or of its dry run).
/// </summary>
/// <param name="TotalRows">The number of data rows.</param>
/// <param name="UsersToCreate">The number of accounts that are (or would be) created.</param>
/// <param name="UsersToUpdate">The number of accounts that are (or would be) updated.</param>
/// <param name="Applied">Whether the changes were saved.</param>
/// <param name="Errors">The validation problems; the import is applied only when there are none.</param>
/// <param name="InvitationsSent">How many new accounts were sent the invitation to activate them.</param>
public sealed record UserImportReport(
    int TotalRows,
    int UsersToCreate,
    int UsersToUpdate,
    bool Applied,
    IReadOnlyList<ImportRowError> Errors,
    int InvitationsSent);
