namespace KOD.Application.Features.Users;

/// <summary>
/// Reads and writes user spreadsheets.
/// </summary>
public interface IUserSpreadsheet
{
    /// <summary>
    /// Reads user rows. Columns are matched by header names (Ukrainian or English).
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The rows.</returns>
    /// <exception cref="InvalidDataException">The file is malformed or required columns are missing.</exception>
    IReadOnlyList<UserSheetRow> Read(Stream content, SpreadsheetFormat format);

    /// <summary>
    /// Writes user rows.
    /// </summary>
    /// <param name="rows">The rows.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The file content.</returns>
    byte[] Write(IEnumerable<UserSheetRow> rows, SpreadsheetFormat format);

    /// <summary>
    /// Writes one-time credentials for distribution to new users.
    /// </summary>
    /// <param name="credentials">The credentials.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The file content.</returns>
    byte[] WriteCredentials(IEnumerable<UserCredentials> credentials, SpreadsheetFormat format);
}
