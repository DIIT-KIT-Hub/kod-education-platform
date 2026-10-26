using System.Collections.Frozen;
using KOD.Application.Abstractions;
using KOD.Domain.Media;
using Microsoft.Extensions.Logging;
using NetVips;

namespace KOD.Infrastructure.Media;

/// <summary>
/// Converts photos to AVIF with libvips (NetVips). Only the loaders of common photo formats are allowed; the image is
/// decoded with shrink-on-load, turned upright by its EXIF orientation, converted to sRGB and saved without metadata.
/// The quality is lowered (and then the size) until the main rendition fits the budget of its purpose. A semaphore
/// bounds how many images are encoded at once, so a burst of uploads cannot exhaust the CPU or memory.
/// </summary>
internal sealed partial class VipsImageProcessor : IImageProcessor, IDisposable
{
    /// <summary>
    /// The largest accepted image (pixels), a guard against decompression bombs.
    /// </summary>
    private const long MaxPixels = 80_000_000;

    /// <summary>
    /// The libvips loader classes of the accepted formats (<see cref="Image.FindLoadBuffer(byte[])"/> returns class names);
    /// PNG and GIF have two possible implementations depending on the libvips build.
    /// </summary>
    private static readonly FrozenSet<string> AllowedLoaders = FrozenSet.Create(
        StringComparer.Ordinal,
        "VipsForeignLoadJpegBuffer",
        "VipsForeignLoadPngBuffer",
        "VipsForeignLoadSpngBuffer",
        "VipsForeignLoadWebpBuffer",
        "VipsForeignLoadHeifBuffer",
        "VipsForeignLoadNsgifBuffer",
        "VipsForeignLoadGifBuffer");

    /// <summary>
    /// The renditions of each purpose.
    /// </summary>
    private static readonly FrozenDictionary<MediaPurpose, Preset> Presets = new Dictionary<MediaPurpose, Preset>
    {
        // Square photos: 320 px for the profile page, 96 px for lists (≈ 10 KB and 2 KB).
        [MediaPurpose.Avatar] = new(MainSize: 320, ThumbnailSize: 96, Square: true, MaxBytes: 40 * 1024),

        // Text and gallery images: up to 1600 px within 100 KB, thumbnails for gallery tiles and cards.
        [MediaPurpose.News] = new(MainSize: 1600, ThumbnailSize: 640, Square: false, MaxBytes: 100 * 1024),
    }.ToFrozenDictionary();

    /// <summary>
    /// Limits concurrent encodings.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(Math.Max(1, Environment.ProcessorCount / 2));

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<VipsImageProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VipsImageProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public VipsImageProcessor(ILogger<VipsImageProcessor> logger)
    {
        _logger = logger;

        // Loaders libvips marks as untrusted (e.g. through ImageMagick) are never used for uploads.
        global::NetVips.NetVips.BlockUntrusted = true;

        // Uploads are never processed twice: the operation cache would only hold memory.
        Cache.Max = 0;
        Cache.MaxMem = 0;
        Cache.MaxFiles = 0;
    }

