import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage, type Schemas } from "@/test/factories";
import { renderPage, type RenderOptions } from "@/test/render";
import { captureDownloads } from "@/test/downloads";
import { file, mockApi, problem, type RecordedRequest } from "@/test/server";
import { formatDateTime } from "@/shared/i18n/format";
import { choose, ids, makeRoleOptions, makeUnitOptions, makeUserListItem, optionsOf, userWith, viewOf } from "../testing";
import { UsersPage } from "./UsersPage";

/**
 * Mocks the endpoints of the page.
 * @param page The page of users (or a status code / responder).
 * @returns The recorded requests of the list, the units and the roles.
 */
function mockUsers(page: Parameters<typeof mockApi>[2] = makePage([makeUserListItem()])) {
    return {
        users: mockApi("get", "/api/v1/users", page),
        units: mockApi("get", "/api/v1/users/units", makeUnitOptions()),
        roles: mockApi("get", "/api/v1/users/roles", makeRoleOptions()),
    };
}

/**
 * Renders the user list.
 * @param options The render options (user, history state, address).
 * @returns The rendered page.
 */
function renderUsers(options: RenderOptions = {}) {
    return renderPage(<UsersPage />, { path: "/admin/users", ...options });
}

/**
 * The query of the last list request.
 * @param calls The recorded list requests.
 * @returns The query values as an object.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

/**
 * The row of a user in the table.
 * @param name The full name.
 * @returns The row.
 */
function rowOf(name: string): HTMLElement {
    return screen.getByRole("link", { name }).closest("tr") as HTMLElement;
}

