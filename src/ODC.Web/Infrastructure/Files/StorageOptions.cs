using System.Globalization;

namespace ODC.Web.Infrastructure.Files;

/// <summary>
/// File storage settings (section <c>Storage</c>) and the layout of the private folder: a folder per organization
/// (<c>orgs/{id}</c>) and one of the platform (<c>platform</c>), every file encrypted with the key of its owner.
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
    /// Gets the absolute root folder.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The folder.</returns>
    public static string RootOf(StorageOptions options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.PrivateRoot));
    }

    /// <summary>
    /// Gets the folder of an organization's files (built from the identifier alone).
    /// </summary>
    /// <param name="root">The absolute root folder.</param>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The folder.</returns>
    public static string OrganizationFolder(string root, Guid organizationId)
        => Path.Combine(root, "orgs", organizationId.ToString("N", CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets the folder of the platform's files.
    /// </summary>
    /// <param name="root">The absolute root folder.</param>
    /// <returns>The folder.</returns>
    public static string PlatformFolder(string root) => Path.Combine(root, "platform");
}
