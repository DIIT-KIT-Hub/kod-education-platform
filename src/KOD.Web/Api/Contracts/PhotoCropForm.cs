using System.Globalization;
using KOD.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KOD.Web.Api.Contracts;

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
    /// Gets the crop square, or <see langword="null"/> when none was sent (the centre is used).
    /// </summary>
    [BindNever]
    public ImageCrop? Crop
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