describe("UsersPage", () => {
    it("asks for the first page of all users and the filter choices", async () => {
        const calls = mockUsers();
        renderUsers();

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(lastQuery(calls.users)).toEqual({ Page: "1", PageSize: "20", Descending: "false", Status: "All" });
        expect(calls.units[0]?.query.get("manage")).toBe("false");
        expect(calls.roles).toHaveLength(1);
    });

    it("shows a user row with the name, e-mail, position, unit, last sign-in and links to the account", async () => {
        mockUsers();
        renderUsers();

        const link = await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(link).toHaveAttribute("href", `/admin/users/${ids.user}`);
        const row = rowOf("Коваленко Марія Андріївна");
        expect(within(row).getByText("maria@odc.local")).toBeInTheDocument();
        expect(within(row).getByText("Доцент")).toBeInTheDocument();
        expect(within(row).getByText("Кафедра права")).toBeInTheDocument();
        expect(within(row).getByText(formatDateTime("2026-11-01T08:30:00Z"))).toBeInTheDocument();
        expect(within(row).getByText("Активний")).toBeInTheDocument();
        expect(within(row).queryByText("Без 2FA")).not.toBeInTheDocument();
        expect(within(row).getByRole("link", { name: "Відкрити" })).toHaveAttribute("href", `/admin/users/${ids.user}`);
    });

    it("shows a dash for a user without a position", async () => {
        mockUsers(makePage([makeUserListItem({ position: null, unit: null })]));
        renderUsers();

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(within(rowOf("Коваленко Марія Андріївна")).getByText("—")).toBeInTheDocument();
        expect(screen.queryByText("Кафедра права")).not.toBeInTheDocument();
    });

    it("marks blocked, locked out, awaiting and active accounts and the missing authenticator", async () => {
        const users: Schemas["UserListItem"][] = [
            makeUserListItem({ id: "a", fullName: "Заблокований", isActive: false, isLockedOut: true }),
            makeUserListItem({ id: "b", fullName: "Тимчасовий", isLockedOut: true }),
            makeUserListItem({ id: "c", fullName: "Новенький", awaitsActivation: true, twoFactorEnabled: false }),
            makeUserListItem({ id: "d", fullName: "Без автентифікатора", twoFactorEnabled: false }),
        ];
        mockUsers(makePage(users));
        renderUsers();

        await screen.findByRole("link", { name: "Заблокований" });
        expect(within(rowOf("Заблокований")).getByText("Заблоковано")).toBeInTheDocument();
        expect(within(rowOf("Заблокований")).queryByText("Тимчасово заблоковано")).not.toBeInTheDocument();
        expect(within(rowOf("Тимчасовий")).getByText("Тимчасово заблоковано")).toBeInTheDocument();
        const awaiting = within(rowOf("Новенький"));
        expect(awaiting.getByText("Очікує активації")).toBeInTheDocument();
        expect(awaiting.getByTitle("Користувач ще не підтвердив пошту й не задав пароль")).toBeInTheDocument();
        expect(awaiting.queryByText("Без 2FA")).not.toBeInTheDocument();
        const noTwoFactor = within(rowOf("Без автентифікатора"));
        expect(noTwoFactor.getByText("Активний")).toBeInTheDocument();
        expect(noTwoFactor.getByText("Без 2FA")).toBeInTheDocument();
        expect(noTwoFactor.getByTitle("Автентифікатор ще не прив'язано")).toBeInTheDocument();
    });

    it("shows the empty state when nothing matches", async () => {
        mockUsers(makePage([]));
        renderUsers();

        expect(await screen.findByText("Користувачів не знайдено")).toBeInTheDocument();
        expect(screen.queryByRole("table")).not.toBeInTheDocument();
    });

    it("shows a skeleton while the list loads", async () => {
        let release: () => void = () => undefined;
        const answered = new Promise<void>((resolve) => (release = resolve));
        mockUsers(async () => {
            await answered;
            return Response.json(makePage([makeUserListItem()]));
        });
        renderUsers();

        expect(await screen.findByRole("status", { name: "Завантаження" })).toBeInTheDocument();
        release();
        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(screen.queryByRole("status", { name: "Завантаження" })).not.toBeInTheDocument();
    });

    it("shows the server's message when the list cannot be loaded", async () => {
        mockUsers(() => problem(403, "forbidden", "Недостатньо прав для перегляду користувачів."));
        renderUsers();

        expect(await screen.findByRole("alert")).toHaveTextContent("Недостатньо прав для перегляду користувачів.");
    });

    it("shows a general message as a toast when the server cannot be reached and keeps the page", async () => {
        mockUsers(() => Response.error());
        renderUsers();

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Не вдалося зв'язатися з сервером. Перевірте з'єднання й спробуйте ще раз.",
        );
        expect(screen.getByRole("heading", { name: "Користувачі" })).toBeInTheDocument();
        expect(screen.getByRole("searchbox", { name: "ПІБ або e-mail" })).toBeInTheDocument();
        expect(screen.queryByRole("table")).not.toBeInTheDocument();
    });

    it("shows the server's message as a toast when the list fails", async () => {
        mockUsers(() => problem(500, "server_error", "Сервер тимчасово недоступний."));
        renderUsers();

        expect(await screen.findByRole("alert")).toHaveTextContent("Сервер тимчасово недоступний.");
    });

    it("shows one toast when several loads fail for the same reason", async () => {
        mockApi("get", "/api/v1/users", () => Response.error());
        mockApi("get", "/api/v1/users/units", () => Response.error());
        mockApi("get", "/api/v1/users/roles", () => Response.error());
        renderUsers();

        await screen.findByRole("alert");
        await waitFor(() => expect(screen.getAllByRole("alert")).toHaveLength(1));
    });

    it("searches by name or e-mail and keeps the search in the history entry", async () => {
        const calls = mockUsers();
        const { user, router } = renderUsers({ state: { view: "page=3" } });

        const search = await screen.findByRole("searchbox", { name: "ПІБ або e-mail" });
        await user.type(search, "  maria {Enter}");

        await waitFor(() => expect(lastQuery(calls.users).Search).toBe("maria"));
        expect(lastQuery(calls.users).Page).toBe("1");
        expect(viewOf(router).get("search")).toBe("maria");
        expect(viewOf(router).has("page")).toBe(false);
        expect(router.state.location.search).toBe("");
    });

    it("offers every account state and filters by the chosen one", async () => {
        const calls = mockUsers();
        const { user, router } = renderUsers();

        const status = await screen.findByRole("combobox", { name: "Статус" });
        expect(status).toHaveTextContent("Усі статуси");
        expect(await optionsOf(user, status)).toEqual(["Усі статуси", "Активні", "Заблоковані", "Очікують активації"]);

        await choose(user, status, "Очікують активації");
        await waitFor(() => expect(lastQuery(calls.users).Status).toBe("AwaitingActivation"));
        expect(viewOf(router).get("status")).toBe("AwaitingActivation");
        expect(status).toHaveTextContent("Очікують активації");

        await choose(user, status, "Заблоковані");
        await waitFor(() => expect(lastQuery(calls.users).Status).toBe("Blocked"));

        // Back to all accounts: the first page of the list is still in the cache, so no new request is needed.
        await choose(user, status, "Усі статуси");
        await waitFor(() => expect(viewOf(router).has("status")).toBe(false));
        expect(status).toHaveTextContent("Усі статуси");
        expect(calls.users[0]?.query.get("Status")).toBe("All");
    });

    it("filters by unit and by role and clears the filters again", async () => {
        const calls = mockUsers();
        const { user, router } = renderUsers({ state: { view: "page=2" } });

        const unit = await screen.findByRole("combobox", { name: "Підрозділ" });
        await waitFor(async () => expect(await optionsOf(user, unit)).toEqual(["Усі підрозділи", "Університет", "Кафедра права", "Ліцей"]));
        await choose(user, unit, "Кафедра права");
        await waitFor(() => expect(lastQuery(calls.users).UnitId).toBe(ids.department));
        expect(lastQuery(calls.users).Page).toBe("1");
        expect(viewOf(router).get("unit")).toBe(ids.department);

        const role = screen.getByRole("combobox", { name: "Роль" });
        expect(await optionsOf(user, role)).toEqual(["Усі ролі", "Працівник", "Керівник"]);
        await choose(user, role, "Керівник");
        await waitFor(() => expect(lastQuery(calls.users).RoleId).toBe(ids.otherRole));
        expect(lastQuery(calls.users).UnitId).toBe(ids.department);

        await choose(user, unit, "Усі підрозділи");
        await waitFor(() => expect(lastQuery(calls.users).UnitId).toBeUndefined());
        await choose(user, role, "Усі ролі");
        await waitFor(() => expect(lastQuery(calls.users).RoleId).toBeUndefined());
        expect(viewOf(router).toString()).toBe("");
    });

    it("restores the whole view from the history entry", async () => {
        const calls = mockUsers();
        renderUsers({
            state: {
                view: `search=ivan&status=Blocked&unit=${ids.organization}&role=${ids.role}&page=2&pageSize=50&sortBy=name&descending=true`,
            },
        });

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(lastQuery(calls.users)).toEqual({
            Search: "ivan",
            Page: "2",
            PageSize: "50",
            SortBy: "name",
            Descending: "true",
            Status: "Blocked",
            UnitId: ids.organization,
            RoleId: ids.role,
        });
        expect(screen.getByRole("searchbox", { name: "ПІБ або e-mail" })).toHaveValue("ivan");
        expect(screen.getByRole("combobox", { name: "Статус" })).toHaveTextContent("Заблоковані");
        await waitFor(() => expect(screen.getByRole("combobox", { name: "Підрозділ" })).toHaveTextContent("Університет"));
        expect(screen.getByRole("combobox", { name: "Роль" })).toHaveTextContent("Працівник");
    });

    it("ignores an unknown status and a page below one", async () => {
        const calls = mockUsers();
        renderUsers({ state: { view: "status=Deleted&page=-4&pageSize=abc" } });

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(lastQuery(calls.users)).toMatchObject({ Status: "All", Page: "1", PageSize: "20" });
    });

    it("moves a query string of the address into the history entry", async () => {
        const calls = mockUsers();
        const { router } = renderUsers({ url: "/admin/users?status=AwaitingActivation" });

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        expect(calls.users[0]?.query.get("Status")).toBe("AwaitingActivation");
        await waitFor(() => expect(router.state.location.search).toBe(""));
        expect(viewOf(router).get("status")).toBe("AwaitingActivation");
    });

    it("sorts by name and by the last sign-in, ascending first", async () => {
        const calls = mockUsers();
        const { user, router } = renderUsers({ state: { view: "page=2" } });

        await user.click(await screen.findByRole("link", { name: "Користувач" }));
        await waitFor(() => expect(lastQuery(calls.users)).toMatchObject({ SortBy: "name", Descending: "false", Page: "1" }));
        expect(screen.getByRole("columnheader", { name: "Користувач" })).toHaveAttribute("aria-sort", "ascending");

        await user.click(screen.getByRole("link", { name: "Користувач" }));
        await waitFor(() => expect(lastQuery(calls.users)).toMatchObject({ SortBy: "name", Descending: "true" }));
        expect(screen.getByRole("columnheader", { name: "Користувач" })).toHaveAttribute("aria-sort", "descending");

        await user.click(screen.getByRole("link", { name: "Останній вхід" }));
        await waitFor(() => expect(lastQuery(calls.users)).toMatchObject({ SortBy: "lastSignIn", Descending: "false" }));
        expect(viewOf(router).get("sortBy")).toBe("lastSignIn");
        expect(screen.queryByRole("link", { name: "Посада" })).not.toBeInTheDocument();
    });

    it("pages through the users and changes the page size", async () => {
        const calls = mockUsers((request) =>
            Response.json(
                makePage([makeUserListItem()], {
                    page: Number(request.query.get("Page")),
                    pageSize: Number(request.query.get("PageSize")),
                    totalCount: 45,
                }),
            ),
        );
        const { user, router } = renderUsers();

        const pages = await screen.findByRole("navigation", { name: "Сторінки" });
        expect(within(pages).getByText("1–20 з 45")).toBeInTheDocument();
        await user.click(within(pages).getByRole("link", { name: "2" }));
        await waitFor(() => expect(lastQuery(calls.users).Page).toBe("2"));
        expect(viewOf(router).get("page")).toBe("2");

        await user.click(within(screen.getByRole("navigation", { name: "Сторінки" })).getByRole("link", { name: "Наступна сторінка" }));
        await waitFor(() => expect(lastQuery(calls.users).Page).toBe("3"));

        await choose(user, screen.getByRole("combobox", { name: "На сторінці" }), "50");
        await waitFor(() => expect(lastQuery(calls.users)).toMatchObject({ PageSize: "50", Page: "1" }));
        expect(viewOf(router).has("page")).toBe(false);
    });

    it("exports with the current filters and reports the start and the end with toasts", async () => {
        mockUsers();
        const saved = captureDownloads();
        const exports = mockApi("get", "/api/v1/user-exports", () => file("users-20261109-1000.xlsx"));
        const { user } = renderUsers({ state: { view: `search=ivan&status=Blocked&unit=${ids.department}&role=${ids.role}&page=2` } });

        await user.click(await screen.findByRole("button", { name: "XLSX" }));

        expect(await screen.findByText("Експорт користувачів розпочато. Файл завантажиться, щойно буде готовий.")).toBeInTheDocument();
        expect(await screen.findByText("Експорт завершено: файл users-20261109-1000.xlsx завантажено.")).toBeInTheDocument();
        expect(Object.fromEntries(exports[0]?.query ?? [])).toEqual({
            format: "Xlsx",
            Status: "Blocked",
            Search: "ivan",
            UnitId: ids.department,
            RoleId: ids.role,
        });
        expect(saved).toEqual([{ name: "users-20261109-1000.xlsx", href: expect.stringMatching(/^blob:/) as unknown }]);
        expect(screen.getByRole("button", { name: "XLSX" })).toBeEnabled();
    });

    it("exports every user as CSV without filters", async () => {
        mockUsers();
        const saved = captureDownloads();
        const exports = mockApi("get", "/api/v1/user-exports", () => file("users-20261109-1000.csv"));
        const { user } = renderUsers();

        await user.click(await screen.findByRole("button", { name: "CSV" }));

        expect(await screen.findByText("Експорт завершено: файл users-20261109-1000.csv завантажено.")).toBeInTheDocument();
        expect(Object.fromEntries(exports[0]?.query ?? [])).toEqual({ format: "Csv", Status: "All" });
        expect(saved.map(({ name }) => name)).toEqual(["users-20261109-1000.csv"]);
    });

    it("exports the awaiting accounts when that filter is chosen", async () => {
        mockUsers();
        captureDownloads();
        const exports = mockApi("get", "/api/v1/user-exports", () => file("users.csv"));
        const { user } = renderUsers();

        await choose(user, await screen.findByRole("combobox", { name: "Статус" }), "Очікують активації");
        await user.click(screen.getByRole("button", { name: "CSV" }));

        await waitFor(() => expect(exports[0]?.query.get("Status")).toBe("AwaitingActivation"));
    });

    it("keeps the button busy while the export is prepared", async () => {
        mockUsers();
        captureDownloads();
        let finish: () => void = () => undefined;
        const ready = new Promise<void>((resolve) => {
            finish = resolve;
        });
        mockApi("get", "/api/v1/user-exports", async () => {
            await ready;
            return file("users.xlsx");
        });
        const { user } = renderUsers();

        await user.click(await screen.findByRole("button", { name: "XLSX" }));
        await waitFor(() => expect(screen.getByRole("button", { name: "XLSX" })).toBeDisabled());
        expect(screen.queryByText(/Експорт завершено/)).not.toBeInTheDocument();

        finish();
        expect(await screen.findByText("Експорт завершено: файл users.xlsx завантажено.")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "XLSX" })).toBeEnabled();
    });

    it("says why the export failed and saves nothing", async () => {
        mockUsers();
        const saved = captureDownloads();
        mockApi("get", "/api/v1/user-exports", () => problem(403, "forbidden", "Недостатньо прав для експорту."));
        const { user } = renderUsers();

        await user.click(await screen.findByRole("button", { name: "XLSX" }));

        expect(await screen.findByRole("alert")).toHaveTextContent("Недостатньо прав для експорту.");
        expect(screen.queryByText(/Експорт завершено/)).not.toBeInTheDocument();
        expect(saved).toEqual([]);
    });

    it("falls back to a general message when the export cannot be reached", async () => {
        mockUsers();
        captureDownloads();
        mockApi("get", "/api/v1/user-exports", () => Response.error());
        const { user } = renderUsers();

        await user.click(await screen.findByRole("button", { name: "CSV" }));

        expect(await screen.findByRole("alert")).toHaveTextContent("Не вдалося експортувати користувачів. Спробуйте ще раз.");
    });

    it("offers export, import and creation to a user with every permission", async () => {
        mockUsers();
        renderUsers();

        expect(await screen.findByRole("link", { name: "Імпорт" })).toHaveAttribute("href", "/admin/users/import");
        expect(screen.getByRole("link", { name: "Додати" })).toHaveAttribute("href", "/admin/users/new");
        expect(screen.getByRole("button", { name: "XLSX" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Адміністрування" })).toHaveAttribute("href", "/admin");
    });

    it.each([
        [["users.view"], []],
        [
            ["users.view", "users.export"],
            ["XLSX", "CSV"],
        ],
        [["users.view", "users.import"], ["Імпорт"]],
        [["users.view", "users.manage"], ["Додати"]],
        [["users.view", "users.security", "roles.manage"], []],
    ])("with %j offers only %j", async (permissions, expected) => {
        mockUsers();
        renderUsers({ user: userWith(...permissions) });

        await screen.findByRole("link", { name: "Коваленко Марія Андріївна" });
        for (const [name, role] of [
            ["XLSX", "button"],
            ["CSV", "button"],
            ["Імпорт", "link"],
            ["Додати", "link"],
        ] as const) {
            if (expected.includes(name)) {
                expect(screen.getByRole(role, { name })).toBeInTheDocument();
            } else {
                expect(screen.queryByRole(role, { name })).not.toBeInTheDocument();
            }
        }
    });

    it("says whose accounts are listed", async () => {
        mockUsers();
        renderUsers({ user: userWith("users.view") });

        expect(await screen.findByText(/Облікові записи працівників і зовнішніх користувачів Ваших організацій/)).toBeInTheDocument();
    });

    it("says that a cabinet-wide administrator sees every organization", async () => {
        mockUsers();
        renderUsers({ user: userWith("users.view", "administration.all") });

        expect(await screen.findByText(/зовнішніх користувачів усіх організацій кабінету/)).toBeInTheDocument();
    });
});
