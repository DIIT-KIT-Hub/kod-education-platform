using System.Net.Http.Headers;
using NetVips;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// Files the tests upload.
/// </summary>
public static class TestFiles
{
    /// <summary>
    /// A square grey PNG large enough for a profile photo.
    /// </summary>
    /// <returns>The bytes.</returns>
    public static byte[] Png()
    {
        using var black = Image.Black(320, 320);
        using var grey = black + 128;
        using var image = grey.Cast(Enums.BandFormat.Uchar);
        return image.WriteToBuffer(".png");
    }

    /// <summary>
    /// A file part of a multipart form.
    /// </summary>
    /// <param name="bytes">The content.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>The part.</returns>
    public static ByteArrayContent Part(byte[] bytes, string mediaType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return content;
    }
}
