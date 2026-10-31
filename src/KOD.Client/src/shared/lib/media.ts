/**
 * The URL of an uploaded image (served to signed-in users with an immutable cache).
 * @param id The image identifier.
 * @param thumbnail Whether to load the small rendition.
 * @returns The URL.
 */
export function mediaUrl(id: string, thumbnail = false): string {
    return thumbnail ? `/media/${id}/thumb` : `/media/${id}`;
}
