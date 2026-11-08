using ODC.Web.Shared.Files;

namespace ODC.Web.Endpoints;

/// <summary>
/// Opens uploaded form files for a handler and closes them afterwards.
/// </summary>
public static class FormFiles
{
    /// <summary>
    /// Opens the files, runs an action with them and closes them.
    /// </summary>
    /// <typeparam name="T">The result of the action.</typeparam>
    /// <param name="files">The uploaded files (none when the form has no files).</param>
    /// <param name="action">The action.</param>
    /// <returns>What the action returned.</returns>
    public static async Task<T> WithUploadsAsync<T>(IEnumerable<IFormFile>? files, Func<IReadOnlyList<FileUpload>, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var streams = new List<Stream>();
        try
        {
            var uploads = new List<FileUpload>();
            foreach (var file in files ?? [])
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new FileUpload(stream, file.FileName, file.ContentType, file.Length));
            }

            return await action(uploads);
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }
}
