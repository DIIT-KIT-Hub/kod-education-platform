import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage, makeUser, type Schemas } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { DocumentsPage } from "./DocumentsPage";

/**
 * A row of the registry.
 * @param overrides Changed fields.
 * @returns The row.
 */
function makeItem(overrides: Partial<Schemas["DocumentListItem"]> = {}): Schemas["DocumentListItem"] {
    return {
        id: "doc-1",
        registrationNumber: "2026/0001",
        title: "Про затвердження графіка відпусток",
        kind: "Order",
        status: "InSigning",
        authorName: "Коваленко Марія Андріївна",
        unitName: "Відділ кадрів",
        createdAt: "2026-11-01T09:30:00Z",
        signedCount: 1,
        signersCount: 3,
        awaitingMe: false,
        authorPhotoId: null,
        ...overrides,
    };
}

/**
 * Mocks the registry with one page of rows.
 * @param items The rows.
 * @param paging The paging of the answer.
 * @returns The recorded calls.
 */
function mockList(items: Schemas["DocumentListItem"][] = [makeItem()], paging: { page?: number; pageSize?: number; totalCount?: number } = {}) {
    return mockApi("get", "/api/v1/documents", makePage(items, paging));
}

/**
 * Renders the registry.
 * @param options The user, the address and the history state.
 * @returns The rendered page.
 */
function renderList(options: { user?: Schemas["CurrentUserResponse"]; url?: string; state?: unknown } = {}) {
    return renderPage(<DocumentsPage />, { path: "/documents", url: options.url ?? "/documents", user: options.user, state: options.state });
}

