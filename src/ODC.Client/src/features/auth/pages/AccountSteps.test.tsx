import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makeAuthContext, makeUser } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { PhotoPage } from "./PhotoPage";
import { SetupAuthenticatorPage } from "./SetupAuthenticatorPage";

describe("PhotoPage", () => {
    it("requires a photo before the cabinet and lets the user sign out instead", async () => {
        const logout = mockApi("post", "/api/v1/auth/logout", 204);
        mockApi("get", "/api/v1/auth/context", makeAuthContext(null));
        const { user } = renderPage(<PhotoPage />, { path: "/account/photo", user: makeUser({ photoId: null }) });

        expect(screen.getByRole("heading", { name: "Додайте фото профілю" })).toBeInTheDocument();
        expect(screen.getByText(/Без фото кабінетом користуватися не можна/)).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Зберегти фото" })).toBeDisabled();
        await user.click(screen.getByRole("button", { name: "Вийти" }));

        expect(await navigatedTo()).toBe("/account/login");
        expect(logout[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("lets a user with a photo replace it and go back to the profile", () => {
        renderPage(<PhotoPage />, { path: "/account/photo" });

        expect(screen.getByRole("heading", { name: "Фото профілю" })).toBeInTheDocument();
        expect(screen.getByText("Нове фото замінить поточне всюди в кабінеті.")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Повернутися" })).toHaveAttribute("href", "/profile");
        expect(screen.queryByRole("button", { name: "Вийти" })).not.toBeInTheDocument();
    });
});

describe("SetupAuthenticatorPage", () => {
    it("links a new authenticator for a sign-in that waits for it", async () => {
        mockApi("get", "/api/v1/auth/login/pending", { twoFactorEnabled: false });
        mockApi("get", "/api/v1/auth/login/authenticator", {
            sharedKey: "JBSWY3DPEHPK3PXP",
            formattedKey: "jbsw y3dp ehpk 3pxp",
            qrCodeSvg: '<svg xmlns="http://www.w3.org/2000/svg"/>',
        });
        renderPage(<SetupAuthenticatorPage />, { path: "/account/setup-authenticator", user: null });

        expect(await screen.findByText("jbsw y3dp ehpk 3pxp")).toBeInTheDocument();
    });

    it("leads back to the sign-in page when no sign-in waits for an authenticator", async () => {
        mockApi("get", "/api/v1/auth/login/pending", () => problem(401, "unauthorized", "Спочатку увійдіть з паролем."));
        renderPage(<SetupAuthenticatorPage />, { path: "/account/setup-authenticator", user: null });

        expect(await screen.findByText("Спочатку увійдіть з паролем.")).toBeInTheDocument();
        await waitFor(() => expect(screen.getByRole("link", { name: "До сторінки входу" })).toHaveAttribute("href", "/account/login"));
    });
});
