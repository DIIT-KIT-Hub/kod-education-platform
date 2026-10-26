using System.Text;
using KOD.Application.Abstractions;
using KOD.Domain.Media;
using KOD.Infrastructure.Media;
using Microsoft.Extensions.Logging.Abstractions;
using NetVips;

namespace KOD.UnitTests.Infrastructure;

public sealed class ImageProcessorTests : IDisposable
{
    /// <summary>
    /// The processor under test (real libvips).
    /// </summary>
    private readonly VipsImageProcessor _processor = new(NullLogger<VipsImageProcessor>.Instance);

    public void Dispose() => _processor.Dispose();

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".gif")]
    [InlineData(".avif")]
    public async Task Converts_photos_to_avif_within_the_size_budget(string format)
    {
        var result = await _processor.ProcessAsync(Photo(2400, 1600, format), MediaPurpose.News, null, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.InRange(Math.Max(result.Width, result.Height), 320, 1600);
        Assert.InRange(result.Main.Length, 1, 100 * 1024);
        Assert.True(IsAvif(result.Main));
        Assert.True(IsAvif(result.Thumbnail));
    }

    [Fact]
    public async Task Avatars_are_square_and_follow_the_chosen_crop()
    {
        var result = await _processor.ProcessAsync(Photo(3000, 2000, ".jpg"), MediaPurpose.Avatar, new ImageCrop(0.5, 0.25, 0.25, 0.375), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(320, result.Width);
        Assert.Equal(320, result.Height);
        Assert.InRange(result.Main.Length, 1, 40 * 1024);
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\"/></svg>")]
    [InlineData("not an image at all")]
    public async Task Rejects_anything_but_photo_formats(string content)
    {
        var result = await _processor.ProcessAsync(Encoding.UTF8.GetBytes(content), MediaPurpose.News, null, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Creates a test photo: blurred colour noise compresses about as badly as a real photo.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="format">The file extension of the encoding.</param>
    /// <returns>The encoded image.</returns>
    private static byte[] Photo(int width, int height, string format)
    {
        using var red = Image.Gaussnoise(width, height, mean: 128, sigma: 60);
        using var green = Image.Gaussnoise(width, height, mean: 110, sigma: 60);
        using var blue = Image.Gaussnoise(width, height, mean: 90, sigma: 60);
        using var bands = red.Bandjoin(green, blue);
        using var soft = bands.Gaussblur(2);
        using var photo = soft.Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);
        return photo.WriteToBuffer(format);
    }

    /// <summary>
    /// Determines whether bytes are an AVIF file (ISO BMFF <c>ftyp</c> box with the <c>avif</c> brand).
    /// </summary>
    /// <param name="data">The bytes.</param>
    /// <returns><see langword="true"/> for AVIF.</returns>
    private static bool IsAvif(byte[] data) => data.Length > 12 && Encoding.ASCII.GetString(data, 4, 8) is "ftypavif" or "ftypavis";
}
