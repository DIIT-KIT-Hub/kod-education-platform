using System.Text;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// A file part of a multipart form.
/// </summary>
/// <param name="Field">The form field.</param>
/// <param name="FileName">The file name.</param>
/// <param name="Content">The bytes.</param>
/// <param name="MediaType">The media type.</param>
public sealed record UploadedFile(string Field, string FileName, byte[] Content, string MediaType)
{
    /// <summary>
    /// A plain text file.
    /// </summary>
    /// <param name="field">The form field.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="text">The text.</param>
    /// <returns>The file.</returns>
    public static UploadedFile Text(string field, string fileName, string text) => new(field, fileName, Encoding.UTF8.GetBytes(text), "text/plain");
}
