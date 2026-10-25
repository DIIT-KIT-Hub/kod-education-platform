namespace KOD.Infrastructure.Files;

/// <summary>
/// File storage settings (section <c>Storage</c>).
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Gets or sets the root folder of private files (documents, signatures). Relative paths are resolved against the content root.
    /// It must not be inside <c>wwwroot</c>.
    /// </summary>
    public string PrivateRoot { get; set; } = "App_Data/storage";

    /// <summary>
    /// Gets or sets the folder inside <c>wwwroot</c> for public uploads (news covers).
    /// </summary>
    public string PublicUploadsFolder { get; set; } = "uploads";
}
