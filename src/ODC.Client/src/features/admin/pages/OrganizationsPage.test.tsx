import { screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { answerConfirm, clearToastsAfterEach, findToast } from "../testing";
import { OrganizationsPage } from "./OrganizationsPage";

clearToastsAfterEach();

/**
 * An organization of the registry.
 * @param overrides The fields to change.
 * @returns The organization.
 */
function makeOrganization(overrides: Partial<Schemas["OrganizationListItem"]> = {}): Schemas["OrganizationListItem"] {
    return {
        id: "o-1",
        code: "ORG",
        name: "Перша організація",
        shortName: "ПО",
        state: "Active",
        databaseName: "odc_org_org",
        schemaVersion: "20261111213522_InitialCreate",
        isCurrent: true,
        migratedAt: "2026-11-11T21:40:00Z",
        createdAt: "2026-11-11T21:40:00Z",
        memberCount: 12,
        ownRoles: true,
        ...overrides,
    };
}

/** The fund: suspended. */
const fund = makeOrganization({ id: "o-2", code: "FUND", name: "Фонд", shortName: null, state: "Suspended", memberCount: 3 });

/**
 * The row of an organization.
 * @param name Its name.
 * @returns The row.
 */
async function rowOf(name: string): Promise<HTMLElement> {
    return (await screen.findByText(name)).closest("tr") as HTMLElement;
}

describe("OrganizationsPage", () => {
    it("lists the organizations with their state, database and people", async () => {
        mockApi("get", "/api/v1/organizations", [makeOrganization({ isCurrent: false }), fund]);
        renderPage(<OrganizationsPage />, { path: "/admin/organizations" });

        const org = await rowOf("Перша організація");
        expect(within(org).getByText("Працює")).toBeInTheDocument();
        expect(within(org).getByText("База не оновлена")).toBeInTheDocument();
        expect(within(org).getByText("odc_org_org")).toBeInTheDocument();
        expect(within(org).getByText("12")).toBeInTheDocument();
        expect(within(org).getByRole("button", { name: "Призупинити" })).toBeInTheDocument();
        expect(within(org).queryByRole("button", { name: "Видалити" })).not.toBeInTheDocument();
        const suspended = await rowOf("Фонд");
        expect(within(suspended).getByText("Призупинена")).toBeInTheDocument();
        expect(within(suspended).getByRole("button", { name: "Відновити" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Нова організація" })).toHaveAttribute("href", "/admin/units/new");
    });

    it("suspends an organization after a confirmation", async () => {
        mockApi("get", "/api/v1/organizations", [makeOrganization()]);
        const suspended = mockApi("post", "/api/v1/organizations/{id}/suspend", 204);
        const page = renderPage(<OrganizationsPage />, { path: "/admin/organizations" });

        await page.user.click(within(await rowOf("Перша організація")).getByRole("button", { name: "Призупинити" }));
        expect(await answerConfirm(page.user, "Призупинити")).toMatch(/^Призупинити «Перша організація»\?/);

        await findToast("Організацію «Перша організація» призупинено.");
        expect(suspended[0]?.path).toBe("/api/v1/organizations/o-1/suspend");
        expect(suspended[0]?.headers.get("x-csrf-token")).toBeTruthy();
    });

    it("resumes a suspended organization and rotates the passwords of a database", async () => {
        mockApi("get", "/api/v1/organizations", [fund]);
        const resumed = mockApi("post", "/api/v1/organizations/{id}/resume", 204);
        const rotated = mockApi("post", "/api/v1/organizations/{id}/rotate-passwords", 204);
        const page = renderPage(<OrganizationsPage />, { path: "/admin/organizations" });

        await page.user.click(within(await rowOf("Фонд")).getByRole("button", { name: "Відновити" }));
        await findToast("Організація «Фонд» знову працює.");
        expect(resumed).toHaveLength(1);

        await page.user.click(within(await rowOf("Фонд")).getByRole("button", { name: "Нові паролі бази" }));
        await answerConfirm(page.user, "Змінити паролі");
        await findToast("Паролі бази «Фонд» змінено.");
        expect(rotated[0]?.path).toBe("/api/v1/organizations/o-2/rotate-passwords");
    });

    it("deletes a suspended organization only after its code is typed", async () => {
        mockApi("get", "/api/v1/organizations", [fund]);
        const deleted = mockApi("delete", "/api/v1/organizations/{id}", 204);
        const page = renderPage(<OrganizationsPage />, { path: "/admin/organizations" });

        await page.user.click(within(await rowOf("Фонд")).getByRole("button", { name: "Видалити" }));
        const dialog = (await screen.findByRole("heading", { name: "Видалити організацію назавжди?" })).closest("dialog") as HTMLElement;
        const confirmButton = within(dialog).getByRole("button", { name: "Видалити назавжди" });
        expect(confirmButton).toBeDisabled();
        await page.user.type(within(dialog).getByRole("textbox", { name: /Введіть код організації/ }), "fun");
        expect(confirmButton).toBeDisabled();
        await page.user.type(within(dialog).getByRole("textbox", { name: /Введіть код організації/ }), "d");
        await page.user.click(confirmButton);

        await findToast("Організацію «Фонд» видалено.");
        expect(deleted[0]?.path).toBe("/api/v1/organizations/o-2");
        expect(deleted[0]?.query.get("confirmation")).toBe("fund");
    });

    it("shows the server's refusal", async () => {
        mockApi("get", "/api/v1/organizations", [makeOrganization()]);
        mockApi("post", "/api/v1/organizations/{id}/suspend", () =>
            problem(400, "validation", "Не можна призупинити останню активну організацію."),
        );
        const page = renderPage(<OrganizationsPage />, { path: "/admin/organizations" });

        await page.user.click(within(await rowOf("Перша організація")).getByRole("button", { name: "Призупинити" }));
        await answerConfirm(page.user, "Призупинити");

        await findToast("Не можна призупинити останню активну організацію.");
    });
});
