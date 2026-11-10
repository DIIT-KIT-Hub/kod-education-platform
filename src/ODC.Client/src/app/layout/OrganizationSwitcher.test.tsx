import { screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { choose } from "@/features/admin/testing";
import { authContextQuery } from "@/shared/session/session";
import { makeAuthContext, makeOrganization } from "@/test/factories";
import { renderPage } from "@/test/render";
import { OrganizationSwitcher } from "./OrganizationSwitcher";

describe("OrganizationSwitcher", () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("shows the organization the cabinet works in", async () => {
        renderPage(<OrganizationSwitcher />);

        expect(await screen.findByText("Організація")).toBeInTheDocument();
        expect(screen.getByText("МО")).toHaveAttribute("title", "Моя організація");
        expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
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

        await choose(user, await screen.findByRole("combobox", { name: "Перейти до іншої організації" }), "Благодійний фонд · гість");

        expect(assign).toHaveBeenCalledWith("/o/FUND/");
    });

    it("shows only an icon on the collapsed sidebar", () => {
        const { container } = renderPage(<OrganizationSwitcher rail />);

        expect(container.querySelector("[title='Моя організація']")).not.toBeNull();
        expect(screen.queryByText("Організація")).not.toBeInTheDocument();
    });
});
