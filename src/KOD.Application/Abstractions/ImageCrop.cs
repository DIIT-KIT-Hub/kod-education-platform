namespace KOD.Application.Abstractions;

/// <summary>
/// A rectangle chosen by the user, in fractions (0–1) of the width and height of the upright image, so it does not
/// depend on the resolution the browser previewed.
/// </summary>
/// <param name="Left">The left edge.</param>
/// <param name="Top">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct ImageCrop(double Left, double Top, double Width, double Height)
{
    /// <summary>
    /// Gets a value indicating whether the rectangle lies inside the image and is not empty.
    /// </summary>
    public bool IsValid => Left >= 0 && Top >= 0 && Width > 0.01 && Height > 0.01 && Left + Width <= 1.0001 && Top + Height <= 1.0001;
}
