import { screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Permissions } from "@/shared/session/session";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { answerConfirm, clearToastsAfterEach, findToast, ids, makeUnitType, makeUnitTypes, userWith } from "../testing";
import { UnitTypeEditorPage } from "./UnitTypeEditorPage";
import { UnitTypesPage } from "./UnitTypesPage";

clearToastsAfterEach();

/** An administrator of one organization (structure rights, no cabinet-wide rights). */
const organizationAdmin = userWith(Permissions.structureView, Permissions.structureManage);

describe("UnitTypesPage", () => {
    it("lists the types with their level and use, editable for a cabinet-wide administrator", async () => {
        mockApi("get", "/api/v1/unit-types", makeUnitTypes());
        renderPage(<UnitTypesPage />, { path: "/admin/unit-types" });

        const department = (await screen.findByRole("link", { name: /^Кафедра/ })).closest("tr") as HTMLElement;
        expect(within(department).getByText("Підрозділ")).toBeInTheDocument();
        expect(within(department).getByText("4")).toBeInTheDocument();
        expect(within(department).getByRole("link", { name: "Редагувати" })).toHaveAttribute(
            "href",
            `/admin/unit-types/${ids.departmentType}`,
        );
        const organization = screen.getByRole("link", { name: /^Заклад/ }).closest("tr") as HTMLElement;
        expect(within(organization).getByText("Організація")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Новий тип" })).toHaveAttribute("href", "/admin/unit-types/new");
    });

    it("shows the shared catalog read-only to an organization's administrator", async () => {
        mockApi("get", "/api/v1/unit-types", makeUnitTypes());
        renderPage(<UnitTypesPage />, { path: "/admin/unit-types", user: organizationAdmin });

        expect(await screen.findAllByRole("link", { name: "Переглянути" })).toHaveLength(2);
        expect(screen.queryByRole("link", { name: "Новий тип" })).not.toBeInTheDocument();
        expect(screen.getByText(/Каталог типів спільний для всіх організацій кабінету/)).toBeInTheDocument();
    });

    it("shows the server's message when the catalog cannot be loaded", async () => {
        mockApi("get", "/api/v1/unit-types", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<UnitTypesPage />, { path: "/admin/unit-types" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});

describe("UnitTypeEditorPage", () => {
    /**
     * Renders the editor of an existing type.
     * @param type The type.
     * @param user The signed-in user.
     * @returns The rendered page.
     */
    function renderExisting(type = makeUnitType(), user = userWith(Permissions.administrationAll, Permissions.structureManage)) {
        mockApi("get", "/api/v1/unit-types/{id}", type);
        return renderPage(<UnitTypeEditorPage />, { path: "/admin/unit-types/:id", url: `/admin/unit-types/${type.id}`, user });
    }

    it("creates a type with its code, icon, level and order", async () => {
        const created = mockApi("post", "/api/v1/unit-types", { id: "t-new" });
        const page = renderPage(<UnitTypeEditorPage />, { path: "/admin/unit-types/new" });

        await page.user.type(await screen.findByRole("textbox", { name: /Назва/ }), "Цех");
        await page.user.type(screen.getByRole("textbox", { name: /Код/ }), "WORKSHOP");
        await page.user.click(screen.getByRole("radio", { name: "flask-conical" }));
        await page.user.click(screen.getByRole("checkbox", { name: /Це організація/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        await findToast("Тип підрозділу збережено");
        expect(await navigatedTo()).toBe("/admin/unit-types");
        expect(created[0]?.body).toEqual({
            name: "Цех",
            code: "WORKSHOP",
            description: null,
            icon: "flask-conical",
            isOrganization: true,
            sortOrder: 100,
        });
    });

    it("checks the name and the code", async () => {
        const created = mockApi("post", "/api/v1/unit-types", { id: "never" });
        const page = renderPage(<UnitTypeEditorPage />, { path: "/admin/unit-types/new" });

        await page.user.type(await screen.findByRole("textbox", { name: /Код/ }), "цех №1");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Вкажіть назву.")).toBeInTheDocument();
        expect(screen.getByText("Лише латинські літери, цифри, «-» та «_».")).toBeInTheDocument();
        expect(created).toHaveLength(0);
    });

    it("edits a type and keeps its values", async () => {
        const updated = mockApi("put", "/api/v1/unit-types/{id}");
        const page = renderExisting();

        const name = await screen.findByRole("textbox", { name: /Назва/ });
        expect(screen.getByRole("radio", { name: "graduation-cap" })).toBeChecked();
        await page.user.clear(name);
        await page.user.type(name, "Кафедра (навчальна)");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await navigatedTo()).toBe("/admin/unit-types");
        expect(updated[0]?.body).toEqual({
            name: "Кафедра (навчальна)",
            code: "DEPARTMENT",
            description: "Навчальний підрозділ",
            icon: "graduation-cap",
            isOrganization: false,
            sortOrder: 20,
        });
    });

    it("cannot delete a type that units use", async () => {
        renderExisting(makeUnitType({ unitCount: 4 }));

        expect(await screen.findByRole("button", { name: "Видалити тип" })).toBeDisabled();
        expect(screen.getByText("Тип можна видалити, коли жоден підрозділ його не має.")).toBeInTheDocument();
    });

    it("deletes an unused type after confirmation", async () => {
        const deleted = mockApi("delete", "/api/v1/unit-types/{id}");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Видалити тип" }));
        expect(await answerConfirm(page.user, "Видалити")).toBe("Видалити тип підрозділу?");

        await findToast("Тип підрозділу видалено");
        expect(await navigatedTo()).toBe("/admin/unit-types");
        expect(deleted).toHaveLength(1);
    });

    it("shows the type read-only to an organization's administrator", async () => {
        renderExisting(makeUnitType(), organizationAdmin);

        expect(await screen.findByRole("textbox", { name: /Назва/ })).toBeDisabled();
        expect(screen.getByRole("textbox", { name: /Код/ })).toBeDisabled();
    });

    it("reports a refused save", async () => {
        mockApi("put", "/api/v1/unit-types/{id}", () => problem(409, "conflict", "Тип із таким кодом уже існує."));
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Зберегти" }));

        await findToast("Тип із таким кодом уже існує.");
    });
});
