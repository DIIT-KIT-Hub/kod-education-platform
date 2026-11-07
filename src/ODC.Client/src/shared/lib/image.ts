/**
 * Returns a smaller WebP copy of a photo, or the file itself when shrinking would not help. The server still converts
 * every image to AVIF within its size budget; this only saves upload time on slow networks. Small files, animations and
 * formats the browser cannot decode are uploaded unchanged.
 * @param file The chosen file.
 * @param maxSide The longest side of the copy in pixels.
 * @returns The file to upload.
 */
export async function shrinkImage(file: File, maxSide: number): Promise<File> {
    if (!file.type.startsWith("image/") || file.type === "image/gif" || file.size < 600 * 1024 || typeof createImageBitmap !== "function") {
        return file;
    }

    try {
        // createImageBitmap turns the photo upright by its EXIF orientation, as the server does.
        const bitmap = await createImageBitmap(file);
        const scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height));
        const canvas = document.createElement("canvas");
        canvas.width = Math.round(bitmap.width * scale);
        canvas.height = Math.round(bitmap.height * scale);
        canvas.getContext("2d")?.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
        bitmap.close();

        const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/webp", 0.92));
        if (!blob || blob.type !== "image/webp" || blob.size >= file.size) {
            return file;
        }

        return new File([blob], `${file.name.replace(/\.[^.]+$/, "")}.webp`, { type: "image/webp" });
    } catch {
        return file;
    }
}

/**
 * The image types accepted for photos (the server checks the content again).
 */
export const imageAccept = "image/jpeg,image/png,image/webp,image/avif,image/gif";
