import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import {
    answerConfirm,
    choose,
    clearToastsAfterEach,
    findToast,
    ids,
    makeAuditEntry,
    makeCatalog,
    makeRoleDetails,
    makeRoleListItem,
} from "../testing";
import { AuditPage } from "./AuditPage";
import { RoleEditorPage } from "./RoleEditorPage";
import { RolesPage } from "./RolesPage";

clearToastsAfterEach();

/**
 * The query of the last list request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

describe("AuditPage", () => {
    it("shows the events with their names, details, users and addresses", async () => {
        mockApi(
            "get",
            "/api/v1/audit-entries",
            makePage([
                makeAuditEntry(),
                makeAuditEntry({ id: "e-2", action: "custom.unknown", userName: null, ipAddress: null, details: null }),
            ]),
        );
        renderPage(<AuditPage />, { path: "/admin/audit" });

        const signIn = (await screen.findByText("Вхід у систему")).closest("tr") as HTMLElement;
        expect(within(signIn).getByText("Chrome, Windows")).toBeInTheDocument();
        expect(within(signIn).getByText("Коваленко Марія Андріївна")).toBeInTheDocument();
        expect(within(signIn).getByText("10.0.0.7")).toBeInTheDocument();
        const unknown = screen.getByText("custom.unknown").closest("tr") as HTMLElement;
        expect(within(unknown).getAllByText("—")).toHaveLength(2);
    });

    it("never takes a code that matches an object member for an event name", async () => {
        mockApi("get", "/api/v1/audit-entries", makePage([makeAuditEntry({ action: "constructor" })]));
        renderPage(<AuditPage />, { path: "/admin/audit" });

        expect(await screen.findByText("constructor")).toBeInTheDocument();
    });

    it("filters by category (the action prefix), dates and search", async () => {
        const calls = mockApi("get", "/api/v1/audit-entries", makePage([makeAuditEntry()]));
        const page = renderPage(<AuditPage />, { path: "/admin/audit", state: { view: "from=2026-11-01&to=2026-11-30" } });
        await screen.findByText("Вхід у систему");
        expect(lastQuery(calls)).toMatchObject({ From: "2026-11-01", To: "2026-11-30", PageSize: "50" });

        await choose(page.user, screen.getByRole("combobox", { name: "Категорія" }), "Документи");
        await waitFor(() => expect(lastQuery(calls).Action).toBe("documents."));

        await page.user.type(screen.getByRole("searchbox"), "10.0.0.7{Enter}");
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ Search: "10.0.0.7", Action: "documents." }));
    });

    it("ignores an unknown category kept in the view", async () => {
        const calls = mockApi("get", "/api/v1/audit-entries", makePage([]));
        renderPage(<AuditPage />, { path: "/admin/audit", state: { view: "category=evil." } });

        expect(await screen.findByText("Подій не знайдено")).toBeInTheDocument();
        expect(lastQuery(calls).Action).toBeUndefined();
    });

    it("shows the server's message when the log cannot be loaded", async () => {
        mockApi("get", "/api/v1/audit-entries", () => problem(403, "forbidden", "Недостатньо прав для виконання дії."));
        renderPage(<AuditPage />, { path: "/admin/audit" });

        expect(await screen.findByText("Недостатньо прав для виконання дії.")).toBeInTheDocument();
    });
});

describe("RolesPage", () => {
    it("lists the roles with their kind, counts and settings links", async () => {
        mockApi(
            "get",
            "/api/v1/roles",
            makePage([
                makeRoleListItem(),
                makeRoleListItem({ id: "r-2", code: "administrator", name: "Адміністратор", isSystem: true, isLeadership: true }),
            ]),
        );
        renderPage(<RolesPage />, { path: "/admin/roles" });

        const own = (await screen.findByRole("link", { name: /^Працівник/ })).closest("tr") as HTMLElement;
        expect(within(own).getByText("Власна")).toBeInTheDocument();
        expect(within(own).getByText("12")).toBeInTheDocument();
        expect(within(own).getByRole("link", { name: "Налаштувати" })).toHaveAttribute("href", `/admin/roles/${ids.role}`);
        const system = screen.getByRole("link", { name: /^Адміністратор/ }).closest("tr") as HTMLElement;
        expect(within(system).getByText("Системна")).toBeInTheDocument();
        expect(within(system).getByText("Керівна")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Нова роль" })).toHaveAttribute("href", "/admin/roles/new");
    });

    it("says when no role matches", async () => {
        mockApi("get", "/api/v1/roles", makePage([]));
        renderPage(<RolesPage />, { path: "/admin/roles" });

        expect(await screen.findByText("Ролей не знайдено")).toBeInTheDocument();
    });
});

describe("RoleEditorPage", () => {
    /**
     * Renders the editor of a new role.
     * @returns The rendered page.
     */
    function renderNew() {
        mockApi("get", "/api/v1/permissions", makeCatalog());
        return renderPage(<RoleEditorPage />, { path: "/admin/roles/new" });
    }

    /**
     * Renders the editor of an existing role.
     * @param role The role.
     * @returns The rendered page.
     */
    function renderExisting(role = makeRoleDetails()) {
        mockApi("get", "/api/v1/permissions", makeCatalog());
        mockApi("get", "/api/v1/roles/{id}", role);
        return renderPage(<RoleEditorPage />, { path: "/admin/roles/:id", url: `/admin/roles/${role.id}` });
    }

    it("creates a role with its code, name and the ticked permissions", async () => {
        const created = mockApi("post", "/api/v1/roles", { id: "r-new" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Код/ }), "project-manager");
        await page.user.type(screen.getByRole("textbox", { name: /Назва/ }), " Керівник проєкту ");
        await page.user.click(screen.getByRole("checkbox", { name: /Створення документів/ }));
        await page.user.click(screen.getByRole("checkbox", { name: /users\.manage/ }));
        await page.user.click(screen.getByRole("checkbox", { name: /Керівна посада/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        await findToast("Роль збережено");
        expect(await navigatedTo()).toBe("/admin/roles");
        expect(created[0]?.body).toEqual({
            code: "project-manager",
            name: "Керівник проєкту",
            description: null,
            isLeadership: true,
            permissions: ["documents.create", "users.manage"],
        });
        expect(created[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("checks the code and the name", async () => {
        const created = mockApi("post", "/api/v1/roles", { id: "never" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Код/ }), "Менеджер Проєкту");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Лише малі латинські літери, цифри та «-».")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть назву ролі.")).toBeInTheDocument();
        expect(created).toHaveLength(0);
    });

    it("edits a role with its permissions but without changing its code", async () => {
        const updated = mockApi("put", "/api/v1/roles/{id}");
        const page = renderExisting();

        expect(await screen.findByRole("checkbox", { name: /Перегляд документів/ })).toBeChecked();
        expect(screen.queryByRole("textbox", { name: /Код/ })).not.toBeInTheDocument();
        await page.user.click(screen.getByRole("checkbox", { name: /Перегляд документів/ }));
        await page.user.click(screen.getByRole("checkbox", { name: /Перегляд користувачів/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await navigatedTo()).toBe("/admin/roles");
        expect(updated[0]?.body).toMatchObject({ name: "Працівник", description: "Звичайна посада", permissions: ["users.view"] });
    });

    it("shows the administrator role with every permission and nothing to tick", async () => {
        renderExisting(makeRoleDetails({ isSystem: true, isAdministrator: true, name: "Адміністратор", permissions: [] }));

        expect(await screen.findByText("Системний адміністратор завжди має всі дозволи.")).toBeInTheDocument();
        for (const name of [/Перегляд документів/, /Створення документів/, /Перегляд користувачів/, /users.manage/]) {
            const checkbox = screen.getByRole("checkbox", { name });
            expect(checkbox).toBeChecked();
            expect(checkbox).toBeDisabled();
        }
        expect(screen.queryByRole("button", { name: "Видалити роль" })).not.toBeInTheDocument();
    });

    it("does not delete a role that people hold", async () => {
        renderExisting(makeRoleDetails({ userCount: 3 }));

        expect(await screen.findByRole("button", { name: "Видалити роль" })).toBeDisabled();
    });

    it("deletes an unused role after confirmation", async () => {
        const deleted = mockApi("delete", "/api/v1/roles/{id}");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Видалити роль" }));
        expect(await answerConfirm(page.user, "Видалити")).toBe("Видалити роль?");

        expect(await navigatedTo()).toBe("/admin/roles");
        expect(deleted).toHaveLength(1);
    });

    it("reports a refused save", async () => {
        mockApi("put", "/api/v1/roles/{id}", () => problem(409, "conflict", "Роль із такою назвою вже існує."));
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Зберегти" }));

        await findToast("Роль із такою назвою вже існує.");
    });

    it("shows the server's message when the role cannot be loaded", async () => {
        mockApi("get", "/api/v1/permissions", makeCatalog());
        mockApi("get", "/api/v1/roles/{id}", () => problem(404, "not_found", "Роль не знайдено."));
        renderPage(<RoleEditorPage />, { path: "/admin/roles/:id", url: "/admin/roles/missing" });

        expect(await screen.findByText("Роль не знайдено.")).toBeInTheDocument();
    });
});
