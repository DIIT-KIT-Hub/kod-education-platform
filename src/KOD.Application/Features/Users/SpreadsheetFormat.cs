namespace KOD.Application.Features.Users;

/// <summary>
/// Supported spreadsheet formats for import and export.
/// </summary>
public enum SpreadsheetFormat
{
    /// <summary>Office Open XML workbook (*.xlsx).</summary>
    Xlsx = 0,

    /// <summary>UTF-8 comma-separated values with BOM (*.csv).</summary>
    Csv = 1,
}