/**
 * The query of the last registry request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

describe("DocumentsPage", () => {
    it("opens the documents awaiting my signature on the first page by default", async () => {
        const calls = mockList();
        renderList();

        expect(await screen.findByRole("link", { name: "Про затвердження графіка відпусток" })).toBeInTheDocument();
        expect(lastQuery(calls)).toEqual({ Scope: "AwaitingMe", Page: "1", PageSize: "20", Descending: "false" });
        expect(screen.getByRole("heading", { name: "Документи" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Очікують мого підпису/ })).toHaveAttribute("aria-current", "page");
    });

    it("shows a skeleton while the registry loads", async () => {
        mockApi("get", "/api/v1/documents", () => new Promise(() => undefined));
        renderList();

        expect(await screen.findByRole("status", { name: "Завантаження" })).toBeInTheDocument();
    });

    it("renders every column of a row", async () => {
        mockList([makeItem()]);
        renderList();

        const link = await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(link).toHaveAttribute("href", "/documents/doc-1");
        const row = link.closest("tr");
        expect(row).not.toBeNull();
        const cells = within(row as HTMLElement);
        expect(cells.getByText("Наказ · Відділ кадрів")).toBeInTheDocument();
        expect(cells.getByText("2026/0001")).toBeInTheDocument();
        expect(cells.getByText("На підписанні")).toBeInTheDocument();
        expect(cells.getByText("1 / 3")).toBeInTheDocument();
        expect(cells.getByText("Коваленко Марія Андріївна")).toBeInTheDocument();
        expect(cells.getByText("01.11.2026, 11:30")).toBeInTheDocument();
        expect(cells.getByRole("link", { name: "Відкрити" })).toHaveAttribute("href", "/documents/doc-1");
        expect(cells.queryByText("Ваш підпис")).not.toBeInTheDocument();
    });

    it("marks a document awaiting my signature and offers to sign it", async () => {
        mockList([makeItem({ awaitingMe: true })]);
        renderList();

        const link = await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        const row = within(link.closest("tr") as HTMLElement);
        expect(row.getByText("Ваш підпис")).toBeInTheDocument();
        expect(row.getByRole("link", { name: "Підписати" })).toHaveAttribute("href", "/documents/doc-1");
    });

    it.each([
        ["Draft", "Чернетка"],
        ["Signed", "Підписано"],
        ["Rejected", "Відхилено"],
        ["Archived", "В архіві"],
    ] as const)("names the state %s", async (status, text) => {
        mockList([makeItem({ status })]);
        renderList();

        const link = await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(within(link.closest("tr") as HTMLElement).getByText(text)).toBeInTheDocument();
    });

    it("offers a new document only to users who may create documents", async () => {
        mockList();
        renderList();

        expect(await screen.findByRole("link", { name: "Новий документ" })).toHaveAttribute("href", "/documents/new");
    });

    it("hides the new document action without documents.create", async () => {
        mockList();
        renderList({ user: makeUser({ permissions: ["documents.view"] }) });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(screen.queryByRole("link", { name: "Новий документ" })).not.toBeInTheDocument();
    });

    it.each([
        ["Створені мною", "Authored"],
        ["Я в маршруті", "Involved"],
        ["Підписані мною", "SignedByMe"],
        ["Усі доступні", "Accessible"],
        ["Очікують мого підпису", "AwaitingMe"],
    ] as const)("switches to the section «%s»", async (name, scope) => {
        const calls = mockList();
        const { user, router } = renderList({ state: { view: "scope=Involved&status=Draft&page=3" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("link", { name: new RegExp(name) }));

        await waitFor(() => expect(lastQuery(calls).Scope).toBe(scope));
        // A section starts over: filters and paging are dropped.
        expect(lastQuery(calls)).toEqual({ Scope: scope, Page: "1", PageSize: "20", Descending: "false" });
        expect(screen.getByRole("link", { name: new RegExp(name) })).toHaveAttribute("aria-current", "page");
        expect(router.state.location.pathname).toBe("/documents");
        expect(router.state.location.state).toMatchObject({ view: `scope=${scope}` });
    });

    it("restores the view from the history state", async () => {
        const calls = mockList();
        renderList({
            state: { view: "scope=Authored&status=Draft&kind=Memo&search=графік&page=2&pageSize=50&sortBy=title&descending=true" },
        });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(lastQuery(calls)).toEqual({
            Scope: "Authored",
            Status: "Draft",
            Kind: "Memo",
            Search: "графік",
            Page: "2",
            PageSize: "50",
            SortBy: "title",
            Descending: "true",
        });
        expect(screen.getByRole("searchbox", { name: "Назва або реєстраційний номер" })).toHaveValue("графік");
        expect(screen.getByRole("combobox", { name: "Статус" })).toHaveTextContent("Чернетка");
        expect(screen.getByRole("combobox", { name: "Вид документа" })).toHaveTextContent("Службова записка");
        expect(screen.getByRole("link", { name: /Створені мною/ })).toHaveAttribute("aria-current", "page");
    });

    it("moves a query string of the address into the history state", async () => {
        const calls = mockList();
        const { router } = renderList({ url: "/documents?scope=SignedByMe" });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await waitFor(() => expect(router.state.location.search).toBe(""));
        expect(router.state.location.state).toMatchObject({ view: "scope=SignedByMe" });
        expect(lastQuery(calls).Scope).toBe("SignedByMe");
    });

    it("ignores unknown or invalid values of the view", async () => {
        const calls = mockList();
        renderList({ state: { view: "scope=Everything&status=Lost&kind=Poem&page=-2&pageSize=abc" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(lastQuery(calls)).toEqual({ Scope: "AwaitingMe", Page: "1", PageSize: "20", Descending: "false" });
    });

    it("searches on Enter and returns to the first page", async () => {
        const calls = mockList();
        const { user, router } = renderList({ state: { view: "scope=Accessible&page=4" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.type(screen.getByRole("searchbox", { name: "Назва або реєстраційний номер" }), "  2026/0001 {Enter}");

        await waitFor(() => expect(lastQuery(calls).Search).toBe("2026/0001"));
        expect(lastQuery(calls)).toMatchObject({ Scope: "Accessible", Page: "1" });
        expect(router.state.location.state).toMatchObject({ view: "scope=Accessible&search=2026%2F0001" });
    });

    it("searches a moment after the user stops typing", async () => {
        const calls = mockList();
        const { user } = renderList();

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.type(screen.getByRole("searchbox", { name: "Назва або реєстраційний номер" }), "відпусток");

        await waitFor(() => expect(lastQuery(calls).Search).toBe("відпусток"));
    });

    it("clears the search", async () => {
        const calls = mockList();
        const { user } = renderList({ state: { view: "search=графік" } });

        const search = await screen.findByRole("searchbox", { name: "Назва або реєстраційний номер" });
        await user.clear(search);
        await user.keyboard("{Enter}");

        await waitFor(() => expect(lastQuery(calls).Search).toBeUndefined());
    });

    it("filters by state and resets the page", async () => {
        const calls = mockList();
        const { user } = renderList({ state: { view: "page=2" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("combobox", { name: "Статус" }));
        const options = screen.getAllByRole("option").map((option) => option.textContent);
        expect(options).toEqual(["Усі статуси", "Чернетка", "На підписанні", "Підписано", "Відхилено", "В архіві"]);
        await user.click(screen.getByRole("option", { name: "Підписано" }));

        await waitFor(() => expect(lastQuery(calls).Status).toBe("Signed"));
        expect(lastQuery(calls).Page).toBe("1");
    });

    it("removes the state filter with «Усі статуси»", async () => {
        const calls = mockList();
        const { user } = renderList({ state: { view: "status=Rejected" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("combobox", { name: "Статус" }));
        await user.click(screen.getByRole("option", { name: "Усі статуси" }));

        await waitFor(() => expect(lastQuery(calls).Status).toBeUndefined());
    });

    it("filters by kind", async () => {
        const calls = mockList();
        const { user } = renderList();

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("combobox", { name: "Вид документа" }));
        expect(screen.getAllByRole("option")).toHaveLength(13);
        await user.click(screen.getByRole("option", { name: "Договір" }));

        await waitFor(() => expect(lastQuery(calls).Kind).toBe("Contract"));
    });

    it("removes the kind filter with «Усі види»", async () => {
        const calls = mockList();
        const { user } = renderList({ state: { view: "kind=Act" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("combobox", { name: "Вид документа" }));
        await user.click(screen.getByRole("option", { name: "Усі види" }));

        await waitFor(() => expect(lastQuery(calls).Kind).toBeUndefined());
    });

    it("sorts by a column, ascending first, then descending", async () => {
        const calls = mockList();
        const { user } = renderList();

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("link", { name: /^Номер/ }));
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ SortBy: "number", Descending: "false" }));

        await user.click(screen.getByRole("link", { name: /^Номер/ }));
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ SortBy: "number", Descending: "true" }));
    });

    it("pages through the registry and keeps the page in the history state", async () => {
        const calls = mockList([makeItem()], { page: 1, pageSize: 20, totalCount: 45 });
        const { user, router } = renderList({ state: { view: "scope=Accessible" } });

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        expect(screen.getByText("1–20 з 45")).toBeInTheDocument();
        await user.click(screen.getByRole("link", { name: "Наступна сторінка" }));

        await waitFor(() => expect(lastQuery(calls).Page).toBe("2"));
        expect(router.state.location.state).toMatchObject({ view: "scope=Accessible&page=2" });
    });

    it("changes the page size", async () => {
        const calls = mockList([makeItem()], { totalCount: 45 });
        const { user } = renderList();

        await screen.findByRole("link", { name: "Про затвердження графіка відпусток" });
        await user.click(screen.getByRole("combobox", { name: "На сторінці" }));
        await user.click(screen.getByRole("option", { name: "50" }));

        await waitFor(() => expect(lastQuery(calls).PageSize).toBe("50"));
    });

    it("says nothing awaits my signature when the section is empty", async () => {
        mockList([]);
        renderList();

        expect(await screen.findByText("Документів не знайдено")).toBeInTheDocument();
        expect(screen.getByText("Документів, що очікують Вашого підпису, немає.")).toBeInTheDocument();
    });

    it("suggests changing the filters when a search finds nothing", async () => {
        mockList([]);
        renderList({ state: { view: "search=немає" } });

        expect(await screen.findByText("Документів не знайдено")).toBeInTheDocument();
        expect(screen.getByText("Спробуйте змінити параметри пошуку або фільтри.")).toBeInTheDocument();
    });

    it("suggests changing the filters when another section is empty", async () => {
        mockList([]);
        renderList({ state: { view: "scope=Authored" } });

        expect(await screen.findByText("Спробуйте змінити параметри пошуку або фільтри.")).toBeInTheDocument();
    });

    it("shows the server's message when the registry fails to load", async () => {
        mockApi("get", "/api/v1/documents", () => problem(403, "forbidden", "Немає доступу до документів."));
        renderList();

        expect(await screen.findByText("Немає доступу до документів.")).toBeInTheDocument();
    });

    it("shows a general message when the server cannot be reached", async () => {
        mockApi("get", "/api/v1/documents", () => Response.error());
        renderList();

        expect(await screen.findByRole("alert")).toHaveTextContent(/Не вдалося зв'язатися з сервером|Не вдалося завантажити документи/);
    });

    it("opens a document from its row", async () => {
        mockList();
        const { user } = renderList();

        await user.click(await screen.findByRole("link", { name: "Відкрити" }));

        expect(await navigatedTo()).toBe("/documents/doc-1");
    });
});
