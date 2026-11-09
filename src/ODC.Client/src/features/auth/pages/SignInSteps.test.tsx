import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makeAuthContext, makeUser } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { ChangePasswordPage } from "./ChangePasswordPage";
import { RecoveryPage } from "./RecoveryPage";
import { TwoFactorPage } from "./TwoFactorPage";

/**
 * Mocks the reload of the auth context after a sign-in step.
 * @returns The recorded calls.
 */
function mockSignedInContext() {
    return mockApi("get", "/api/v1/auth/context", makeAuthContext(makeUser()));
}

describe("TwoFactorPage", () => {
    it("signs in with the code, keeping 'remember me', and opens the page the user wanted", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/two-factor", 204);
        mockSignedInContext();
        const { user } = renderPage(<TwoFactorPage />, {
            path: "/account/login-2fa",
            url: "/account/login-2fa?returnUrl=%2Fdocuments%2F42&rememberMe=1",
            user: null,
        });

        await user.type(await screen.findByLabelText("Код підтвердження"), "123456");

        expect(await navigatedTo()).toBe("/documents/42");
        expect(calls[0]?.body).toEqual({ code: "123456", rememberMe: true });
    });

    it("never follows a return address to another site", async () => {
        mockApi("post", "/api/v1/auth/login/two-factor", 204);
        mockSignedInContext();
        const { user } = renderPage(<TwoFactorPage />, {
            path: "/account/login-2fa",
            url: "/account/login-2fa?returnUrl=https%3A%2F%2Fevil.example.com",
            user: null,
            routes: [{ path: "/", element: <p>home</p> }],
        });

        await user.type(await screen.findByLabelText("Код підтвердження"), "123456");

        expect(await screen.findByText("home")).toBeInTheDocument();
    });

    it("leads to the recovery code step with the same return address and choice", async () => {
        renderPage(<TwoFactorPage />, {
            path: "/account/login-2fa",
            url: "/account/login-2fa?returnUrl=%2Fchats&rememberMe=1",
            user: null,
        });

        expect(await screen.findByRole("link", { name: "Увійти за резервним кодом" })).toHaveAttribute(
            "href",
            "/account/login-recovery?returnUrl=%2Fchats&rememberMe=1",
        );
    });
});

describe("RecoveryPage", () => {
    /**
     * Renders the recovery code step.
     * @param url The opened address.
     * @returns The rendered page.
     */
    function renderRecovery(url = "/account/login-recovery?returnUrl=%2Fprofile") {
        return renderPage(<RecoveryPage />, { path: "/account/login-recovery", url, user: null });
    }

    it("signs in with a trimmed recovery code and opens the page the user wanted", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/recovery", 204);
        const context = mockSignedInContext();
        const { user } = renderRecovery();

        await user.type(await screen.findByRole("textbox", { name: "Резервний код" }), "  ABCD-1234  ");
        await user.click(screen.getByRole("button", { name: "Увійти" }));

        expect(await navigatedTo()).toBe("/profile");
        expect(calls[0]?.body).toEqual({ code: "ABCD-1234", rememberMe: false });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(context).toHaveLength(1);
    });

    it("asks for a code and refuses an overlong one", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/recovery", 204);
        const { user } = renderRecovery();

        await user.click(await screen.findByRole("button", { name: "Увійти" }));
        expect(await screen.findByText("Введіть резервний код.")).toBeInTheDocument();

        await user.type(screen.getByRole("textbox", { name: "Резервний код" }), "x".repeat(33));
        await user.click(screen.getByRole("button", { name: "Увійти" }));
        expect(await screen.findByText("Резервний код задовгий.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("reports a wrong code and stays on the step", async () => {
        mockApi("post", "/api/v1/auth/login/recovery", () => problem(400, "invalid_code", "Резервний код недійсний."));
        const { user } = renderRecovery();

        await user.type(await screen.findByRole("textbox", { name: "Резервний код" }), "WRONG-0000");
        await user.click(screen.getByRole("button", { name: "Увійти" }));

        expect(await screen.findByText("Резервний код недійсний.")).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: "Резервний код" })).toBeInTheDocument();
    });

    it("leads a locked-out account to the lockout page", async () => {
        mockApi("post", "/api/v1/auth/login/recovery", () => problem(423, "locked_out", "Обліковий запис тимчасово заблоковано."));
        const { user } = renderRecovery();

        await user.type(await screen.findByRole("textbox", { name: "Резервний код" }), "ABCD-1234");
        await user.click(screen.getByRole("button", { name: "Увійти" }));

        expect(await navigatedTo()).toBe("/account/lockout");
    });
});

describe("ChangePasswordPage", () => {
    /**
     * Renders the password change page of a signed-in user.
     * @returns The rendered page.
     */
    function renderChange() {
        return renderPage(<ChangePasswordPage />, { path: "/account/change-password" });
    }

    /**
     * Fills the three password fields.
     * @param page The rendered page.
     * @param current The current password.
     * @param next The new password.
     * @param again The repeated new password.
     */
    async function fill(page: ReturnType<typeof renderChange>, current: string, next: string, again: string) {
        if (current) {
            await page.user.type(screen.getByLabelText("Поточний пароль"), current);
        }

        if (next) {
            await page.user.type(screen.getByLabelText("Новий пароль"), next);
        }

        if (again) {
            await page.user.type(screen.getByLabelText("Повторіть новий пароль"), again);
        }

        await page.user.click(screen.getByRole("button", { name: "Зберегти пароль" }));
    }

    it("changes the password, reloads the session and returns to the security page", async () => {
        const calls = mockApi("post", "/api/v1/auth/password", 204);
        const context = mockSignedInContext();
        const page = renderChange();

        await fill(page, "Old-Password-1", "New-Password-2026", "New-Password-2026");

        expect(await screen.findByText("Пароль змінено.")).toBeInTheDocument();
        expect(await navigatedTo()).toBe("/profile/security");
        expect(calls[0]?.body).toEqual({ currentPassword: "Old-Password-1", newPassword: "New-Password-2026" });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(context).toHaveLength(1);
    });

    it("asks for both passwords", async () => {
        const calls = mockApi("post", "/api/v1/auth/password", 204);
        const page = renderChange();

        await fill(page, "", "", "");

        expect(await screen.findByText("Вкажіть поточний пароль.")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть новий пароль.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("refuses a short password and a repetition that differs", async () => {
        const calls = mockApi("post", "/api/v1/auth/password", 204);
        const page = renderChange();

        await fill(page, "Old-Password-1", "Short-1", "Short-2");

        expect(await screen.findByText("Пароль має містити від 12 до 128 символів.")).toBeInTheDocument();
        expect(screen.getByText("Паролі не збігаються.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("reports the server's refusal and stays on the form", async () => {
        mockApi("post", "/api/v1/auth/password", () => problem(400, "identity", "Неправильний поточний пароль."));
        const page = renderChange();

        await fill(page, "Wrong-Password-1", "New-Password-2026", "New-Password-2026");

        expect(await screen.findByText("Неправильний поточний пароль.")).toBeInTheDocument();
        await waitFor(() => expect(screen.getByRole("button", { name: "Зберегти пароль" })).toBeEnabled());
    });

    it("leads back to the security page", () => {
        renderChange();

        expect(screen.getByRole("link", { name: "Повернутися" })).toHaveAttribute("href", "/profile/security");
    });
});
