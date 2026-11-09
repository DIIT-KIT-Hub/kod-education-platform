import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { SecurityPage } from "./SecurityPage";

/**
 * The profile as the server returns it.
 * @param overrides Changed fields.
 * @returns The profile.
 */
function makeProfile(overrides: Partial<Schemas["ProfileDto"]> = {}): Schemas["ProfileDto"] {
    return {
        id: "user-1",
        fullName: "Шевченко Тарас",
        lastName: "Шевченко",
        firstName: "Тарас",
        middleName: null,
        email: "taras@example.com",
        phoneNumber: null,
        positions: [],
        modules: [],
        permissions: [],
        twoFactorEnabled: true,
        recoveryCodesLeft: 3,
        createdAt: "2026-01-10T08:00:00Z",
        lastSignInAt: null,
        language: null,
        photoId: null,
        canEditPersonalData: false,
        ...overrides,
    };
}

/**
 * Renders the security page.
 * @param profile The profile.
 * @returns The rendered page.
 */
function renderSecurity(profile: Schemas["ProfileDto"] = makeProfile()) {
    mockApi("get", "/api/v1/profile", profile);
    return renderPage(<SecurityPage />, { path: "/profile/security" });
}

/**
 * Confirms the open confirmation dialog.
 * @param page The rendered page.
 * @param title The dialog title.
 * @param action The confirm button text.
 */
async function confirmDialog(page: ReturnType<typeof renderSecurity>, title: string, action: string) {
    const dialog = (await screen.findByRole("heading", { name: title })).closest("dialog") as HTMLElement;
    await page.user.click(within(dialog).getAllByRole("button", { name: action }).at(-1) as HTMLElement);
}

describe("SecurityPage", () => {
    it("shows the 2FA state, the recovery codes left and the password link", async () => {
        renderSecurity();

        expect(await screen.findByText("Увімкнено")).toBeInTheDocument();
        expect(screen.getByText("3")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Змінити пароль" })).toHaveAttribute("href", "/account/change-password");
    });

    it("warns that 2FA is not set up and offers no new codes then", async () => {
        renderSecurity(makeProfile({ twoFactorEnabled: false }));

        expect(await screen.findByText("Не налаштовано")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Нові коди" })).not.toBeInTheDocument();
    });

    it("asks for the six digits before generating new codes", async () => {
        const calls = mockApi("post", "/api/v1/profile/recovery-codes", { codes: [] });
        const page = renderSecurity();

        await page.user.type(await screen.findByRole("textbox", { name: "Код із застосунку-автентифікатора" }), "123");
        await page.user.click(screen.getByRole("button", { name: "Нові коди" }));

        expect(await screen.findByText("Введіть 6 цифр із застосунку-автентифікатора.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("generates new codes after confirmation and shows them once", async () => {
        const calls = mockApi("post", "/api/v1/profile/recovery-codes", { codes: ["AAAA-1111", "BBBB-2222"] });
        const page = renderSecurity();

        await page.user.type(await screen.findByRole("textbox", { name: "Код із застосунку-автентифікатора" }), "654321");
        await page.user.click(screen.getByRole("button", { name: "Нові коди" }));
        await confirmDialog(page, "Згенерувати нові коди? Старі перестануть діяти.", "Нові коди");

        expect(await screen.findByText("AAAA-1111")).toBeInTheDocument();
        expect(screen.getByText("BBBB-2222")).toBeInTheDocument();
        expect(calls[0]?.body).toEqual({ code: "654321" });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(screen.getByRole("textbox", { name: "Код із застосунку-автентифікатора" })).toHaveValue("");
    });

    it("reports a wrong code and clears it", async () => {
        mockApi("post", "/api/v1/profile/recovery-codes", () => problem(400, "validation", "Неправильний код."));
        const page = renderSecurity();

        const input = await screen.findByRole("textbox", { name: "Код із застосунку-автентифікатора" });
        await page.user.type(input, "000000");
        await page.user.click(screen.getByRole("button", { name: "Нові коди" }));
        await confirmDialog(page, "Згенерувати нові коди? Старі перестануть діяти.", "Нові коди");

        expect(await screen.findByText("Неправильний код.")).toBeInTheDocument();
        await waitFor(() => expect(input).toHaveValue(""));
    });

    it("signs out the other devices after confirmation", async () => {
        const calls = mockApi("post", "/api/v1/profile/sign-out-everywhere");
        const page = renderSecurity();

        await page.user.click(await screen.findByRole("button", { name: "Вийти на інших пристроях" }));
        await confirmDialog(page, "Вийти на всіх інших пристроях?", "Вийти на інших пристроях");

        await waitFor(() => expect(calls).toHaveLength(1));
        expect(await screen.findByText("Входи на інших пристроях завершено")).toBeInTheDocument();
    });

    it("does nothing when signing out elsewhere is cancelled", async () => {
        const calls = mockApi("post", "/api/v1/profile/sign-out-everywhere");
        const page = renderSecurity();

        await page.user.click(await screen.findByRole("button", { name: "Вийти на інших пристроях" }));
        await confirmDialog(page, "Вийти на всіх інших пристроях?", "Скасувати");

        expect(calls).toHaveLength(0);
    });
});
