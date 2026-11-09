import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makeAuthContext, makeUser } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { ActivationPage } from "./ActivationPage";

/**
 * Renders the activation page of a visitor whose browser has no activation and no pending sign-in.
 * @returns The rendered page.
 */
function renderFresh() {
    mockApi("get", "/api/v1/auth/activation", () => problem(404, "no_activation", "Активацію не розпочато."));
    mockApi("get", "/api/v1/auth/login/pending", () => problem(404, "not_found"));
    return renderPage(<ActivationPage />, { path: "/account/activate", user: null });
}

/**
 * Sends the e-mail of the first step and waits for the code step.
 * @param page The rendered page.
 * @param resendAfterSeconds The wait before another code.
 * @returns The recorded start calls.
 */
async function startWith(page: ReturnType<typeof renderFresh>, resendAfterSeconds = 60) {
    const calls = mockApi("post", "/api/v1/auth/activation", { stage: "Code", email: "ivan@example.com", resendAfterSeconds });
    await page.user.type(await screen.findByRole("textbox", { name: "Електронна пошта" }), " Ivan@Example.com ");
    await page.user.click(screen.getByRole("button", { name: "Надіслати код" }));
    await screen.findByRole("heading", { name: "Введіть код із листа" });
    return calls;
}

describe("ActivationPage", () => {
    it("starts with the e-mail step and asks for a valid address", async () => {
        const page = renderFresh();

        expect(await screen.findByRole("heading", { name: "Активація облікового запису" })).toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Надіслати код" }));
        expect(await screen.findByText("Вкажіть електронну пошту.")).toBeInTheDocument();

        await page.user.type(screen.getByRole("textbox", { name: "Електронна пошта" }), "не пошта");
        await page.user.click(screen.getByRole("button", { name: "Надіслати код" }));
        expect(await screen.findByText("Некоректна адреса електронної пошти.")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Увійдіть з паролем" })).toHaveAttribute("href", "/account/login");
    });

    it("sends a code and waits before another one can be asked for", async () => {
        const page = renderFresh();

        const calls = await startWith(page);

        expect(calls[0]?.body).toEqual({ email: "Ivan@Example.com" });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(screen.getByText("ivan@example.com")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: /Надіслати ще раз через 1:00|Надіслати ще раз через 0:5\d/ })).toBeDisabled();
    });

    it("sends another code once the wait is over", async () => {
        const page = renderFresh();
        const calls = await startWith(page, 0);

        await page.user.click(screen.getByRole("button", { name: "Надіслати код ще раз" }));

        await waitFor(() => expect(calls).toHaveLength(2));
        expect(calls[1]?.body).toEqual({ email: "ivan@example.com" });
        expect(await screen.findByText("Новий код надіслано.")).toBeInTheDocument();
    });

    it("reports a wrong code and lets the user try again", async () => {
        const page = renderFresh();
        await startWith(page);
        mockApi("post", "/api/v1/auth/activation/code", () => problem(400, "activation_invalid", "Невірний код."));

        await page.user.type(screen.getByRole("textbox", { name: "Код із листа" }), "111111");

        expect(await screen.findByText("Невірний код.")).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: "Код із листа" })).toHaveValue("");
    });

    it("says an expired code no longer works and offers only a new one", async () => {
        const page = renderFresh();
        await startWith(page, 0);
        mockApi("post", "/api/v1/auth/activation/code", () => problem(400, "activation_expired", "Код прострочено."));

        await page.user.type(screen.getByRole("textbox", { name: "Код із листа" }), "111111");

        expect(await screen.findByText("Цей код більше не діє. Надішліть новий — попередні коди перестануть діяти.")).toBeInTheDocument();
        expect(screen.queryByRole("textbox", { name: "Код із листа" })).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Надіслати код ще раз" })).toBeEnabled();
    });

    it("starts over when the server no longer knows the activation", async () => {
        const page = renderFresh();
        await startWith(page);
        mockApi("post", "/api/v1/auth/activation/code", () => problem(404, "no_activation", "Почніть активацію спочатку."));
        mockApi("delete", "/api/v1/auth/activation");

        await page.user.type(screen.getByRole("textbox", { name: "Код із листа" }), "111111");

        expect(await screen.findByText("Почніть активацію спочатку.")).toBeInTheDocument();
        expect(await screen.findByRole("button", { name: "Надіслати код" })).toBeInTheDocument();
    });

    it("lets the user change the e-mail, dropping the activation on the server", async () => {
        const page = renderFresh();
        await startWith(page);
        const cancelled = mockApi("delete", "/api/v1/auth/activation");

        await page.user.click(screen.getByRole("button", { name: "Змінити пошту" }));

        expect(await screen.findByRole("button", { name: "Надіслати код" })).toBeInTheDocument();
        expect(cancelled).toHaveLength(1);
    });

    it("greets the user after the right code and checks the new password", async () => {
        const page = renderFresh();
        await startWith(page);
        const verified = mockApi("post", "/api/v1/auth/activation/code", { firstName: "Іван", hasAuthenticator: false, hasPhoto: false });
        const saved = mockApi("post", "/api/v1/auth/activation/password", { next: "setup" });

        await page.user.type(screen.getByRole("textbox", { name: "Код із листа" }), "482913");
        expect(await screen.findByRole("heading", { name: "Вітаємо, Іван!" })).toBeInTheDocument();
        expect(verified[0]?.body).toEqual({ code: "482913" });

        await page.user.type(screen.getByLabelText("Новий пароль"), "Short-1");
        await page.user.type(screen.getByLabelText("Повторіть пароль"), "Short-2");
        await page.user.click(screen.getByRole("button", { name: "Зберегти пароль" }));
        expect(await screen.findByText("Пароль має містити від 12 до 128 символів.")).toBeInTheDocument();
        expect(screen.getByText("Паролі не збігаються.")).toBeInTheDocument();
        expect(saved).toHaveLength(0);
    });

    it("saves the password and continues with linking an authenticator", async () => {
        mockApi("get", "/api/v1/auth/activation", { stage: "Password", email: "ivan@example.com", resendAfterSeconds: 0 });
        const saved = mockApi("post", "/api/v1/auth/activation/password", { next: "setup" });
        mockApi("get", "/api/v1/auth/login/authenticator", {
            sharedKey: "JBSWY3DPEHPK3PXP",
            formattedKey: "jbsw y3dp ehpk 3pxp",
            qrCodeSvg: '<svg xmlns="http://www.w3.org/2000/svg"/>',
        });
        const page = renderPage(<ActivationPage />, { path: "/account/activate", user: null });

        expect(await screen.findByRole("heading", { name: "Придумайте пароль" })).toBeInTheDocument();
        await page.user.type(screen.getByLabelText("Новий пароль"), "New-Password-2026");
        await page.user.type(screen.getByLabelText("Повторіть пароль"), "New-Password-2026");
        await page.user.click(screen.getByRole("button", { name: "Зберегти пароль" }));

        await waitFor(() => expect(saved[0]?.body).toEqual({ newPassword: "New-Password-2026" }));
        expect(await screen.findByText("jbsw y3dp ehpk 3pxp")).toBeInTheDocument();
    });

    it("asks for the code of an authenticator that is already linked and then for a photo", async () => {
        mockApi("get", "/api/v1/auth/activation", () => problem(404, "no_activation"));
        mockApi("get", "/api/v1/auth/login/pending", { twoFactorEnabled: true });
        mockApi("post", "/api/v1/auth/login/two-factor", 204);
        mockApi("get", "/api/v1/auth/context", makeAuthContext(makeUser({ photoId: null })));
        const page = renderPage(<ActivationPage />, { path: "/account/activate", user: null });

        expect(await screen.findByRole("heading", { name: "Код з автентифікатора" })).toBeInTheDocument();
        await page.user.type(screen.getByLabelText("Код підтвердження"), "123456");

        expect(await screen.findByRole("heading", { name: "Додайте фото профілю" })).toBeInTheDocument();
    });

    it("finishes in the cabinet when the account already has a photo", async () => {
        mockApi("get", "/api/v1/auth/activation", () => problem(404, "no_activation"));
        mockApi("get", "/api/v1/auth/login/pending", { twoFactorEnabled: true });
        mockApi("post", "/api/v1/auth/login/two-factor", 204);
        mockApi("get", "/api/v1/auth/context", makeAuthContext(makeUser()));
        const page = renderPage(<ActivationPage />, { path: "/account/activate", user: null });

        await page.user.type(await screen.findByLabelText("Код підтвердження"), "123456");

        // The toast first: it comes before the navigation and leaves after 4.5 s, which a loaded run can exceed.
        expect(await screen.findByText("Обліковий запис активовано. Ласкаво просимо до КОД!")).toBeInTheDocument();
        expect(await navigatedTo()).toBe("/");
    });

    it("returns to the e-mail step when the password step has expired", async () => {
        mockApi("get", "/api/v1/auth/activation", { stage: "Password", email: "ivan@example.com", resendAfterSeconds: 0 });
        mockApi("post", "/api/v1/auth/activation/password", () => problem(400, "activation_expired", "Час на встановлення пароля минув."));
        mockApi("delete", "/api/v1/auth/activation");
        const page = renderPage(<ActivationPage />, { path: "/account/activate", user: null });

        await page.user.type(await screen.findByLabelText("Новий пароль"), "New-Password-2026");
        await page.user.type(screen.getByLabelText("Повторіть пароль"), "New-Password-2026");
        await page.user.click(screen.getByRole("button", { name: "Зберегти пароль" }));

        expect(await screen.findByText("Час на встановлення пароля минув.")).toBeInTheDocument();
        expect(await screen.findByRole("button", { name: "Надіслати код" })).toBeInTheDocument();
    });

    it("tells the user when the server cannot send e-mails", async () => {
        const page = renderFresh();
        mockApi("post", "/api/v1/auth/activation", () => problem(503, "email_unavailable", "Надсилання листів не налаштовано."));

        await page.user.type(await screen.findByRole("textbox", { name: "Електронна пошта" }), "ivan@example.com");
        await page.user.click(screen.getByRole("button", { name: "Надіслати код" }));

        expect(await screen.findByText("Надсилання листів не налаштовано.")).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: "Електронна пошта" })).toBeInTheDocument();
    });
});
