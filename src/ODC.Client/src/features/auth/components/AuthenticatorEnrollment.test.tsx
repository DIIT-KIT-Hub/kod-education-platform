import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { makeAuthContext, makeUser, type Schemas } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { AuthenticatorCodeForm, LinkAuthenticator, RecoveryCodes } from "./AuthenticatorEnrollment";

/** The authenticator key the server returns. */
const key: Schemas["AuthenticatorSetupResponse"] = {
    sharedKey: "JBSWY3DPEHPK3PXP",
    formattedKey: "jbsw y3dp ehpk 3pxp",
    qrCodeSvg: '<svg xmlns="http://www.w3.org/2000/svg"><rect width="1" height="1"/></svg>',
};

/**
 * Mocks the reload of the auth context after the sign-in.
 * @returns The recorded calls.
 */
function mockSignedInContext() {
    return mockApi("get", "/api/v1/auth/context", makeAuthContext(makeUser()));
}

/**
 * Renders the authenticator linking step for a sign-in waiting for the second factor.
 * @param rememberMe Whether the sign-in survives closing the browser.
 * @returns The rendered page and the callback.
 */
function renderLink(rememberMe = false) {
    const onEnrolled = vi.fn();
    const page = renderPage(<LinkAuthenticator rememberMe={rememberMe} onEnrolled={onEnrolled} badge="Крок 4" />, {
        path: "/account/setup-authenticator",
        user: null,
    });
    return { ...page, onEnrolled };
}

describe("LinkAuthenticator", () => {
    it("shows a placeholder while the key loads, then the QR code and the key", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        renderLink();

        expect(screen.getByRole("heading", { name: "Захистіть вхід" })).toBeInTheDocument();
        expect(screen.getByText("Крок 4")).toBeInTheDocument();
        expect(document.title).toBe("Підключення автентифікатора · КОД");

        const qr = await screen.findByRole("img", { name: "QR-код для автентифікатора" });
        expect(qr.getAttribute("src")).toBe(`data:image/svg+xml;charset=utf-8,${encodeURIComponent(key.qrCodeSvg)}`);
        expect(screen.getByText("jbsw y3dp ehpk 3pxp")).toBeInTheDocument();
    });

    it("copies the unformatted key", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        const { user } = renderLink();

        await user.click(await screen.findByRole("button", { name: "Копіювати" }));

        expect(await screen.findByText("Скопійовано")).toBeInTheDocument();
        await expect(navigator.clipboard.readText()).resolves.toBe("JBSWY3DPEHPK3PXP");
    });

    it("says when the key cannot be copied", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        const { user } = renderLink();
        vi.spyOn(navigator.clipboard, "writeText").mockRejectedValueOnce(new Error("denied"));

        await user.click(await screen.findByRole("button", { name: "Копіювати" }));

        expect(await screen.findByText("Не вдалося скопіювати")).toBeInTheDocument();
    });

    it("shows the server's message when the key cannot be loaded", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", () => problem(401, "no_pending_sign_in", "Почніть вхід спочатку."));
        renderLink();

        expect(await screen.findByRole("alert")).toHaveTextContent("Почніть вхід спочатку.");
        expect(screen.queryByRole("img", { name: "QR-код для автентифікатора" })).not.toBeInTheDocument();
    });

    it("says the server cannot be reached when the key request fails", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", () => Response.error());
        renderLink();

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Не вдалося зв'язатися з сервером. Перевірте з'єднання й спробуйте ще раз.",
        );
    });

    it("asks for six digits before calling the server", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        const enroll = mockApi("post", "/api/v1/auth/login/authenticator", { codes: [] });
        const { user } = renderLink();

        await user.type(await screen.findByLabelText("Код з застосунку"), "123");
        await user.click(screen.getByRole("button", { name: "Підтвердити та увійти" }));

        expect(await screen.findByText("Код містить 6 цифр.")).toBeInTheDocument();
        expect(enroll).toHaveLength(0);
    });

    it("links the authenticator once six digits are typed, reloads the session and hands over the codes", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        const enroll = mockApi("post", "/api/v1/auth/login/authenticator", { codes: ["AAAA-1111", "BBBB-2222"] });
        const context = mockSignedInContext();
        const { user, onEnrolled } = renderLink(true);

        await user.type(await screen.findByLabelText("Код з застосунку"), "12a3456");

        await waitFor(() => expect(onEnrolled).toHaveBeenCalledExactlyOnceWith(["AAAA-1111", "BBBB-2222"]));
        expect(enroll).toHaveLength(1);
        expect(enroll[0]?.body).toEqual({ code: "123456", rememberMe: true });
        expect(enroll[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(context).toHaveLength(1);
    });

    it("shows the server's message for a wrong code and stays", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        mockApi("post", "/api/v1/auth/login/authenticator", () => problem(400, "invalid_code", "Невірний код підтвердження."));
        const { user, onEnrolled } = renderLink();

        await user.type(await screen.findByLabelText("Код з застосунку"), "000000");

        expect(await screen.findByText("Невірний код підтвердження.")).toBeInTheDocument();
        expect(onEnrolled).not.toHaveBeenCalled();
        expect(screen.queryByTestId("location")).not.toBeInTheDocument();
    });

    it("says the server cannot be reached when linking fails", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        mockApi("post", "/api/v1/auth/login/authenticator", () => Response.error());
        const { user } = renderLink();

        await user.type(await screen.findByLabelText("Код з застосунку"), "000000");

        expect(await screen.findByText("Не вдалося зв'язатися з сервером. Спробуйте ще раз.")).toBeInTheDocument();
    });

    it("opens the lockout page after too many wrong codes", async () => {
        mockApi("get", "/api/v1/auth/login/authenticator", key);
        mockApi("post", "/api/v1/auth/login/authenticator", () => problem(423, "locked_out", "Обліковий запис заблоковано."));
        const { user, onEnrolled } = renderLink();

        await user.type(await screen.findByLabelText("Код з застосунку"), "000000");

        expect(await navigatedTo()).toBe("/account/lockout");
        expect(onEnrolled).not.toHaveBeenCalled();
    });
});

