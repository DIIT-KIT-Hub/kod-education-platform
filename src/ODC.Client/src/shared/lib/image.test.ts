import { afterEach, describe, expect, it, vi } from "vitest";
import { imageAccept, shrinkImage } from "./image";

/**
 * A file of a given size and type.
 * @param name The name.
 * @param type The media type.
 * @param size The size in bytes.
 * @returns The file.
 */
function file(name: string, type: string, size: number): File {
    return new File([new Uint8Array(size)], name, { type });
}

/**
 * Replaces the canvas the function draws on with a stand-in that returns the given blob.
 * @param blob The blob `toBlob` hands back (null when encoding fails).
 * @returns The stand-in canvas and its drawing context.
 */
function fakeCanvas(blob: Blob | null) {
    const context = { drawImage: vi.fn() };
    const canvas = {
        width: 0,
        height: 0,
        getContext: vi.fn(() => context),
        toBlob: vi.fn((callback: (value: Blob | null) => void, type: string, quality: number) => {
            void type;
            void quality;
            callback(blob);
        }),
    };
    const create = document.createElement.bind(document);
    vi.spyOn(document, "createElement").mockImplementation((tag: string) =>
        tag === "canvas" ? (canvas as unknown as HTMLCanvasElement) : create(tag),
    );
    return { canvas, context };
}

/**
 * Replaces createImageBitmap with one that decodes to a bitmap of the given size.
 * @param width The width.
 * @param height The height.
 * @returns The bitmap.
 */
function fakeBitmap(width: number, height: number) {
    const bitmap = { width, height, close: vi.fn() };
    vi.stubGlobal(
        "createImageBitmap",
        vi.fn(() => Promise.resolve(bitmap)),
    );
    return bitmap;
}

afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

describe("shrinkImage", () => {
    const large = 700 * 1024;

    it("returns a file that is not an image unchanged", async () => {
        const pdf = file("doc.pdf", "application/pdf", large);
        expect(await shrinkImage(pdf, 1600)).toBe(pdf);
    });

    it("returns a GIF unchanged (animations would be lost)", async () => {
        const gif = file("cat.gif", "image/gif", large);
        expect(await shrinkImage(gif, 1600)).toBe(gif);
    });

    it("returns a small photo unchanged", async () => {
        const photo = file("small.jpg", "image/jpeg", 600 * 1024 - 1);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });

    it("returns the photo unchanged when the browser cannot decode images", async () => {
        vi.stubGlobal("createImageBitmap", undefined);
        const photo = file("big.jpg", "image/jpeg", large);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });

    it("draws a large photo scaled to the longest side and returns a WebP copy", async () => {
        const bitmap = fakeBitmap(4000, 2000);
        const { canvas, context } = fakeCanvas(new Blob([new Uint8Array(1000)], { type: "image/webp" }));
        const photo = file("holiday.photo.jpg", "image/jpeg", large);

        const result = await shrinkImage(photo, 1600);

        expect(canvas.width).toBe(1600);
        expect(canvas.height).toBe(800);
        expect(context.drawImage).toHaveBeenCalledWith(bitmap, 0, 0, 1600, 800);
        expect(bitmap.close).toHaveBeenCalled();
        expect(canvas.toBlob).toHaveBeenCalledWith(expect.any(Function), "image/webp", 0.92);
        expect(result).not.toBe(photo);
        expect(result.name).toBe("holiday.photo.webp");
        expect(result.type).toBe("image/webp");
        expect(result.size).toBe(1000);
    });

    it("never enlarges a photo smaller than the longest side", async () => {
        fakeBitmap(1000, 1200);
        const { canvas } = fakeCanvas(new Blob([new Uint8Array(10)], { type: "image/webp" }));

        await shrinkImage(file("tall.png", "image/png", large), 1600);

        expect(canvas.width).toBe(1000);
        expect(canvas.height).toBe(1200);
    });

    it("keeps the original when encoding fails", async () => {
        fakeBitmap(4000, 2000);
        fakeCanvas(null);
        const photo = file("big.jpg", "image/jpeg", large);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });

    it("keeps the original when the browser cannot encode WebP (falls back to PNG)", async () => {
        fakeBitmap(4000, 2000);
        fakeCanvas(new Blob([new Uint8Array(10)], { type: "image/png" }));
        const photo = file("big.jpg", "image/jpeg", large);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });

    it("keeps the original when the copy is not smaller", async () => {
        fakeBitmap(4000, 2000);
        fakeCanvas(new Blob([new Uint8Array(large)], { type: "image/webp" }));
        const photo = file("big.jpg", "image/jpeg", large);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });

    it("keeps the original when decoding fails", async () => {
        vi.stubGlobal(
            "createImageBitmap",
            vi.fn(() => Promise.reject(new Error("broken"))),
        );
        const photo = file("broken.heic", "image/heic", large);
        expect(await shrinkImage(photo, 1600)).toBe(photo);
    });
});

describe("imageAccept", () => {
    it("lists the photo types", () => {
        expect(imageAccept.split(",")).toEqual(["image/jpeg", "image/png", "image/webp", "image/avif", "image/gif"]);
    });
});
