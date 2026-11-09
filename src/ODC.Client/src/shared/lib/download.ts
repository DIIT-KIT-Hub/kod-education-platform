import { unwrap } from "@/shared/api/problem";

/**
 * Reads the file name of a `Content-Disposition` header (`filename*` in UTF-8 first, then `filename`).
 * @param header The header value.
 * @returns The name, or null when the header has none.
 */
export function fileNameOf(header: string | null): string | null {
    if (!header) {
        return null;
    }

    const encoded = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header)?.[1];
    if (encoded) {
        try {
            return decodeURIComponent(encoded.trim());
        } catch {
            // A broken escape: fall back to the plain name.
        }
    }

    const plain = /filename\s*=\s*("([^"]*)"|[^;]+)/i.exec(header);
    const name = (plain?.[2] ?? plain?.[1])?.trim();
    return name || null;
}

/**
 * Downloads a file of the API and hands it to the browser to save, so the caller knows when it is ready (a plain
 * link gives no sign of the server still preparing it). Fails with an {@link ApiError} when the server refuses.
 * @param url The API address (same origin; the sign-in cookies go along).
 * @param fallbackName The name to save under when the server gives none.
 * @returns The name the file was saved under.
 */
export async function downloadFile(url: string, fallbackName: string): Promise<string> {
    const response = await fetch(url, { credentials: "same-origin" });
    if (!response.ok) {
        const body: unknown = await response.json().catch(() => undefined);
        await unwrap(Promise.resolve({ error: body, response }));
    }

    const name = fileNameOf(response.headers.get("Content-Disposition")) ?? fallbackName;
    const href = URL.createObjectURL(await response.blob());
    const link = document.createElement("a");
    link.href = href;
    link.download = name;
    link.hidden = true;
    document.body.append(link);
    link.click();
    link.remove();
    // The browser reads the object address when the download starts; let it go a little later.
    setTimeout(() => URL.revokeObjectURL(href), 60_000);
    return name;
}
