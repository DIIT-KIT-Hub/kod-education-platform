import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Permissions } from "@/shared/session/session";
import { makePage } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import {
    choose,
    clearToastsAfterEach,
    findToast,
    ids,
    makeUnitDetails,
    makeUnitListItem,
    makeUnitOptions,
    makeUnitTypes,
    userWith,
} from "../testing";
import { UnitEditorPage } from "./UnitEditorPage";
import { UnitsPage } from "./UnitsPage";

clearToastsAfterEach();

/** An administrator of one organization (structure rights, no cabinet-wide rights). */
const organizationAdmin = userWith(Permissions.structureView, Permissions.structureManage);

/**
 * The query of the last list request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

/**
 * Mocks the choices of the unit pages (registered after `/organization-units/{id}`, so they win over it).
 */
function mockChoices() {
    mockApi("get", "/api/v1/unit-types", makeUnitTypes());
    mockApi("get", "/api/v1/organization-units/options", makeUnitOptions());
}

describe("UnitsPage", () => {
    it("lists the units with their parent, type, staff and state", async () => {
        mockApi(
            "get",
            "/api/v1/organization-units/list",
            makePage([
                makeUnitListItem(),
                makeUnitListItem({ id: "u-2", name: "Університет", code: "UNI", parentName: null, isActive: false }),
            ]),
        );
        mockChoices();
        renderPage(<UnitsPage />, { path: "/admin/units" });

        const law = (await screen.findByRole("link", { name: /^Кафедра права/ })).closest("tr") as HTMLElement;
        expect(within(law).getByText("Університет")).toBeInTheDocument();
        expect(within(law).getByText("7")).toBeInTheDocument();
        expect(within(law).getByText("Активний")).toBeInTheDocument();
        expect(within(law).getByRole("link", { name: "Редагувати" })).toHaveAttribute("href", `/admin/units/${ids.department}`);
        const top = screen.getByRole("link", { name: /^Університет/ }).closest("tr") as HTMLElement;
        expect(within(top).getByText("Організація верхнього рівня")).toBeInTheDocument();
        expect(within(top).getByText("Неактивний")).toBeInTheDocument();
    });

    it("filters by organization, type and search", async () => {
        const calls = mockApi("get", "/api/v1/organization-units/list", makePage([makeUnitListItem()]));
        mockChoices();
        const page = renderPage(<UnitsPage />, { path: "/admin/units" });
        await screen.findByRole("link", { name: /^Кафедра права/ });

        await choose(page.user, screen.getByRole("combobox", { name: "Організація" }), "Ліцей");
        await waitFor(() => expect(lastQuery(calls).OrganizationId).toBe(ids.otherOrganization));
        await choose(page.user, screen.getByRole("combobox", { name: "Тип" }), "Кафедра");
        await waitFor(() => expect(lastQuery(calls).TypeId).toBe(ids.departmentType));
        await page.user.type(screen.getByRole("searchbox"), "LAW{Enter}");
        await waitFor(() =>
            expect(lastQuery(calls)).toMatchObject({ Search: "LAW", OrganizationId: ids.otherOrganization, TypeId: ids.departmentType }),
        );
    });

    it("offers new organizations only to cabinet-wide administrators", async () => {
        mockApi("get", "/api/v1/organization-units/list", makePage([]));
        mockChoices();
        const page = renderPage(<UnitsPage />, { path: "/admin/units", user: organizationAdmin });

        expect(await screen.findByText("Підрозділів не знайдено")).toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Нова організація" })).not.toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Новий підрозділ" })).toHaveAttribute("href", "/admin/units/new");
        page.unmount();

        mockApi("get", "/api/v1/organization-units/list", makePage([]));
        mockChoices();
        renderPage(<UnitsPage />, { path: "/admin/units" });
        expect(await screen.findByRole("link", { name: "Нова організація" })).toBeInTheDocument();
    });
});

