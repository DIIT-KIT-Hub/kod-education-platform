import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { rememberPage } from "@/shared/lib/lastPage";
import { makeUser } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi } from "@/test/server";
import { HelpLayout } from "./components/HelpLayout";
import { ManualContext } from "./components/manualContext";
import { HelpHomePage } from "./pages/HelpHomePage";
import { HelpTopicPage } from "./pages/HelpTopicPage";

/**
 * The permission titles the access badges show.
 */
function mockPermissions() {
    mockApi("get", "/api/v1/permissions", [
        { code: "documents.view", module: "Documents", title: "Мої документи", description: "" },
        { code: "documents.create", module: "Documents", title: "Створення документів", description: "" },
        { code: "users.manage", module: "Users", title: "Керування користувачами", description: "" },
    ]);
}

/**
 * The topic page as the help layout shows it to a reader.
 * @param granted Whether the reader holds the permissions of the page.
 * @returns The page inside the manual context.
 */
function topicPage(granted = true) {
    const titles = new Map([["documents.create", "Створення документів"]]);
    return (
        <ManualContext.Provider value={{ permissionTitles: titles, has: () => granted }}>
            <HelpTopicPage />
        </ManualContext.Provider>
    );
}

beforeEach(() => {
    sessionStorage.clear();
});

describe("HelpHomePage", () => {
    it("shows the chapters with their pages and the pages to start with", async () => {
        renderPage(<HelpHomePage />, { path: "/help" });

        expect(await screen.findByRole("heading", { name: "Довідка Кабінету організаційних документів" })).toBeInTheDocument();
        const chapters = within(screen.getByRole("region", { name: "Розділи довідки" }));
        expect(chapters.getByRole("link", { name: "Документи" })).toHaveAttribute("href", "/help/documents-list");
        expect(chapters.getByRole("link", { name: "Створення документа" })).toHaveAttribute("href", "/help/documents-create");
        const start = within(screen.getByRole("region", { name: "З чого почати" }));
        expect(start.getByRole("link", { name: /Активація облікового запису/ })).toHaveAttribute("href", "/help/account-activation");
    });

    it("opens the page of an anchor of the former one-page manual", async () => {
        renderPage(<HelpHomePage />, { path: "/help", url: "/help#documents-sign" });

        expect(await navigatedTo()).toBe("/help/documents-sign");
    });

    it("ignores an unknown anchor", async () => {
        renderPage(<HelpHomePage />, { path: "/help", url: "/help#nothing" });

        expect(await screen.findByRole("heading", { name: "Довідка Кабінету організаційних документів" })).toBeInTheDocument();
    });
});

describe("HelpTopicPage", { timeout: 20_000 }, () => {
    it("loads the page with its breadcrumbs, the access badge and the neighbours", async () => {
        renderPage(topicPage(), { path: "/help/:topicId", url: "/help/documents-create" });

        expect(await screen.findByRole("heading", { level: 1, name: "Створення документа" })).toBeInTheDocument();
        expect(await screen.findByRole("heading", { level: 2, name: "Файл" }, { timeout: 5000 })).toBeInTheDocument();
        const breadcrumbs = within(screen.getByRole("navigation", { name: "Навігаційний ланцюжок" }));
        expect(breadcrumbs.getByRole("link", { name: "Довідка" })).toHaveAttribute("href", "/help");
        expect(breadcrumbs.getByText("Документи")).toBeInTheDocument();
        expect(screen.getByText("Доступно Вам")).toBeInTheDocument();

        const neighbours = within(screen.getByRole("navigation", { name: "Сусідні сторінки" }));
        expect(neighbours.getByRole("link", { name: /Попередня сторінка/ })).toHaveAttribute("href", "/help/documents-lifecycle");
        expect(neighbours.getByRole("link", { name: /Наступна сторінка/ })).toHaveAttribute("href", "/help/documents-route");
        expect(screen.getByRole("link", { name: "Створити звернення" })).toHaveAttribute("href", "/requests/new");
    });

    it("gives the subheadings anchors and lists them under “On this page”", async () => {
        renderPage(topicPage(), { path: "/help/:topicId", url: "/help/documents-create" });

        const toc = within(await screen.findByRole("navigation", { name: "На цій сторінці" }, { timeout: 15_000 }));
        expect(screen.getByRole("heading", { level: 2, name: "Файл" })).toHaveAttribute("id", "файл");
        expect(toc.getByRole("link", { name: "Файл" })).toHaveAttribute("href", "#файл");
    });

    it("names the permission a reader lacks", async () => {
        renderPage(topicPage(false), { path: "/help/:topicId", url: "/help/documents-create" });

        expect(await screen.findByText("Потрібен дозвіл: «Створення документів»")).toBeInTheDocument();
    });

    it("says the page does not exist for an unknown topic", async () => {
        renderPage(topicPage(), { path: "/help/:topicId", url: "/help/no-such-page" });

        expect(await screen.findByText("Такої сторінки в довідці немає")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "До огляду довідки" })).toHaveAttribute("href", "/help");
    });

    it("has no previous page on the first topic", async () => {
        renderPage(topicPage(), { path: "/help/:topicId", url: "/help/intro-about" });

        const neighbours = within(await screen.findByRole("navigation", { name: "Сусідні сторінки" }));
        expect(neighbours.queryByRole("link", { name: /Попередня сторінка/ })).not.toBeInTheDocument();
    });
});

