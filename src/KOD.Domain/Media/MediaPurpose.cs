namespace KOD.Domain.Media;

/// <summary>
/// What an uploaded image is used for; decides its size, crop and who may upload it.
/// </summary>
public enum MediaPurpose
{
    /// <summary>A profile photo (square).</summary>
    Avatar,

    /// <summary>An image of a news article: in the text, in a gallery or the cover.</summary>
    News,
}
