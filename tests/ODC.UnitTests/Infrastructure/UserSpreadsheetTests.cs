using System.Text;
using ODC.Web.Features.Admin.Users;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The user import and export files: CSV (any common separator, UTF-8 with or without a byte order mark, badly quoted
/// fields recovered, formula injection neutralized on export) and XLSX, with Ukrainian or English headers.
/// </summary>
public sealed class UserSpreadsheetTests
{
    /// <summary>
    /// The spreadsheet service.
    /// </summary>
    private readonly UserSpreadsheet _spreadsheet = new();

    [Theory]
    [InlineData(SpreadsheetFormat.Csv)]
    [InlineData(SpreadsheetFormat.Xlsx)]
    public void An_export_reads_back_as_the_same_rows(SpreadsheetFormat format)
    {
        UserSheetRow[] rows =
        [
            new(0, "ivan@example.com", "Петренко", "Іван", "Олегович", "+380501234567", "SALES", "head", "Начальник відділу; продажі", "активний"),
            new(0, "olena@example.com", "Коваль", "Олена", null, null, "HR", "employee", "Інспектор \"А\"", "заблокований"),
        ];

        var read = _spreadsheet.Read(new MemoryStream(_spreadsheet.Write(rows, format)), format);

        Assert.Equal(2, read.Count);
        Assert.Equal(rows[0] with { RowNumber = 2 }, read[0]);
        Assert.Equal(rows[1] with { RowNumber = 3 }, read[1]);
    }

    [Fact]
    public void A_CSV_export_opens_in_Excel_with_a_byte_order_mark_and_semicolons()
    {
        var bytes = _spreadsheet.Write([new(0, "a@example.com", "Бондар", "Олег", null, null, null, null, null, null)], SpreadsheetFormat.Csv);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Email;Прізвище;Ім'я;По батькові;Телефон;Код підрозділу;Код ролі;Посада;Статус", lines[0]);
        Assert.Equal("a@example.com;Бондар;Олег;;;;;;", lines[1]);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://evil\")", "'=HYPERLINK(\"http://evil\")")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-2+3", "'-2+3")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("+380501234567", "+380501234567")]
    [InlineData("Звичайний текст", "Звичайний текст")]
    public void A_CSV_export_neutralizes_cells_that_spreadsheets_would_evaluate(string value, string expected)
    {
        var bytes = _spreadsheet.Write([new(0, "a@example.com", value, "Олег", null, null, null, null, null, null)], SpreadsheetFormat.Csv);

        var read = Assert.Single(_spreadsheet.Read(new MemoryStream(bytes), SpreadsheetFormat.Csv));
        Assert.Equal(expected, read.LastName);
    }

    [Theory]
    [InlineData(",")]
    [InlineData(";")]
    [InlineData("\t")]
    public void A_CSV_import_detects_the_separator(string separator)
    {
        var text = string.Join(separator, "email", "last name", "first name", "role") + "\n" + string.Join(separator, "a@example.com", "Бондар", "Олег", "employee") + "\n";

        var row = Assert.Single(_spreadsheet.Read(Utf8(text, bom: false), SpreadsheetFormat.Csv));

        Assert.Equal(("a@example.com", "Бондар", "Олег", "employee"), (row.Email, row.LastName, row.FirstName, row.RoleCode));
    }

    [Fact]
    public void A_CSV_import_reads_quoted_fields_trims_cells_and_skips_empty_lines()
    {
        const string text = "Email;Прізвище;Ім'я;Посада\r\n\"a@example.com\";\" Бондар \";Олег;\"Начальник; \"\"старший\"\"\"\r\n;;;\r\nb@example.com;Коваль;Олена;\r\n";

        var rows = _spreadsheet.Read(Utf8(text, bom: true), SpreadsheetFormat.Csv);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Бондар", rows[0].LastName);
        Assert.Equal("Начальник; \"старший\"", rows[0].PositionTitle);
        Assert.Null(rows[1].PositionTitle);
        Assert.Equal(4, rows[1].RowNumber);
    }

    [Fact]
    public void A_CSV_import_reads_UTF_16_with_its_byte_order_mark()
    {
        const string text = "Email;Прізвище;Ім'я\r\na@example.com;Ґудзь;Їжак\r\n";
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray();

        var row = Assert.Single(_spreadsheet.Read(new MemoryStream(bytes), SpreadsheetFormat.Csv));

        Assert.Equal(("Ґудзь", "Їжак"), (row.LastName, row.FirstName));
    }

    [Fact]
    public void A_file_without_the_required_columns_is_refused_naming_them()
    {
        var exception = Assert.Throws<InvalidDataException>(() => _spreadsheet.Read(Utf8("Email;Телефон\r\na@example.com;1\r\n", bom: false), SpreadsheetFormat.Csv));

        Assert.Equal("У файлі відсутні обов'язкові стовпці: Прізвище, Ім'я.", exception.Message);
    }

    [Fact]
    public void An_empty_file_has_no_rows()
    {
        Assert.Empty(_spreadsheet.Read(new MemoryStream(), SpreadsheetFormat.Csv));
    }

    [Fact]
    public void A_file_with_more_than_ten_thousand_rows_is_refused()
    {
        var text = new StringBuilder("Email;Прізвище;Ім'я\n");
        for (var i = 0; i < 10_002; i++)
        {
            text.Append('u').Append(i).Append("@example.com;Т;К\n");
        }

        var exception = Assert.Throws<InvalidDataException>(() => _spreadsheet.Read(Utf8(text.ToString(), bom: false), SpreadsheetFormat.Csv));

        Assert.Equal("Файл містить більше 10000 рядків.", exception.Message);
    }

    [Fact]
    public void A_file_that_is_not_a_workbook_is_refused_as_XLSX()
    {
        var exception = Assert.Throws<InvalidDataException>(() => _spreadsheet.Read(Utf8("not a workbook", bom: false), SpreadsheetFormat.Xlsx));

        Assert.Equal("Не вдалося прочитати файл XLSX. Переконайтеся, що це коректна книга Excel.", exception.Message);
    }

    /// <summary>
    /// A UTF-8 stream of a text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="bom">Whether to start with a byte order mark.</param>
    /// <returns>The stream.</returns>
    private static MemoryStream Utf8(string text, bool bom)
        => new([.. bom ? Encoding.UTF8.GetPreamble() : [], .. Encoding.UTF8.GetBytes(text)]);
}
