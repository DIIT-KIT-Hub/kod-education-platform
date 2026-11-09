import { fireEvent, screen, within } from "@testing-library/react";
import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { renderPage } from "@/test/render";
import { ArticleBody } from "./ArticleBody";

/** A text with an image, a gallery of two tiles and an image inside a link. */
const html = [
    "<h2>Подія</h2>",
    '<p>Вступ <a href="https://example.org">посилання</a></p>',
    '<img src="/media/a" alt="Перше фото">',
    '<div data-type="gallery"><img src="/media/b/thumb" data-full="/media/b" alt="Друге"><img src="/media/c/thumb" data-full="/media/c" alt="Третє"></div>',
    '<p><a href="/media/d"><img src="/media/d" alt="У посиланні"></a></p>',
].join("");

/**
 * Renders the text of an article.
 * @param body The HTML.
 * @returns The rendered page.
 */
function renderBody(body = html) {
    return renderPage(<ArticleBody html={body} />);
}

/** The browser's own currentSrc of images (happy-dom never loads images). */
const ownCurrentSrc = Object.getOwnPropertyDescriptor(HTMLImageElement.prototype, "currentSrc");

/**
 * Makes images report what a browser reports: the address of a loaded image, nothing for a lazy image that has not
 * been scrolled into view yet (the server marks every image of an article `loading="lazy"`).
 */
function emulateLoadedImages(): void {
    Object.defineProperty(HTMLImageElement.prototype, "currentSrc", {
        configurable: true,
        get(this: HTMLImageElement) {
            return this.getAttribute("loading") === "lazy" ? "" : this.src;
        },
    });
}

/**
 * The open viewer.
 * @returns The dialog element.
 */
function viewer(): HTMLElement {
    return screen.getByRole("dialog", { name: "Перегляд фото" });
}

/**
 * The image the viewer shows.
 * @returns The image.
 */
function shownImage(): HTMLImageElement {
    return viewer().querySelector("img") as HTMLImageElement;
}

describe("ArticleBody", () => {
    beforeAll(emulateLoadedImages);
    afterAll(() => {
        if (ownCurrentSrc) {
            Object.defineProperty(HTMLImageElement.prototype, "currentSrc", ownCurrentSrc);
        } else {
            delete (HTMLImageElement.prototype as { currentSrc?: string }).currentSrc;
        }
    });

    it("inserts the sanitized HTML of the server as it is", () => {
        renderBody();

        expect(screen.getByRole("heading", { name: "Подія" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "посилання" })).toHaveAttribute("href", "https://example.org");
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("opens a clicked image in the full-screen viewer and goes through every image of the text", async () => {
        const { user } = renderBody();

        await user.click(screen.getByAltText("Перше фото"));

        expect(viewer()).toHaveAttribute("open");
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/a");
        expect(shownImage()).toHaveAttribute("alt", "Перше фото");
        expect(within(viewer()).getByText("1 / 4")).toBeInTheDocument();

        // Gallery tiles show their full rendition.
        await user.click(within(viewer()).getByRole("button", { name: "Наступне фото" }));
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/b");
        expect(within(viewer()).getByText("2 / 4")).toBeInTheDocument();

        // Going back from the first wraps to the last.
        await user.click(within(viewer()).getByRole("button", { name: "Попереднє фото" }));
        await user.click(within(viewer()).getByRole("button", { name: "Попереднє фото" }));
        expect(within(viewer()).getByText("4 / 4")).toBeInTheDocument();
        await user.click(within(viewer()).getByRole("button", { name: "Наступне фото" }));
        expect(within(viewer()).getByText("1 / 4")).toBeInTheDocument();
    });

    it("opens a gallery tile at its full rendition", async () => {
        const { user } = renderBody();

        await user.click(screen.getByAltText("Третє"));

        expect(shownImage()).toHaveProperty("src", "https://localhost/media/c");
        expect(within(viewer()).getByText("3 / 4")).toBeInTheDocument();
    });

    it("leaves an image inside a link to the link", async () => {
        const { user } = renderBody();

        await user.click(screen.getByAltText("У посиланні"));

        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("ignores clicks on the text", async () => {
        const { user } = renderBody();

        await user.click(screen.getByRole("heading", { name: "Подія" }));

        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("moves with the arrow keys", async () => {
        const { user } = renderBody();
        await user.click(screen.getByAltText("Перше фото"));

        fireEvent.keyDown(viewer(), { key: "ArrowRight" });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/b");
        fireEvent.keyDown(viewer(), { key: "ArrowLeft" });
        fireEvent.keyDown(viewer(), { key: "ArrowLeft" });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/d");
        fireEvent.keyDown(viewer(), { key: "Enter" });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/d");
    });

    it("moves with a swipe longer than 50 pixels", async () => {
        const { user } = renderBody();
        await user.click(screen.getByAltText("Перше фото"));

        fireEvent.pointerDown(shownImage(), { clientX: 300 });
        fireEvent.pointerUp(shownImage(), { clientX: 200 });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/b");

        fireEvent.pointerDown(shownImage(), { clientX: 200 });
        fireEvent.pointerUp(shownImage(), { clientX: 300 });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/a");

        fireEvent.pointerDown(shownImage(), { clientX: 200 });
        fireEvent.pointerUp(shownImage(), { clientX: 230 });
        expect(shownImage()).toHaveProperty("src", "https://localhost/media/a");
    });

    it("closes with the close button", async () => {
        const { user } = renderBody();
        await user.click(screen.getByAltText("Перше фото"));

        await user.click(within(viewer()).getByRole("button", { name: "Закрити" }));

        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("closes on a click on the backdrop but not on the image", async () => {
        const { user } = renderBody();
        await user.click(screen.getByAltText("Перше фото"));

        await user.click(shownImage());
        expect(viewer()).toBeInTheDocument();

        await user.click(viewer());
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("opens a lazy in-text image that has not loaded yet by its address", async () => {
        const { user } = renderBody('<img src="/media/a" alt="Перше фото"><p>Довгий текст</p><img src="/media/far" alt="Далеке" loading="lazy">');

        await user.click(screen.getByAltText("Перше фото"));
        await user.click(within(viewer()).getByRole("button", { name: "Наступне фото" }));

        expect(shownImage()).toHaveProperty("src", "https://localhost/media/far");
    });

    it("shows a single image without the counter and the arrows", async () => {
        const { user } = renderBody('<p>Текст</p><img src="/media/only" alt="Одне">');

        await user.click(screen.getByAltText("Одне"));

        expect(shownImage()).toHaveProperty("src", "https://localhost/media/only");
        expect(within(viewer()).queryByText("1 / 1")).not.toBeInTheDocument();
        expect(within(viewer()).queryByRole("button", { name: "Наступне фото" })).not.toBeInTheDocument();
        expect(within(viewer()).queryByRole("button", { name: "Попереднє фото" })).not.toBeInTheDocument();
    });
});
