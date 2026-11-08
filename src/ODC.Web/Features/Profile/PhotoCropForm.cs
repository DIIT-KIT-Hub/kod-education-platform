using System.Globalization;
using ODC.Web.Shared.Media;

namespace ODC.Web.Features.Profile;

/// <summary>
/// The square of a photo chosen in the cropper, as fractions of the image (invariant culture).
/// </summary>
public sealed class PhotoCropForm
{
    /// <summary>
    /// Gets or sets the left edge.
    /// </summary>
    public string? CropLeft { get; set; }

    /// <summary>
    /// Gets or sets the top edge.
    /// </summary>
    public string? CropTop { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public string? CropWidth { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public string? CropHeight { get; set; }

    /// <summary>
    /// Builds the crop square (a method, so it stays out of the form schema).
    /// </summary>
    /// <returns>The square, or <see langword="null"/> when none was sent (the centre is used).</returns>
    public ImageCrop? ToCrop()
        => Parse(CropLeft) is { } left && Parse(CropTop) is { } top && Parse(CropWidth) is { } width && Parse(CropHeight) is { } height
            ? new ImageCrop(left, top, width, height)
            : null;

    /// <summary>
    /// Parses a fraction.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The number or <see langword="null"/>.</returns>
    private static double? Parse(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) ? number : null;
}
