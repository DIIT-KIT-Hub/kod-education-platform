namespace KOD.Application.Abstractions;

/// <summary>
/// The public assets of earlier versions (news covers in <c>wwwroot/uploads</c>). New images are uploaded media
/// (<see cref="IMediaStorage"/>); the old files are still served and are deleted when their article drops them.
/// </summary>
public interface IPublicAssetStorage
{
    /// <summary>
    /// Deletes a previously stored asset (other URLs are ignored).
    /// </summary>
    /// <param name="url">The public site-relative URL.</param>
    void Delete(string url);
}