describe("RecoveryCodes", () => {
    it("shows every code once with the continuing action", async () => {
        renderPage(
            <RecoveryCodes codes={["AAAA-1111", "BBBB-2222", "CCCC-3333"]} badge="Крок 4">
                <button type="button">Далі</button>
            </RecoveryCodes>,
            { user: null },
        );

        expect(await screen.findByRole("heading", { name: "Автентифікатор підключено" })).toBeInTheDocument();
        expect(screen.getByText("Крок 4")).toBeInTheDocument();
        expect(screen.getAllByRole("listitem").map((item) => item.textContent)).toEqual(["AAAA-1111", "BBBB-2222", "CCCC-3333"]);
        expect(screen.getByRole("button", { name: "Далі" })).toBeInTheDocument();
    });

    it("copies all codes, one per line", async () => {
        const { user } = renderPage(
            <RecoveryCodes codes={["AAAA-1111", "BBBB-2222"]}>
                <span />
            </RecoveryCodes>,
            { user: null },
        );

        await user.click(await screen.findByRole("button", { name: "Скопіювати коди" }));

        expect(await screen.findByText("Скопійовано")).toBeInTheDocument();
        await expect(navigator.clipboard.readText()).resolves.toBe("AAAA-1111\nBBBB-2222");
    });
});

describe("AuthenticatorCodeForm", () => {
    /**
     * Renders the code form of a sign-in waiting for the second factor.
     * @param rememberMe Whether the sign-in survives closing the browser.
     * @returns The rendered page and the callback.
     */
    function renderForm(rememberMe = false) {
        const onVerified = vi.fn(() => Promise.resolve());
        const page = renderPage(<AuthenticatorCodeForm rememberMe={rememberMe} submitText="Увійти" onVerified={onVerified} />, {
            path: "/account/login-2fa",
            user: null,
        });
        return { ...page, onVerified };
    }

    it("shows the code field and labels the button", async () => {
        renderForm();

        expect(await screen.findByLabelText("Код підтвердження")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Увійти" })).toBeInTheDocument();
    });

    it("asks for six digits before calling the server", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/two-factor", 204);
        const { user } = renderForm();

        await user.click(await screen.findByRole("button", { name: "Увійти" }));

        expect(await screen.findByText("Код містить 6 цифр.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("sends the code once when the sixth digit is typed, reloads the session and reports success", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/two-factor", 204);
        const context = mockSignedInContext();
        const { user, onVerified } = renderForm(true);

        await user.type(await screen.findByLabelText("Код підтвердження"), "654321");

        await waitFor(() => expect(onVerified).toHaveBeenCalledOnce());
        expect(calls).toHaveLength(1);
        expect(calls[0]?.body).toEqual({ code: "654321", rememberMe: true });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(context).toHaveLength(1);
    });

    it("sends the code with the button too", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/two-factor", 204);
        mockSignedInContext();
        const { user, onVerified } = renderForm();

        const input = await screen.findByLabelText("Код підтвердження");
        await user.type(input, "65432");
        await user.click(screen.getByRole("button", { name: "Увійти" }));

        expect(await screen.findByText("Код містить 6 цифр.")).toBeInTheDocument();
        await user.type(input, "1");
        await waitFor(() => expect(onVerified).toHaveBeenCalledOnce());
        expect(calls).toHaveLength(1);
        expect(calls[0]?.body).toEqual({ code: "654321", rememberMe: false });
    });

    it("shows the server's message for a wrong code", async () => {
        mockApi("post", "/api/v1/auth/login/two-factor", () => problem(400, "invalid_code", "Невірний код підтвердження."));
        const { user, onVerified } = renderForm();

        await user.type(await screen.findByLabelText("Код підтвердження"), "111111");

        expect(await screen.findByText("Невірний код підтвердження.")).toBeInTheDocument();
        expect(onVerified).not.toHaveBeenCalled();
    });

    it("says the server cannot be reached", async () => {
        mockApi("post", "/api/v1/auth/login/two-factor", () => Response.error());
        const { user } = renderForm();

        await user.type(await screen.findByLabelText("Код підтвердження"), "111111");

        expect(await screen.findByText("Не вдалося зв'язатися з сервером. Спробуйте ще раз.")).toBeInTheDocument();
    });

    it("opens the lockout page when the account is locked", async () => {
        mockApi("post", "/api/v1/auth/login/two-factor", () => problem(423, "locked_out", "Заблоковано."));
        const { user, onVerified } = renderForm();

        await user.type(await screen.findByLabelText("Код підтвердження"), "111111");

        expect(await navigatedTo()).toBe("/account/lockout");
        expect(onVerified).not.toHaveBeenCalled();
    });

    it("blocks the code and the button while the code is checked", async () => {
        let answer: (response: Response) => void = () => undefined;
        mockApi("post", "/api/v1/auth/login/two-factor", () => new Promise<Response>((resolve) => (answer = resolve)));
        mockSignedInContext();
        const { user, onVerified } = renderForm();

        const input = await screen.findByLabelText("Код підтвердження");
        await user.type(input, "222222");

        await waitFor(() => expect(input).toBeDisabled());
        expect(screen.getByRole("button", { name: "Увійти" })).toBeDisabled();
        answer(new Response(null, { status: 204 }));
        await waitFor(() => expect(onVerified).toHaveBeenCalledOnce());
    });
});
