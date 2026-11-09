import { onTestFinished, vi } from "vitest";

/**
 * A file the page handed to the browser to save.
 */
export interface SavedFile {
    /** The name it is saved under. */
    readonly name: string;
    /** The (object) address of its content. */
    readonly href: string;
}

/**
 * Records the files a page saves through a temporary link (happy-dom would try to open the link) until the test ends.
 * @returns The saved files, in order.
 */
export function captureDownloads(): SavedFile[] {
    const saved: SavedFile[] = [];
    const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function save(this: HTMLAnchorElement) {
        saved.push({ name: this.download, href: this.href });
    });
    onTestFinished(() => click.mockRestore());
    return saved;
}
