namespace KOD.Web.Hosting;

/// <summary>
/// Where the single-page application (src/KOD.Client) is served from (section <c>Spa</c>).
/// </summary>
internal sealed class SpaOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Spa";

    /// <summary>
    /// The URL path of the application. While the Blazor pages are being replaced it lives under <c>/app</c>.
    /// </summary>
    public const string BasePath = "/app";

    /// <summary>
    /// Gets or sets the folder with the built application, relative to the content root: the published copy in
    /// production, the output of <c>pnpm dev</c> / <c>pnpm build</c> (<c>../KOD.Client/dist</c>) in development.
    /// </summary>
    public string Root { get; set; } = "wwwroot/app";
}
