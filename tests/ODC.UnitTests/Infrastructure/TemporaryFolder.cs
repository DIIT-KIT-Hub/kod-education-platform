using Microsoft.Extensions.Hosting.Internal;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// An empty folder under the system temporary directory, deleted with everything in it at the end of the test. It also
/// serves as the content root of a host environment, so storage services resolve their relative paths inside it.
/// </summary>
internal sealed class TemporaryFolder : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporaryFolder"/> class.
    /// </summary>
    public TemporaryFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"odc-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        Environment = new HostingEnvironment { ContentRootPath = Path, EnvironmentName = "Testing", ApplicationName = "ODC.Web" };
    }

    /// <summary>
    /// Gets the full path of the folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a host environment whose content root is this folder.
    /// </summary>
    public HostingEnvironment Environment { get; }

    /// <summary>
    /// Lists the files under the folder (relative paths with forward slashes).
    /// </summary>
    /// <returns>The files.</returns>
    public IReadOnlyList<string> Files()
        => Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories)
            .Select(f => System.IO.Path.GetRelativePath(Path, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A stream a failed test left open: the system cleans its temporary directory.
        }
    }
}
