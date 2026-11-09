import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makeAuthContext, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { ProfilePage } from "./ProfilePage";

/**
 * The profile as the server returns it.
 * @param overrides Changed fields.
 * @returns The profile.
 */
function makeProfile(overrides: Partial<Schemas["ProfileDto"]> = {}): Schemas["ProfileDto"] {
    return {
        id: "user-1",
        fullName: "Шевченко Тарас Григорович",
        lastName: "Шевченко",
        firstName: "Тарас",
        middleName: "Григорович",
        email: "taras@example.com",
        phoneNumber: null,
        positions: [
            {
                unitId: "unit-1",
                unitName: "Відділ кадрів",
                roleId: "role-1",
                roleName: "Кадровик",
                title: "Інспектор з кадрів",
                isPrimary: true,
            },
            { unitId: "unit-2", unitName: "Бухгалтерія", roleId: "role-2", roleName: "Працівник", title: null, isPrimary: false },
        ],
        modules: ["Documents", "Chats"],
        permissions: [
            { code: "documents.view", module: "Documents", title: "Перегляд документів", description: "Відкривати доступні документи" },
        ],
        twoFactorEnabled: true,
        recoveryCodesLeft: 8,
        createdAt: "2026-01-10T08:00:00Z",
        lastSignInAt: "2026-11-09T07:30:00Z",
        language: "uk",
        photoId: null,
        canEditPersonalData: false,
        ...overrides,
    };
}

/**
 * Renders the profile page.
 * @param profile The profile.
 * @returns The rendered page.
 */
function renderProfile(profile: Schemas["ProfileDto"] = makeProfile()) {
    mockApi("get", "/api/v1/profile", profile);
    return renderPage(<ProfilePage />, { path: "/profile" });
}

describe("ProfilePage", () => {
    it("shows the name, e-mail, 2FA, positions, modules and permissions", async () => {
        renderProfile();

        expect(await screen.findByRole("heading", { name: "Шевченко Тарас Григорович" })).toBeInTheDocument();
        expect(screen.getByText("2FA увімкнено")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Інспектор з кадрів/ })).toHaveAttribute("href", "/structure/unit-1");
        expect(screen.getByText("Відділ кадрів · роль «Кадровик»")).toBeInTheDocument();
        expect(screen.getByText("Основна")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Працівник/ })).toHaveAttribute("href", "/structure/unit-2");
        expect(screen.getByText("Усі мої дозволи (1)")).toBeInTheDocument();
        expect(screen.getByText("Перегляд документів")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Змінити фото" })).toHaveAttribute("href", "/account/photo");
    });

    it("says when no position is assigned", async () => {
        renderProfile(makeProfile({ positions: [] }));

        expect(await screen.findByText("Посади не призначено")).toBeInTheDocument();
    });

    it("lets an employee change only the phone, sending nothing else", async () => {
        const calls = mockApi("patch", "/api/v1/profile");
        const page = renderProfile();

        expect(await screen.findByRole("heading", { name: "Контакти" })).toBeInTheDocument();
        expect(screen.queryByRole("textbox", { name: /Прізвище/ })).not.toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: /Електронна пошта/ })).toBeDisabled();
        const save = screen.getByRole("button", { name: "Зберегти" });
        expect(save).toBeDisabled();

        await page.user.type(screen.getByRole("textbox", { name: "Телефон" }), "+380 50 111 22 33");
        await page.user.click(save);

        await waitFor(() => expect(calls).toHaveLength(1));
        expect(calls[0]?.body).toEqual({
            lastName: null,
            firstName: null,
            middleName: null,
            email: null,
            phoneNumber: "+380 50 111 22 33",
        });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        expect(await screen.findByText("Зміни збережено.")).toBeInTheDocument();
    });

    it("refuses a malformed phone number", async () => {
        const calls = mockApi("patch", "/api/v1/profile");
        const page = renderProfile();

        await page.user.type(await screen.findByRole("textbox", { name: "Телефон" }), "дзвоніть");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Некоректний номер телефону.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("lets an administrator change the names and e-mail and reloads the session", async () => {
        const calls = mockApi("patch", "/api/v1/profile");
        const context = mockApi("get", "/api/v1/auth/context", makeAuthContext());
        const page = renderProfile(makeProfile({ canEditPersonalData: true }));

        const lastName = await screen.findByRole("textbox", { name: /Прізвище/ });
        await page.user.clear(lastName);
        await page.user.type(lastName, " Коваль ");
        await page.user.clear(screen.getByRole("textbox", { name: /По батькові/ }));
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        await waitFor(() => expect(calls).toHaveLength(1));
        expect(calls[0]?.body).toEqual({
            lastName: "Коваль",
            firstName: "Тарас",
            middleName: "",
            email: "taras@example.com",
            phoneNumber: null,
        });
        await waitFor(() => expect(context).toHaveLength(1));
    });

    it("asks for the names and a valid e-mail", async () => {
        const calls = mockApi("patch", "/api/v1/profile");
        const page = renderProfile(makeProfile({ canEditPersonalData: true }));

        await page.user.clear(await screen.findByRole("textbox", { name: /Прізвище/ }));
        await page.user.clear(screen.getByRole("textbox", { name: /Ім'я/ }));
        const email = screen.getByRole("textbox", { name: /Електронна пошта/ });
        await page.user.clear(email);
        await page.user.type(email, "не пошта");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Вкажіть прізвище.")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть ім'я.")).toBeInTheDocument();
        expect(screen.getByText("Некоректна адреса.")).toBeInTheDocument();
        expect(calls).toHaveLength(0);
    });

    it("reports a refused change and keeps the form", async () => {
        mockApi("patch", "/api/v1/profile", () => problem(409, "conflict", "Адреса вже використовується."));
        const page = renderProfile(makeProfile({ canEditPersonalData: true }));

        const email = await screen.findByRole("textbox", { name: /Електронна пошта/ });
        await page.user.clear(email);
        await page.user.type(email, "busy@example.com");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));

        expect(await screen.findByText("Адреса вже використовується.")).toBeInTheDocument();
        expect(email).toHaveValue("busy@example.com");
    });

    it("saves the chosen interface language in the profile", async () => {
        const calls = mockApi("put", "/api/v1/auth/language");
        mockApi("get", "/api/v1/auth/context", makeAuthContext());
        const page = renderProfile();

        await page.user.click(await screen.findByRole("radio", { name: /English/ }));

        await waitFor(() => expect(calls[0]?.body).toEqual({ language: "en" }));
    });

    it("reports a failed language change", async () => {
        mockApi("put", "/api/v1/auth/language", () => problem(400, "validation", "Мова не підтримується."));
        const page = renderProfile();

        await page.user.click(await screen.findByRole("radio", { name: /English/ }));

        expect(await screen.findByText("Мова не підтримується.")).toBeInTheDocument();
    });

    it("shows the server's message when the profile cannot be loaded", async () => {
        mockApi("get", "/api/v1/profile", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<ProfilePage />, { path: "/profile" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});
