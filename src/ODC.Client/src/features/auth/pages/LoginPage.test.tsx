import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { mockApi, problem } from "@/test/server";
import { navigatedTo, renderPage } from "@/test/render";
import { LoginPage } from "./LoginPage";

/**
 * Renders the sign-in page for a visitor.
 * @param url The address (with a return address).
 * @returns The rendered page.
 */
function renderLogin(url = "/account/login") {
    return renderPage(<LoginPage />, { path: "/account/login", url, user: null });
}

describe("LoginPage", () => {
    it("asks for the e-mail and the password before calling the server", async () => {
        const calls = mockApi("post", "/api/v1/auth/login", { next: "two-factor" });
        const { user } = renderLogin();

        await user.click(await screen.findByRole("button", { name: "Продовжити" }));

        expect(await screen.findByText("Вкажіть електронну пошту.")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть пароль.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("rejects an address that is not an e-mail", async () => {
        const { user } = renderLogin();

        await user.type(await screen.findByLabelText("Електронна пошта"), "not-an-email");
        await user.type(screen.getByLabelText("Пароль"), "secret");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await screen.findByText("Некоректна адреса електронної пошти.")).toBeInTheDocument();
    });

    it("sends the credentials with the antiforgery token and opens the second factor step", async () => {
        const calls = mockApi("post", "/api/v1/auth/login", { next: "two-factor" });
        const { user } = renderLogin("/account/login?returnUrl=%2Fdocuments");

        await user.type(await screen.findByLabelText("Електронна пошта"), "  admin@odc.local ");
        await user.type(screen.getByLabelText("Пароль"), "Correct-horse-1");
        await user.click(screen.getByRole("checkbox"));
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await navigatedTo()).toBe("/account/login-2fa?returnUrl=%2Fdocuments&rememberMe=1");
        expect(calls).toHaveLength(1);
        expect(calls[0]?.body).toEqual({ email: "admin@odc.local", password: "Correct-horse-1", rememberMe: true });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("leads a user without an authenticator to the enrolment", async () => {
        mockApi("post", "/api/v1/auth/login", { next: "setup" });
        const { user } = renderLogin();

        await user.type(await screen.findByLabelText("Електронна пошта"), "new@odc.local");
        await user.type(screen.getByLabelText("Пароль"), "Correct-horse-1");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await navigatedTo()).toBe("/account/setup-authenticator");
    });

    it("ignores a return address that leads to another site", async () => {
        mockApi("post", "/api/v1/auth/login", { next: "two-factor" });
        const { user } = renderLogin("/account/login?returnUrl=https%3A%2F%2Fevil.example%2F");

        await user.type(await screen.findByLabelText("Електронна пошта"), "admin@odc.local");
        await user.type(screen.getByLabelText("Пароль"), "Correct-horse-1");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await navigatedTo()).toBe("/account/login-2fa");
    });

    it("shows the server's message for wrong credentials and stays on the page", async () => {
        mockApi("post", "/api/v1/auth/login", () => problem(400, "invalid_credentials", "Невірна пошта або пароль."));
        const { user } = renderLogin();

        await user.type(await screen.findByLabelText("Електронна пошта"), "admin@odc.local");
        await user.type(screen.getByLabelText("Пароль"), "wrong");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await screen.findByText("Невірна пошта або пароль.")).toBeInTheDocument();
        expect(screen.queryByTestId("location")).not.toBeInTheDocument();
    });

    it("opens the lockout page when the account is locked", async () => {
        mockApi("post", "/api/v1/auth/login", () => problem(423, "locked_out", "Обліковий запис тимчасово заблоковано."));
        const { user } = renderLogin();

        await user.type(await screen.findByLabelText("Електронна пошта"), "admin@odc.local");
        await user.type(screen.getByLabelText("Пароль"), "wrong");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await navigatedTo()).toBe("/account/lockout");
    });

    it("says the server cannot be reached when the request fails", async () => {
        mockApi("post", "/api/v1/auth/login", () => Response.error());
        const { user } = renderLogin();

        await user.type(await screen.findByLabelText("Електронна пошта"), "admin@odc.local");
        await user.type(screen.getByLabelText("Пароль"), "Correct-horse-1");
        await user.click(screen.getByRole("button", { name: "Продовжити" }));

        expect(await screen.findByText(/Не вдалося зв'язатися з сервером/)).toBeInTheDocument();
    });

    it("links to the account activation", async () => {
        renderLogin();

        expect(await screen.findByRole("link", { name: /Активувати обліковий запис/ })).toHaveAttribute("href", "/account/activate");
    });
});
