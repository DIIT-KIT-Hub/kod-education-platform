namespace KOD.Web.Hosting;

/// <summary>
/// Where the application log goes besides the console (configuration section <c>Logs</c>). Levels and per-namespace
/// overrides come from the standard <c>Serilog</c> section.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Logs";

    /// <summary>
    /// Gets or sets the log file path, relative to the content root. A new file starts every day; the date is inserted
    /// before the extension (<c>kod-20261026.log</c>).
    /// </summary>
    public string FilePath { get; set; } = "App_Data/logs/kod-.log";

    /// <summary>
    /// Gets or sets how many daily files are kept.
    /// </summary>
    public int RetainedFiles { get; set; } = 31;

    /// <summary>
    /// Gets or sets the database table of the log (created on first use).
    /// </summary>
    public string DatabaseTable { get; set; } = "logs";

    /// <summary>
    /// Gets or sets how many days log rows are kept in the database.
    /// </summary>
    public int DatabaseRetentionDays { get; set; } = 90;
}
