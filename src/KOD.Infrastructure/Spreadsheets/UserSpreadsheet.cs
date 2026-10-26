using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using KOD.Application.Features.Users;
using KOD.Domain.Localization;

namespace KOD.Infrastructure.Spreadsheets;

/// <summary>
/// Reads and writes user spreadsheets in XLSX (ClosedXML) and CSV (CsvHelper) formats.
/// </summary>
internal sealed class UserSpreadsheet : IUserSpreadsheet
{
    /// <summary>
    /// The maximum number of data rows accepted in one import.
    /// </summary>
    private const int MaxRows = 10_000;

    /// <summary>
    /// The column headers written to files, in order.
    /// </summary>
    private static readonly string[] Headers =
        ["Email", "Прізвище", "Ім'я", "По батькові", "Телефон", "Код підрозділу", "Код ролі", "Посада", "Статус"];

    /// <summary>
    /// The credential file headers.
    /// </summary>
    private static readonly string[] CredentialHeaders = ["Email", "ПІБ", "Тимчасовий пароль"];

    /// <summary>
    /// Maps accepted header spellings (Ukrainian and English) to column indexes of <see cref="Headers"/>.
    /// </summary>
    private static readonly FrozenDictionary<string, int> HeaderAliases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["email"] = 0, ["e-mail"] = 0, ["пошта"] = 0, ["електронна пошта"] = 0,
        ["прізвище"] = 1, ["last name"] = 1, ["last_name"] = 1, ["lastname"] = 1,
        ["ім'я"] = 2, ["імʼя"] = 2, ["ім’я"] = 2, ["first name"] = 2, ["first_name"] = 2, ["firstname"] = 2,
        ["по батькові"] = 3, ["middle name"] = 3, ["middle_name"] = 3, ["middlename"] = 3,
        ["телефон"] = 4, ["phone"] = 4,
        ["код підрозділу"] = 5, ["підрозділ"] = 5, ["unit"] = 5, ["unit_code"] = 5, ["unit code"] = 5,
        ["код ролі"] = 6, ["роль"] = 6, ["role"] = 6, ["role_code"] = 6, ["role code"] = 6,
        ["посада"] = 7, ["position"] = 7, ["title"] = 7,
        ["статус"] = 8, ["status"] = 8,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyList<UserSheetRow> Read(Stream content, SpreadsheetFormat format)
    {
        ArgumentNullException.ThrowIfNull(content);
        var table = format == SpreadsheetFormat.Csv ? ReadCsv(content) : ReadXlsx(content);
        if (table.Count == 0)
        {
            return [];
        }

        var map = MapHeaders(table[0]);
        var rows = new List<UserSheetRow>(table.Count - 1);
        for (var i = 1; i < table.Count; i++)
        {
            var cells = table[i];
            string? Cell(int column) => map.TryGetValue(column, out var index) && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index])
                ? cells[index].Trim()
                : null;

            if (cells.TrueForAll(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new UserSheetRow(i + 1, Cell(0) ?? string.Empty, Cell(1) ?? string.Empty, Cell(2) ?? string.Empty, Cell(3), Cell(4), Cell(5), Cell(6), Cell(7), Cell(8)));
            if (rows.Count > MaxRows)
            {
                throw new InvalidDataException(Texts.T("Файл містить більше {0} рядків.", MaxRows));
            }
        }

