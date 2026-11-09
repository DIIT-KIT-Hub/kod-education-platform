import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { navigatedTo, renderPage } from "@/test/render";
import { LockoutPage } from "./LockoutPage";

describe("LockoutPage", () => {
    it("explains the temporary lock", async () => {
        renderPage(<LockoutPage />, { path: "/account/lockout", user: null });

        expect(await screen.findByRole("heading", { name: "Забагато спроб" })).toBeInTheDocument();
        expect(screen.getByText(/тимчасово заблоковано на 15 хвилин/)).toBeInTheDocument();
        expect(document.title).toBe("Вхід тимчасово заблоковано · КОД");
    });

    it("leads back to the sign-in page", async () => {
        const { user } = renderPage(<LockoutPage />, { path: "/account/lockout", user: null });

        const link = await screen.findByRole("link", { name: "До сторінки входу" });
        expect(link).toHaveAttribute("href", "/account/login");
        await user.click(link);

        expect(await navigatedTo()).toBe("/account/login");
    });
});
