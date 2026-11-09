import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage, makeUser } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { Permissions } from "@/shared/session/session";
import { makeNewsItem } from "@/features/news/testing";
import { NewsPage } from "./NewsPage";

/**
 * Renders the news list.
 * @param options The user and the history state of the view.
 * @returns The rendered page.
 */
function renderNews(options: { user?: ReturnType<typeof makeUser>; state?: unknown; url?: string } = {}) {
    return renderPage(<NewsPage />, { path: "/news", url: options.url ?? "/news", ...options });
}

describe("NewsPage", () => {
    it("shows skeletons while loading, then the cards with their facts", async () => {
        const calls = mockApi(
            "get",
            "/api/v1/news",
            makePage(
                [
                    makeNewsItem({
                        isPinned: true,
                        organization: { id: "o1", name: "Головне управління" },
                        coverImageUrl: "/media/c1",
                        coverThumbnailUrl: "/media/c1/thumb",
                    }),
                    makeNewsItem({
                        id: "10000000-0000-7000-8000-000000000002",
                        slug: "training",
                        title: "Курси підвищення кваліфікації",
                        category: "Training",
                        coverImageUrl: "/media/c2",
                        coverThumbnailUrl: null,
                    }),
                ],
                { pageSize: 9 },
            ),
        );
        const { container } = renderNews();

        expect(screen.queryByRole("link", { name: /Відкриття нового корпусу/ })).not.toBeInTheDocument();
        const first = await screen.findByRole("link", { name: /Відкриття нового корпусу/ });
        expect(first).toHaveAttribute("href", "/news/new-building");
        expect(within(first).getByText("Закріплено")).toBeInTheDocument();
        expect(within(first).getByText("Урочисте відкриття відбудеться у п'ятницю.")).toBeInTheDocument();
        expect(within(first).getByText(/Головне управління/)).toBeInTheDocument();
        expect(within(first).getByText(/5 листопада 2026/)).toBeInTheDocument();
        expect(within(first).getByTitle("Переглядів")).toHaveTextContent("42");
        expect(within(first).getByTitle("Реакцій")).toHaveTextContent("7");
        expect(within(first).getByTitle("Коментарів")).toHaveTextContent("3");
        expect(within(first).getByText("Коваленко Марія Андріївна")).toBeInTheDocument();

        const second = screen.getByRole("link", { name: /Курси підвищення кваліфікації/ });
        expect(within(second).getByText("Навчання та розвиток")).toBeInTheDocument();
        expect(within(second).queryByText("Закріплено")).not.toBeInTheDocument();

        // The thumbnail of the cover when there is one, the cover itself otherwise.
        const covers = Array.from(container.querySelectorAll("img")).map((image) => image.getAttribute("src"));
        expect(covers).toEqual(expect.arrayContaining(["/media/c1/thumb", "/media/c2"]));

        expect(calls[0]?.query.get("Page")).toBe("1");
        expect(calls[0]?.query.get("PageSize")).toBe("9");
        expect(calls[0]?.query.has("Category")).toBe(false);
        expect(calls[0]?.query.has("IncludeDrafts")).toBe(false);
    });

    it("shows a placeholder instead of a missing cover", async () => {
        mockApi("get", "/api/v1/news", makePage([makeNewsItem()]));
        renderNews();

        const card = await screen.findByRole("link", { name: /Відкриття нового корпусу/ });
        expect(card.querySelector("img")).toBeNull();
    });

    it("offers the management only to news managers", async () => {
        mockApi("get", "/api/v1/news", makePage([]));
        renderNews();

        expect(await screen.findByRole("link", { name: "Керування" })).toHaveAttribute("href", "/news/manage");
        expect(screen.getByRole("link", { name: "Нова публікація" })).toHaveAttribute("href", "/news/manage/new");
    });

    it("hides the management from readers", async () => {
        mockApi("get", "/api/v1/news", makePage([]));
        renderNews({ user: makeUser({ permissions: [Permissions.newsView] }) });

        expect(await screen.findByText("Новин не знайдено")).toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Керування" })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Нова публікація" })).not.toBeInTheDocument();
    });

    it("says when nothing was found", async () => {
        mockApi("get", "/api/v1/news", makePage([]));
        renderNews();

        expect(await screen.findByText("Новин не знайдено")).toBeInTheDocument();
        expect(screen.getByText("Спробуйте іншу категорію або пошуковий запит.")).toBeInTheDocument();
        expect(screen.queryByRole("navigation", { name: "Сторінки" })).not.toBeInTheDocument();
    });

    it("shows the server's message when the list cannot be loaded", async () => {
        mockApi("get", "/api/v1/news", () => problem(500, "server_error", "Сервер недоступний."));
        renderNews();

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });

    it("falls back to a generic message for an error without details", async () => {
        mockApi("get", "/api/v1/news", () => problem(500, "server_error", ""));
        renderNews();

        expect(await screen.findByText("Не вдалося завантажити новини.")).toBeInTheDocument();
    });

    it("filters by the category tab", async () => {
        const calls = mockApi("get", "/api/v1/news", makePage([makeNewsItem()]));
        const { user } = renderNews();

        const tabs = await screen.findByRole("navigation", { name: "Категорії" });
        expect(within(tabs).getByRole("link", { name: "Усі" })).toHaveAttribute("aria-current", "page");
        expect(within(tabs).getAllByRole("link").map((link) => link.textContent)).toEqual([
            "Усі",
            "Новини",
            "Оголошення",
            "Подія",
            "Навчання та розвиток",
            "Проєкти та досягнення",
        ]);

        await user.click(within(tabs).getByRole("link", { name: "Подія" }));

        await waitFor(() => expect(calls.at(-1)?.query.get("Category")).toBe("Event"));
        expect(within(tabs).getByRole("link", { name: "Подія" })).toHaveAttribute("aria-current", "page");
        expect(within(tabs).getByRole("link", { name: "Усі" })).not.toHaveAttribute("aria-current");
    });

    it("ignores an unknown category of the view", async () => {
        const calls = mockApi("get", "/api/v1/news", makePage([]));
        renderNews({ state: { view: "category=Nonsense" } });

        await screen.findByText("Новин не знайдено");
        expect(calls[0]?.query.has("Category")).toBe(false);
    });

    it("searches and goes back to the first page", async () => {
        const calls = mockApi("get", "/api/v1/news", makePage([makeNewsItem()], { page: 2, pageSize: 9, totalCount: 20 }));
        const { user } = renderNews({ state: { view: "page=2" } });

        const search = await screen.findByRole("searchbox", { name: "Пошук у новинах" });
        expect(calls[0]?.query.get("Page")).toBe("2");
        await user.type(search, "  корпус {Enter}");

        await waitFor(() => expect(calls.at(-1)?.query.get("Search")).toBe("корпус"));
        expect(calls.at(-1)?.query.get("Page")).toBe("1");

        await user.clear(search);
        await user.keyboard("{Enter}");
        await waitFor(() => expect(calls.at(-1)?.query.has("Search")).toBe(false));
    });

    it("pages through the articles and changes the page size", async () => {
        const calls = mockApi("get", "/api/v1/news", (request) => {
            const page = Number(request.query.get("Page"));
            const pageSize = Number(request.query.get("PageSize"));
            return Response.json(makePage([makeNewsItem({ title: `Сторінка ${page}` })], { page, pageSize, totalCount: 30 }));
        });
        const { user } = renderNews();

        expect(await screen.findByText("Сторінка 1")).toBeInTheDocument();
        const pages = screen.getByRole("navigation", { name: "Сторінки" });
        expect(within(pages).getByText("1–9 з 30")).toBeInTheDocument();

        await user.click(within(pages).getByRole("link", { name: "Наступна сторінка" }));
        expect(await screen.findByText("Сторінка 2")).toBeInTheDocument();
        expect(calls.at(-1)?.query.get("Page")).toBe("2");

        await user.click(screen.getByRole("combobox", { name: "На сторінці" }));
        expect(screen.getAllByRole("option").map((option) => option.textContent)).toEqual(["9", "18", "36", "72"]);
        await user.click(screen.getByRole("option", { name: "18" }));

        await waitFor(() => expect(calls.at(-1)?.query.get("PageSize")).toBe("18"));
        expect(calls.at(-1)?.query.get("Page")).toBe("1");
    });

    it("reads the page from an old address with a query string", async () => {
        const calls = mockApi("get", "/api/v1/news", makePage([]));
        renderNews({ url: "/news?page=3&category=Announcement" });

        await screen.findByText("Новин не знайдено");
        expect(calls[0]?.query.get("Page")).toBe("3");
        expect(calls[0]?.query.get("Category")).toBe("Announcement");
    });
});
