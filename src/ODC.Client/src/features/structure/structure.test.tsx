import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Permissions } from "@/shared/session/session";
import { makePage, makeUser, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { filterTree } from "./components/UnitTree";
import { StructurePage } from "./pages/StructurePage";
import { UnitPage } from "./pages/UnitPage";

/** The type of organizations. */
const organization: Schemas["UnitTypeRef"] = { id: "t-org", name: "Організація", icon: "Building2", isOrganization: true };

/** The type of divisions. */
const division: Schemas["UnitTypeRef"] = { id: "t-div", name: "Відділ", icon: "Users", isOrganization: false };

/**
 * A node of the unit tree.
 * @param id The id (also the code).
 * @param name The name.
 * @param children The nested units.
 * @param overrides Changed fields.
 * @returns The node.
 */
function node(
    id: string,
    name: string,
    children: Schemas["UnitTreeNode"][] = [],
    overrides: Partial<Schemas["UnitTreeNode"]> = {},
): Schemas["UnitTreeNode"] {
    return {
        id,
        code: id.toUpperCase(),
        name,
        shortName: null,
        type: children.length > 0 ? organization : division,
        isActive: true,
        staffCount: 0,
        children,
        ...overrides,
    };
}

/** A tree of two organizations. */
const tree = [
    node("uni", "Університет", [
        node("hr", "Відділ кадрів", [], { staffCount: 4 }),
        node("fin", "Фінансовий відділ", [], { shortName: "ФВ" }),
    ]),
    node("lab", "Лабораторія", [node("lab-it", "Сектор ІТ")]),
];

describe("filterTree", () => {
    it("keeps everything without a search", () => {
        expect(filterTree(tree, "")).toEqual(tree);
    });

    it("keeps a matching unit with its branch and the path to it", () => {
        const found = filterTree(tree, "фв");

        expect(found).toHaveLength(1);
        expect(found[0]?.name).toBe("Університет");
        expect(found[0]?.children.map((child) => child.name)).toEqual(["Фінансовий відділ"]);
    });

    it("matches codes and keeps a matching parent whole", () => {
        expect(filterTree(tree, "lab").map((item) => item.children.length)).toEqual([1]);
        expect(filterTree(tree, "немає такого")).toEqual([]);
    });
});

describe("StructurePage", () => {
    it("shows the organizations with their units, types, staff counts and links", async () => {
        mockApi("get", "/api/v1/organization-units", tree);
        renderPage(<StructurePage />, { path: "/structure", user: makeUser({ permissions: [Permissions.structureView] }) });

        expect(await screen.findByRole("link", { name: "Університет" })).toHaveAttribute("href", "/structure/uni");
        const hr = screen.getByRole("link", { name: "Відділ кадрів" }).closest("li") as HTMLElement;
        expect(within(hr).getByText("4")).toBeInTheDocument();
        expect(within(hr).getByText("Відділ")).toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Керувати структурою" })).not.toBeInTheDocument();
    });

    it("offers management to structure managers and new organizations only to cabinet administrators", async () => {
        mockApi("get", "/api/v1/organization-units", tree);
        const page = renderPage(<StructurePage />, {
            path: "/structure",
            user: makeUser({ permissions: [Permissions.structureView, Permissions.structureManage] }),
        });

        expect(await screen.findByRole("link", { name: "Керувати структурою" })).toHaveAttribute("href", "/admin/units");
        expect(screen.queryByRole("link", { name: "Нова організація" })).not.toBeInTheDocument();

        page.unmount();
        mockApi("get", "/api/v1/organization-units", tree);
        renderPage(<StructurePage />, { path: "/structure" });
        expect(await screen.findByRole("link", { name: "Нова організація" })).toHaveAttribute("href", "/admin/units/new");
    });

    it("filters the tree by the search and says when nothing matches", async () => {
        mockApi("get", "/api/v1/organization-units", tree);
        const page = renderPage(<StructurePage />, { path: "/structure" });
        await screen.findByRole("link", { name: "Університет" });

        await page.user.type(screen.getByRole("searchbox"), "Сектор{Enter}");
        await waitFor(() => expect(screen.queryByRole("link", { name: "Університет" })).not.toBeInTheDocument());
        expect(screen.getByRole("link", { name: "Сектор ІТ" })).toBeInTheDocument();

        await page.user.clear(screen.getByRole("searchbox"));
        await page.user.type(screen.getByRole("searchbox"), "zzz{Enter}");
        expect(await screen.findByText("Підрозділів не знайдено")).toBeInTheDocument();
    });

    it("says when the structure is empty and hides the search", async () => {
        mockApi("get", "/api/v1/organization-units", []);
        renderPage(<StructurePage />, { path: "/structure" });

        expect(await screen.findByText("Структуру ще не заповнено")).toBeInTheDocument();
        expect(screen.queryByRole("searchbox")).not.toBeInTheDocument();
    });

    it("shows the server's message when the tree cannot be loaded", async () => {
        mockApi("get", "/api/v1/organization-units", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<StructurePage />, { path: "/structure" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});

describe("UnitPage", () => {
    /**
     * A unit as the server returns it.
     * @param overrides Changed fields.
     * @returns The unit.
     */
    function makeUnit(overrides: Partial<Schemas["UnitDetails"]> = {}): Schemas["UnitDetails"] {
        return {
            id: "hr",
            code: "HR",
            name: "Відділ кадрів",
            shortName: "ВК",
            type: division,
            isActive: true,
            parentId: "uni",
            sortOrder: 0,
            path: [
                { id: "uni", name: "Університет" },
                { id: "hr", name: "Відділ кадрів" },
            ],
            organization: { id: "uni", name: "Університет" },
            children: [node("hr-1", "Сектор обліку", [], { staffCount: 2 })],
            staffCount: 4,
            canManage: true,
            ...overrides,
        };
    }

    /**
     * A person of the staff list.
     * @param overrides Changed fields.
     * @returns The row.
     */
    function makeStaff(overrides: Partial<Schemas["UnitStaffItem"]> = {}): Schemas["UnitStaffItem"] {
        return {
            userId: "u-1",
            fullName: "Коваленко Марія",
            position: "Начальник відділу",
            email: "maria@example.com",
            phoneNumber: "+380501112233",
            isPrimary: true,
            isLeadership: true,
            photoId: null,
            ...overrides,
        };
    }

    /**
     * Renders the page of a unit.
     * @param unit The unit.
     * @param staff The staff rows.
     * @param user The signed-in user.
     * @returns The rendered page and the staff calls.
     */
    function renderUnit(unit = makeUnit(), staff = [makeStaff()], user = makeUser()) {
        mockApi("get", "/api/v1/organization-units/{id}", unit);
        const calls = mockApi("get", "/api/v1/organization-units/{id}/staff", makePage(staff));
        return { ...renderPage(<UnitPage />, { path: "/structure/:id", url: `/structure/${unit.id}`, user }), calls };
    }

    /**
     * The query of the last staff request.
     * @param calls The recorded calls.
     * @returns The query values.
     */
    function lastQuery(calls: RecordedRequest[]): Record<string, string> {
        return Object.fromEntries(calls.at(-1)?.query ?? []);
    }

    it("shows the unit with its path, organization, nested units and staff", async () => {
        renderUnit(makeUnit(), [
            makeStaff(),
            makeStaff({
                userId: "u-2",
                fullName: "Бондар Олег",
                position: "Інспектор",
                isPrimary: false,
                isLeadership: false,
                phoneNumber: null,
            }),
        ]);

        expect(await screen.findByRole("heading", { name: "Відділ кадрів" })).toBeInTheDocument();
        expect(screen.getAllByRole("link", { name: "Університет" })[0]).toHaveAttribute("href", "/structure/uni");
        expect(screen.getByRole("link", { name: "Організація: Університет" })).toHaveAttribute("href", "/structure/uni");
        expect(screen.getByRole("link", { name: /Сектор обліку/ })).toHaveAttribute("href", "/structure/hr-1");
        expect(screen.getByText("Відділ · працівників: 2")).toBeInTheDocument();
        const leader = (await screen.findByText("Коваленко Марія")).closest("tr") as HTMLElement;
        expect(within(leader).getByText("Керівник")).toBeInTheDocument();
        expect(within(leader).getByRole("link", { name: "+380501112233" })).toHaveAttribute("href", "tel:+380501112233");
        expect(within(leader).getByRole("link", { name: "maria@example.com" })).toHaveAttribute("href", "mailto:maria@example.com");
        const other = screen.getByText("Бондар Олег").closest("tr") as HTMLElement;
        expect(within(other).getByText("(сумісництво)")).toBeInTheDocument();
        expect(within(other).queryByText("Керівник")).not.toBeInTheDocument();
    });

    it("marks an inactive unit and does not link the organization to itself", async () => {
        renderUnit(
            makeUnit({
                id: "uni",
                name: "Університет",
                isActive: false,
                organization: { id: "uni", name: "Університет" },
                path: [{ id: "uni", name: "Університет" }],
                children: [],
            }),
        );

        expect(await screen.findByText("Неактивний")).toBeInTheDocument();
        expect(screen.queryByText("Організація: Університет")).not.toBeInTheDocument();
    });

    it("offers editing only to managers allowed for this unit", async () => {
        const page = renderUnit();
        expect(await screen.findByRole("link", { name: "Редагувати" })).toHaveAttribute("href", "/admin/units/hr");
        expect(screen.getByRole("link", { name: "Додати підрозділ" })).toHaveAttribute("href", "/admin/units/new");

        page.unmount();
        renderUnit(makeUnit({ canManage: false }));
        await screen.findByRole("heading", { name: "Відділ кадрів" });
        expect(screen.queryByRole("link", { name: "Редагувати" })).not.toBeInTheDocument();
    });

    it("hides editing from someone without the structure management permission", async () => {
        renderUnit(makeUnit(), [makeStaff()], makeUser({ permissions: [Permissions.structureView] }));

        await screen.findByRole("heading", { name: "Відділ кадрів" });
        expect(screen.queryByRole("link", { name: "Редагувати" })).not.toBeInTheDocument();
    });

    it("searches the staff from the first page", async () => {
        const { calls, user } = renderUnit(makeUnit(), [makeStaff()]);
        await screen.findByText("Коваленко Марія");

        await user.type(screen.getByRole("searchbox"), "Марія{Enter}");

        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ Search: "Марія", Page: "1", PageSize: "20" }));
    });

    it("says when nobody works in the unit", async () => {
        renderUnit(makeUnit(), []);

        expect(await screen.findByText("Працівників не знайдено")).toBeInTheDocument();
    });

    it("says that a missing unit was not found", async () => {
        mockApi("get", "/api/v1/organization-units/{id}", () => problem(404, "not_found", "Запис не знайдено."));
        renderPage(<UnitPage />, { path: "/structure/:id", url: "/structure/missing" });

        expect(await screen.findByText("Підрозділ не знайдено")).toBeInTheDocument();
    });

    it("shows the server's message for other failures", async () => {
        mockApi("get", "/api/v1/organization-units/{id}", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<UnitPage />, { path: "/structure/:id", url: "/structure/hr" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});