        return rows;
    }

    /// <inheritdoc />
    public byte[] Write(IEnumerable<UserSheetRow> rows, SpreadsheetFormat format)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var data = rows.Select(static r => new[] { r.Email, r.LastName, r.FirstName, r.MiddleName, r.PhoneNumber, r.UnitCode, r.RoleCode, r.PositionTitle, r.Status });
        return format == SpreadsheetFormat.Csv ? WriteCsv(Headers, data) : WriteXlsx("Користувачі", Headers, data);
    }

    /// <inheritdoc />
    public byte[] WriteCredentials(IEnumerable<UserCredentials> credentials, SpreadsheetFormat format)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var data = credentials.Select(static c => new string?[] { c.Email, c.FullName, c.TemporaryPassword });
        return format == SpreadsheetFormat.Csv ? WriteCsv(CredentialHeaders, data) : WriteXlsx("Облікові дані", CredentialHeaders, data);
    }

    /// <summary>
    /// Maps known columns to their index in the header row.
    /// </summary>
    /// <param name="header">The header row.</param>
    /// <returns>Known column number → cell index.</returns>
    private static Dictionary<int, int> MapHeaders(List<string> header)
    {
        var map = new Dictionary<int, int>();
        for (var i = 0; i < header.Count; i++)
        {
            if (HeaderAliases.TryGetValue(header[i].Trim().Trim('﻿'), out var column))
            {
                map.TryAdd(column, i);
            }
        }

        string[] required = ["Email", "Прізвище", "Ім'я"];
        var missing = required.Where((_, column) => !map.ContainsKey(column)).ToList();
        return missing.Count == 0
            ? map
            : throw new InvalidDataException(Texts.T("У файлі відсутні обов'язкові стовпці: {0}.", string.Join(", ", missing)));
    }

    /// <summary>
    /// Reads all cells of the first worksheet as text.
    /// </summary>
    /// <param name="content">The workbook stream.</param>
    /// <returns>The rows.</returns>
    private static List<List<string>> ReadXlsx(Stream content)
    {
        try
        {
            using var workbook = new XLWorkbook(content);
            var sheet = workbook.Worksheets.First();
            var used = sheet.RangeUsed();
            if (used is null)
            {
                return [];
            }

            var columns = used.ColumnCount();
            return [.. used.Rows().Select(row => Enumerable.Range(1, columns).Select(c => row.Cell(c).GetFormattedString()).ToList())];
        }
        catch (Exception exception) when (exception is not InvalidDataException)
        {
            throw new InvalidDataException(Texts.T("Не вдалося прочитати файл XLSX. Переконайтеся, що це коректна книга Excel."), exception);
        }
    }

    /// <summary>
    /// Reads all records of a CSV file (delimiter is detected automatically).
    /// </summary>
    /// <param name="content">The CSV stream.</param>
    /// <returns>The rows.</returns>
    private static List<List<string>> ReadCsv(Stream content)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            HasHeaderRecord = false,
            BadDataFound = null,
            MissingFieldFound = null,
        };

        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, configuration);
        var rows = new List<List<string>>();
        while (csv.Read())
        {
            rows.Add([.. csv.Parser.Record ?? []]);
            if (rows.Count > MaxRows + 1)
            {
                throw new InvalidDataException(Texts.T("Файл містить більше {0} рядків.", MaxRows));
            }
        }

        return rows;
    }

    /// <summary>
    /// Writes a CSV file with a UTF-8 BOM and «;» delimiter (opens correctly in Excel with Ukrainian regional settings).
    /// </summary>
    /// <param name="headers">The header row.</param>
    /// <param name="rows">The data rows.</param>
    /// <returns>The file content.</returns>
    private static byte[] WriteCsv(string[] headers, IEnumerable<string?[]> rows)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" }))
        {
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }

            csv.NextRecord();
            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    csv.WriteField(Neutralize(cell));
                }

                csv.NextRecord();
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Writes a formatted XLSX workbook with a frozen, filterable header row.
    /// </summary>
    /// <param name="sheetName">The worksheet name.</param>
    /// <param name="headers">The header row.</param>
    /// <param name="rows">The data rows.</param>
    /// <returns>The file content.</returns>
    private static byte[] WriteXlsx(string sheetName, string[] headers, IEnumerable<string?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                // Cells are written as text to keep phone numbers and codes intact.
                sheet.Cell(r, c + 1).SetValue(row[c] ?? string.Empty);
            }

            r++;
        }

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.Black;
        headerRow.Style.Font.FontColor = XLColor.White;
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents(1, Math.Min(r, 200));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Prevents CSV formula injection by prefixing values that spreadsheet apps would evaluate.
    /// </summary>
    /// <param name="value">The cell value.</param>
    /// <returns>The safe value.</returns>
    private static string? Neutralize(string? value)
        => value is { Length: > 0 } && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' && !value.StartsWith("+380", StringComparison.Ordinal)
            ? "'" + value
            : value;
}