    /// <inheritdoc />
    public async Task<ProcessedImage?> ProcessAsync(ReadOnlyMemory<byte> source, MediaPurpose purpose, ImageCrop? crop, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var data = source.ToArray();
            return await Task.Run(() => Process(data, Presets[purpose], crop), cancellationToken);
        }
        catch (VipsException exception)
        {
            LogRejected(exception.Message);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Produces the renditions.
    /// </summary>
    /// <param name="data">The uploaded file.</param>
    /// <param name="preset">The renditions to produce.</param>
    /// <param name="crop">The part chosen by the user.</param>
    /// <returns>The renditions, or <see langword="null"/> when the file is not an accepted image.</returns>
    private static ProcessedImage? Process(byte[] data, Preset preset, ImageCrop? crop)
    {
        var loader = Image.FindLoadBuffer(data);
        if (loader is null || !AllowedLoaders.Contains(loader))
        {
            return null;
        }

        using (var header = Image.NewFromBuffer(data, access: Enums.Access.Sequential))
        {
            if ((long)header.Width * header.Height > MaxPixels)
            {
                return null;
            }
        }

        // A crop is applied to an upright working copy; without one, shrink-on-load gives the main rendition directly.
        using var main = crop is { } area ? Crop(data, area, preset) : Fit(Image.ThumbnailBuffer(data, preset.MainSize, height: preset.MainSize, size: Enums.Size.Down, crop: preset.Square ? Enums.Interesting.Centre : null));
        using var thumbnailSource = main.ThumbnailImage(preset.ThumbnailSize, height: preset.ThumbnailSize, size: Enums.Size.Down, crop: preset.Square ? Enums.Interesting.Centre : null);
        var (bytes, width, height) = EncodeWithinBudget(main, preset.MaxBytes);
        var thumbnail = thumbnailSource.HeifsaveBuffer(q: 50, compression: Enums.ForeignHeifCompression.Av1, effort: 3, keep: Enums.ForeignKeep.None);
        return new ProcessedImage(bytes, thumbnail, width, height);
    }

    /// <summary>
    /// Cuts the chosen part out of an upright copy and resizes it.
    /// </summary>
    /// <param name="data">The uploaded file.</param>
    /// <param name="area">The part in fractions of the upright image.</param>
    /// <param name="preset">The renditions to produce.</param>
    /// <returns>The resized part in memory.</returns>
    private static Image Crop(byte[] data, ImageCrop area, Preset preset)
    {
        // Shrink-on-load to a working size that still leaves the chosen part sharp enough for the main rendition.
        var working = preset.MainSize * 4;
        using var upright = Image.ThumbnailBuffer(data, working, height: working, size: Enums.Size.Down);
        var left = (int)Math.Clamp(Math.Round(area.Left * upright.Width), 0, upright.Width - 1);
        var top = (int)Math.Clamp(Math.Round(area.Top * upright.Height), 0, upright.Height - 1);
        var width = (int)Math.Clamp(Math.Round(area.Width * upright.Width), 1, upright.Width - left);
        var height = (int)Math.Clamp(Math.Round(area.Height * upright.Height), 1, upright.Height - top);
        using var part = upright.ExtractArea(left, top, width, height);
        using var resized = part.ThumbnailImage(preset.MainSize, height: preset.MainSize, size: Enums.Size.Down, crop: preset.Square ? Enums.Interesting.Centre : null);
        return Fit(resized.Copy());
    }

    /// <summary>
    /// Converts an image to 8-bit sRGB in memory, so it can be encoded several times.
    /// </summary>
    /// <param name="image">The image (disposed).</param>
    /// <returns>The converted image.</returns>
    private static Image Fit(Image image)
    {
        using (image)
        {
            using var srgb = image.Interpretation is Enums.Interpretation.Srgb or Enums.Interpretation.Bw ? image.Copy() : image.Colourspace(Enums.Interpretation.Srgb);
            using var bytes = srgb.Format == Enums.BandFormat.Uchar ? srgb.Copy() : srgb.Cast(Enums.BandFormat.Uchar);
            return bytes.CopyMemory();
        }
    }

    /// <summary>
    /// Encodes the main rendition, lowering the quality and then the dimensions until it fits the budget.
    /// </summary>
    /// <param name="image">The image in memory.</param>
    /// <param name="maxBytes">The size budget.</param>
    /// <returns>The AVIF bytes and their dimensions.</returns>
    private static (byte[] Bytes, int Width, int Height) EncodeWithinBudget(Image image, int maxBytes)
    {
        var current = image;
        try
        {
            while (true)
            {
                byte[] bytes = [];
                foreach (var quality in (int[])[62, 54, 46, 38, 30])
                {
                    bytes = current.HeifsaveBuffer(q: quality, compression: Enums.ForeignHeifCompression.Av1, effort: 3, keep: Enums.ForeignKeep.None);
                    if (bytes.Length <= maxBytes)
                    {
                        return (bytes, current.Width, current.Height);
                    }
                }

                // Still too large at the lowest acceptable quality: fewer pixels.
                if (Math.Max(current.Width, current.Height) <= 320)
                {
                    return (bytes, current.Width, current.Height);
                }

                var smaller = current.Resize(0.8).CopyMemory();
                if (!ReferenceEquals(current, image))
                {
                    current.Dispose();
                }

                current = smaller;
            }
        }
        finally
        {
            if (!ReferenceEquals(current, image))
            {
                current.Dispose();
            }
        }
    }

    /// <summary>
    /// Logs a file that libvips could not process.
    /// </summary>
    /// <param name="reason">The libvips error.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "An uploaded image was rejected: {Reason}")]
    private partial void LogRejected(string reason);

    /// <summary>
    /// The renditions of a purpose.
    /// </summary>
    /// <param name="MainSize">The longest side of the main rendition.</param>
    /// <param name="ThumbnailSize">The longest side of the thumbnail.</param>
    /// <param name="Square">Whether both renditions are cut to a square.</param>
    /// <param name="MaxBytes">The size budget of the main rendition.</param>
    private sealed record Preset(int MainSize, int ThumbnailSize, bool Square, int MaxBytes);
}