describe("HelpLayout", { timeout: 20_000 }, () => {
    it("leads back to the page of the cabinet the user left, with its view", async () => {
        mockPermissions();
        rememberPage({ path: "/documents", state: { view: "scope=Authored" } });
        const { user, router } = renderPage(<HelpLayout />, { path: "/help" });

        await user.click(await screen.findByRole("button", { name: "Повернутися до кабінету" }));

        expect(await navigatedTo()).toBe("/documents");
        expect(router.state.location.state).toEqual({ view: "scope=Authored" });
    });

    it("leads to the home page when the last page is unknown or unsafe", async () => {
        mockPermissions();
        rememberPage({ path: "//evil.example", state: null });
        const { user } = renderPage(<HelpLayout />, { path: "/help", routes: [{ path: "/", element: <p>home</p> }] });

        await user.click(await screen.findByRole("button", { name: "Повернутися до кабінету" }));

        expect(await screen.findByText("home")).toBeInTheDocument();
    });

    it("lists every chapter, unfolds the one of the open page and folds on demand", async () => {
        mockPermissions();
        const { user } = renderPage(<HelpLayout />, { path: "/help/:topicId", url: "/help/documents-sign" });

        const nav = within(await screen.findByRole("complementary", { name: "Зміст довідки" }));
        const documents = nav.getByRole("button", { name: "Документи" });
        expect(documents).toHaveAttribute("aria-expanded", "true");
        expect(nav.getByRole("link", { name: /^Підписання документа/ })).toHaveAttribute("aria-current", "page");
        expect(nav.getByRole("button", { name: "Новини" })).toHaveAttribute("aria-expanded", "false");

        await user.click(documents);
        expect(documents).toHaveAttribute("aria-expanded", "false");
        await user.click(nav.getByRole("button", { name: "Новини" }));
        expect(nav.getByRole("button", { name: "Новини" })).toHaveAttribute("aria-expanded", "true");
    });

    it("marks the pages of features the reader cannot use", async () => {
        mockPermissions();
        renderPage(<HelpLayout />, {
            path: "/help/:topicId",
            url: "/help/admin-overview",
            user: makeUser({ permissions: ["documents.view"] }),
        });

        const nav = within(await screen.findByRole("complementary", { name: "Зміст довідки" }));
        expect(
            within(nav.getByRole("link", { name: /Створення облікового запису/ })).getByLabelText("Потрібен дозвіл"),
        ).toBeInTheDocument();
    });

    it("opens the search with Ctrl + K, finds pages and opens the chosen one at its section", async () => {
        mockPermissions();
        const { user } = renderPage(<HelpLayout />, { path: "/help" });
        await screen.findByRole("button", { name: "Повернутися до кабінету" });

        fireEvent.keyDown(window, { key: "k", ctrlKey: true });
        const input = await screen.findByRole("combobox");
        expect(screen.getByText(/Введіть щонайменше два символи/)).toBeInTheDocument();

        await user.type(input, "резервний код");
        const results = await screen.findByRole("listbox", { name: "Результати пошуку" }, { timeout: 5000 });
        const first = within(results).getAllByRole("option")[0];
        expect(first).toHaveAttribute("aria-selected", "true");
        expect(first).toHaveTextContent("Вхід за резервним кодом");

        await user.keyboard("{Enter}");
        expect(await navigatedTo()).toMatch(/^\/help\/account-recovery/);
    });

    it("says when nothing is found and moves through results with the arrows", async () => {
        mockPermissions();
        const { user } = renderPage(<HelpLayout />, { path: "/help" });

        await user.click(await screen.findByRole("button", { name: /Пошук у довідці/ }));
        const input = await screen.findByRole("combobox");
        await user.type(input, "ґґґґ неіснуюче");
        expect(await screen.findByText(/Нічого не знайдено/, {}, { timeout: 5000 })).toBeInTheDocument();

        await user.clear(input);
        await user.type(input, "документ");
        const options = await waitFor(() => {
            const found = screen.getAllByRole("option");
            expect(found.length).toBeGreaterThan(1);
            return found;
        });
        await user.keyboard("{ArrowDown}");
        expect(options[1]).toHaveAttribute("aria-selected", "true");
        await user.keyboard("{ArrowUp}{ArrowUp}");
        expect(screen.getAllByRole("option").at(-1)).toHaveAttribute("aria-selected", "true");
    });
});