describe("UnitEditorPage", () => {
    /**
     * Renders the editor of a new unit.
     * @param view The view of the address (`top=true`, `parent=…`).
     * @param user The signed-in user.
     * @returns The rendered page.
     */
    function renderNew(view = "", user = userWith(Permissions.administrationAll, Permissions.structureManage)) {
        mockChoices();
        return renderPage(<UnitEditorPage />, { path: "/admin/units/new", user, state: view ? { view } : undefined });
    }

    /**
     * Renders the editor of an existing unit.
     * @param unit The unit.
     * @returns The rendered page.
     */
    function renderExisting(unit = makeUnitDetails()) {
        mockApi("get", "/api/v1/organization-units/{id}", unit);
        mockChoices();
        return renderPage(<UnitEditorPage />, { path: "/admin/units/:id", url: `/admin/units/${unit.id}` });
    }

    it("creates a unit under the first organization with a department type by default", async () => {
        const created = mockApi("post", "/api/v1/organization-units", { id: "u-new" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Повна назва/ }), " Кафедра історії ");
        await page.user.type(screen.getByRole("textbox", { name: /Код/ }), "HIST");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        await findToast("Підрозділ збережено");
        expect(await navigatedTo()).toBe("/admin/units");
        expect(created[0]?.body).toEqual({
            code: "HIST",
            name: "Кафедра історії",
            shortName: null,
            typeId: ids.departmentType,
            parentId: ids.organization,
            sortOrder: 0,
            isActive: true,
        });
    });

    it("takes the parent from the address only when it is one of the user's units", async () => {
        const page = renderNew(`parent=${ids.otherOrganization}`);
        expect(await screen.findByRole("combobox", { name: /Батьківський підрозділ/ })).toHaveTextContent("Ліцей");
        page.unmount();

        renderNew("parent=00000000-0000-0000-0000-00000000dead");
        expect(await screen.findByRole("combobox", { name: /Батьківський підрозділ/ })).toHaveTextContent("Університет");
    });

    it("creates a top-level organization with an organization type", async () => {
        const created = mockApi("post", "/api/v1/organization-units", { id: "u-new" });
        const page = renderNew("top=true");

        expect(await screen.findByRole("heading", { name: "Нова організація" })).toBeInTheDocument();
        await page.user.type(screen.getByRole("textbox", { name: /Повна назва/ }), "Коледж");
        await page.user.type(screen.getByRole("textbox", { name: /Код/ }), "COL");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        await waitFor(() => expect(created[0]?.body).toMatchObject({ parentId: null, typeId: ids.organizationType }));
    });

    it("does not let an organization's administrator create a top-level organization", async () => {
        renderNew("top=true", organizationAdmin);

        expect(await screen.findByText(/Організації верхнього рівня створює лише адміністратор усіх організацій/)).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: /Повна назва/ })).toBeDisabled();
    });

    it("checks the name and the code", async () => {
        const created = mockApi("post", "/api/v1/organization-units", { id: "never" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Код/ }), "Каф 1");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Вкажіть назву.")).toBeInTheDocument();
        expect(screen.getByText("Лише латинські літери, цифри, «-» та «_».")).toBeInTheDocument();
        expect(created).toHaveLength(0);
    });

    it("edits a unit and can deactivate it", async () => {
        const updated = mockApi("put", "/api/v1/organization-units/{id}");
        const page = renderExisting();

        expect(await screen.findByRole("textbox", { name: /Повна назва/ })).toHaveValue("Кафедра права");
        await page.user.click(screen.getByRole("checkbox", { name: /Активний/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await navigatedTo()).toBe("/admin/units");
        expect(updated[0]?.body).toEqual({
            code: "LAW",
            name: "Кафедра права",
            shortName: "КП",
            typeId: ids.departmentType,
            parentId: ids.organization,
            sortOrder: 5,
            isActive: false,
        });
    });

    it("shows a unit of an organization the user does not manage read-only", async () => {
        renderExisting(makeUnitDetails({ canManage: false }));

        expect(await screen.findByText(/структурою якої Ви не керуєте/)).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: /Повна назва/ })).toBeDisabled();
    });

    it("reports a refused save", async () => {
        mockApi("put", "/api/v1/organization-units/{id}", () => problem(409, "conflict", "Підрозділ із таким кодом уже існує."));
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Зберегти" }));

        await findToast("Підрозділ із таким кодом уже існує.");
    });
});
