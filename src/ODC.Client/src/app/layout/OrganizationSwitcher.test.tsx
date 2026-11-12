import { screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { choose } from "@/features/admin/testing";
import { authContextQuery, Permissions } from "@/shared/session/session";
import { makeAuthContext, makeOrganization, makeUser } from "@/test/factories";
import { renderPage } from "@/test/render";
import { OrganizationSwitcher } from "./OrganizationSwitcher";

describe("OrganizationSwitcher", () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("shows the organization the cabinet works in with its short name and code", async () => {
        renderPage(<OrganizationSwitcher />, { user: makeUser({ permissions: [Permissions.documentsView] }) });

        const bar = await screen.findByRole("region", { name: "Поточна організація" });
        expect(within(bar).getByText("Моя організація")).toBeInTheDocument();
        expect(within(bar).getByText("МО")).toBeInTheDocument();
        expect(within(bar).getByText("ORG")).toBeInTheDocument();
        expect(within(bar).getByText("Ви тут працюєте")).toBeInTheDocument();
        expect(within(bar).queryByRole("combobox")).not.toBeInTheDocument();
        expect(within(bar).queryByRole("link", { name: /Усі організації/ })).not.toBeInTheDocument();
    });

    it("switches to another organization by loading the cabinet at its address", async () => {
        const assign = vi.spyOn(window.location, "assign").mockImplementation(() => undefined);
        const { user, queryClient } = renderPage(<OrganizationSwitcher />);
        const context = makeAuthContext();
        queryClient.setQueryData(authContextQuery.queryKey, {
            ...context,
            organizations: [
                makeOrganization(),
                makeOrganization({ id: "f", code: "FUND", name: "Благодійний фонд", shortName: null, access: "Guest" }),
            ],
        });

        await choose(
            user,
            await screen.findByRole("combobox", { name: "Перейти до іншої організації" }),
            "Благодійний фонд · Гість · FUND",
        );

        expect(assign).toHaveBeenCalledWith("/o/FUND/");
    });

    it("leads a platform administrator to the registry of all organizations", async () => {
        renderPage(<OrganizationSwitcher />, { user: makeUser({ permissions: [Permissions.administrationAll] }) });

        expect(await screen.findByRole("link", { name: /Усі організації/ })).toHaveAttribute("href", "/admin/organizations");
    });
});
