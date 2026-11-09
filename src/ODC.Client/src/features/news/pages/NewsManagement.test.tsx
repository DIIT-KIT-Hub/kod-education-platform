import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { makeArticle, makeNewsItem } from "../testing";
import { NewsEditorPage } from "./NewsEditorPage";
import { NewsManagePage } from "./NewsManagePage";

/** The organizations whose news the user manages. */
const organizations = [
    { id: "org-1", name: "Університет" },
    { id: "org-2", name: "Лабораторія" },
];

/**
 * Mocks the organizations of the editor (registered last: it must win over `/api/v1/news/{id}`).
 * @param cabinetWide Whether the user manages cabinet-wide news.
 */
function mockOrganizations(cabinetWide = false) {
    mockApi("get", "/api/v1/news/organizations", { organizations, cabinetWide });
}

/**
 * The query of the last list request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

describe("NewsManagePage", () => {
    it("lists drafts and published articles with their state, organization and edit links", async () => {
        const calls = mockApi(
            "get",
            "/api/v1/news",
            makePage([
                makeNewsItem({ id: "n-1", title: "Опублікована", isPinned: true }),
                makeNewsItem({
                    id: "n-2",
                    title: "Чернетка новини",
                    publishedAt: null,
                    organization: { id: "org-1", name: "Університет" },
                }),
            ]),
        );
        renderPage(<NewsManagePage />, { path: "/news/manage" });

        const published = (await screen.findByRole("link", { name: /^Опублікована/ })).closest("tr") as HTMLElement;
        expect(within(published).getByText("Опубліковано")).toBeInTheDocument();
        expect(within(published).getByText("Закріплено")).toBeInTheDocument();
        expect(within(published).getByText("Увесь кабінет")).toBeInTheDocument();
        const draft = screen.getByRole("link", { name: /^Чернетка новини/ }).closest("tr") as HTMLElement;
        expect(within(draft).getByText("Чернетка")).toBeInTheDocument();
        expect(within(draft).getByText("Університет")).toBeInTheDocument();
        expect(within(draft).getByRole("link", { name: "Редагувати" })).toHaveAttribute("href", "/news/manage/n-2");
        expect(lastQuery(calls)).toMatchObject({ IncludeDrafts: "true", Page: "1" });
        expect(screen.getByRole("link", { name: "Нова публікація" })).toHaveAttribute("href", "/news/manage/new");
    });

    it("searches the articles from the first page", async () => {
        const calls = mockApi("get", "/api/v1/news", makePage([makeNewsItem()]));
        const page = renderPage(<NewsManagePage />, { path: "/news/manage", state: { view: "page=3" } });
        await screen.findByRole("searchbox");

        await page.user.type(screen.getByRole("searchbox"), "корпус{Enter}");

        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ Search: "корпус", Page: "1" }));
    });

    it("says when there is nothing yet", async () => {
        mockApi("get", "/api/v1/news", makePage([]));
        renderPage(<NewsManagePage />, { path: "/news/manage" });

        expect(await screen.findByText("Публікацій ще немає")).toBeInTheDocument();
    });

    it("shows the server's message when the list fails", async () => {
        mockApi("get", "/api/v1/news", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<NewsManagePage />, { path: "/news/manage" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});

describe("NewsEditorPage", () => {
    /**
     * Renders the editor of a new article.
     * @param cabinetWide Whether the user manages cabinet-wide news.
     * @returns The rendered page.
     */
    function renderNew(cabinetWide = false) {
        mockOrganizations(cabinetWide);
        return renderPage(<NewsEditorPage />, { path: "/news/manage/new" });
    }

    /**
     * Renders the editor of an existing article.
     * @param article The article.
     * @returns The rendered page.
     */
    function renderExisting(article = makeArticle({ canManage: true })) {
        mockApi("get", "/api/v1/news/{id}", article);
        mockOrganizations();
        return renderPage(<NewsEditorPage />, { path: "/news/manage/:id", url: `/news/manage/${article.id}` });
    }

    it("starts a new article published in the user's first organization", async () => {
        renderNew();

        expect(await screen.findByRole("heading", { name: "Нова публікація" })).toBeInTheDocument();
        expect(screen.getByRole("combobox", { name: "Організація" })).toHaveTextContent("Університет");
        expect(screen.getByRole("checkbox", { name: /Опубліковано/ })).toBeChecked();
        expect(screen.getByRole("checkbox", { name: /Дозволити коментарі/ })).toBeChecked();
        expect(screen.getByRole("checkbox", { name: /Закріпити зверху/ })).not.toBeChecked();
        expect(screen.queryByRole("button", { name: "Видалити" })).not.toBeInTheDocument();
    });

    it("offers the whole cabinet only to cabinet-wide editors", async () => {
        const page = renderNew(true);

        const organization = await screen.findByRole("combobox", { name: "Організація" });
        expect(organization).toHaveTextContent("Увесь кабінет (усі організації)");
        await page.user.click(organization);
        const listbox = document.getElementById(organization.getAttribute("aria-controls") ?? "") as HTMLElement;
        expect(
            within(listbox)
                .getAllByRole("option")
                .map((option) => option.textContent),
        ).toEqual(["Увесь кабінет (усі організації)", "Університет", "Лабораторія"]);
    });

    it("asks for a title, a summary and a text", async () => {
        const created = mockApi("post", "/api/v1/news", { id: "never" });
        const page = renderNew();

        await page.user.click(await screen.findByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Вкажіть заголовок.")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть анонс.")).toBeInTheDocument();
        expect(screen.getByText("Напишіть текст новини.")).toBeInTheDocument();
        expect(created).toHaveLength(0);
    });

    it("creates the article with every setting and returns to the list", async () => {
        const created = mockApi("post", "/api/v1/news", { id: "n-9" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Заголовок/ }), "  Новий корпус  ");
        await page.user.type(screen.getByRole("textbox", { name: /Анонс/ }), "Відкриття у п'ятницю");
        await page.user.click(screen.getByRole("textbox", { name: "Текст новини" }));
        await page.user.keyboard("Запрошуємо всіх");
        await page.user.click(screen.getByRole("checkbox", { name: /Закріпити зверху/ }));
        await page.user.click(screen.getByRole("checkbox", { name: /Опубліковано/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await navigatedTo()).toBe("/news/manage");
        expect(created[0]?.body).toMatchObject({
            title: "Новий корпус",
            summary: "Відкриття у п'ятницю",
            category: "News",
            isPinned: true,
            publish: false,
            removeCover: false,
            organizationId: "org-1",
            allowComments: true,
        });
        expect((created[0]?.body as { body: string }).body).toContain("Запрошуємо всіх");
        expect(created[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("edits an existing article with its current values", async () => {
        const updated = mockApi("put", "/api/v1/news/{id}");
        const article = makeArticle({
            canManage: true,
            isPinned: true,
            allowComments: false,
            organization: { id: "org-2", name: "Лабораторія" },
        });
        const page = renderExisting(article);

        const title = await screen.findByRole("textbox", { name: /Заголовок/ });
        expect(screen.getByRole("heading", { name: "Редагування публікації" })).toBeInTheDocument();
        expect(title).toHaveValue(article.title);
        expect(screen.getByRole("combobox", { name: "Організація" })).toHaveTextContent("Лабораторія");
        expect(screen.getByRole("checkbox", { name: /Дозволити коментарі/ })).not.toBeChecked();
        await page.user.clear(title);
        await page.user.type(title, "Оновлений заголовок");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await navigatedTo()).toBe("/news/manage");
        expect(updated[0]?.path).toBe(`/api/v1/news/${article.id}`);
        expect(updated[0]?.body).toMatchObject({
            title: "Оновлений заголовок",
            organizationId: "org-2",
            isPinned: true,
            allowComments: false,
            publish: true,
        });
    });

    it("deletes an article after confirmation", async () => {
        const removed = mockApi("delete", "/api/v1/news/{id}");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Видалити" }));
        const dialog = (await screen.findByRole("heading", { name: "Видалити публікацію остаточно?" })).closest("dialog") as HTMLElement;
        await page.user.click(within(dialog).getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        expect(await screen.findByText("Публікацію видалено")).toBeInTheDocument();
        expect(await navigatedTo()).toBe("/news/manage");
        expect(removed).toHaveLength(1);
    });

    it("reports a refused save and stays on the form", async () => {
        mockApi("put", "/api/v1/news/{id}", () => problem(403, "forbidden", "Ви не керуєте новинами цієї організації."));
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Ви не керуєте новинами цієї організації.")).toBeInTheDocument();
        expect(screen.getByRole("heading", { name: "Редагування публікації" })).toBeInTheDocument();
    });
});
