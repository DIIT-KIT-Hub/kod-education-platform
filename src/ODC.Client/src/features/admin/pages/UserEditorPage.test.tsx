import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Permissions } from "@/shared/session/session";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import {
    answerConfirm,
    choose,
    clearToastsAfterEach,
    findToast,
    ids,
    makeCatalog,
    makePosition,
    makeRoleOptions,
    makeUnitOptions,
    makeUserDetails,
    userWith,
} from "../testing";
import { UserEditorPage } from "./UserEditorPage";

clearToastsAfterEach();

/** An HR specialist: manages accounts and their security, not roles. */
const hr = userWith(Permissions.usersView, Permissions.usersManage, Permissions.usersSecurity);

/**
 * Mocks the choices of the editor (registered after `/users/{id}`, so they win over it).
 */
function mockChoices() {
    mockApi("get", "/api/v1/users/units", makeUnitOptions());
    mockApi("get", "/api/v1/users/roles", makeRoleOptions());
    mockApi("get", "/api/v1/permissions", makeCatalog());
}

/**
 * Renders the editor of an existing account.
 * @param details The account.
 * @param user The signed-in user.
 * @returns The rendered page.
 */
function renderExisting(details = makeUserDetails(), user = userWith(...Object.values(Permissions))) {
    mockApi("get", "/api/v1/users/{id}", details);
    mockChoices();
    return renderPage(<UserEditorPage />, { path: "/admin/users/:id", url: `/admin/users/${details.id}`, user });
}

/**
 * Renders the editor of a new account.
 * @param user The signed-in user.
 * @returns The rendered page.
 */
function renderNew(user = userWith(...Object.values(Permissions))) {
    mockChoices();
    return renderPage(<UserEditorPage />, { path: "/admin/users/new", user });
}

describe("UserEditorPage — a new account", () => {
    it("explains that the user activates the account from an invitation", async () => {
        renderNew();

        expect(await screen.findByText(/створюється без пароля: користувач отримає лист-запрошення/)).toBeInTheDocument();
        expect(screen.queryByText(/тимчасов/)).not.toBeInTheDocument();
    });

    it("creates an account with a position and opens it", async () => {
        const created = mockApi("post", "/api/v1/users", { id: "u-new" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Прізвище/ }), "Бондар");
        await page.user.type(screen.getByRole("textbox", { name: /Ім'я/ }), "Олег");
        await page.user.type(screen.getByRole("textbox", { name: /Електронна пошта/ }), "oleg@odc.local");
        await page.user.click(screen.getByRole("button", { name: "Додати посаду" }));
        await choose(page.user, screen.getByRole("combobox", { name: "Підрозділ" }), /Кафедра права/);
        await choose(page.user, screen.getByRole("combobox", { name: "Роль" }), "Керівник");
        await page.user.type(screen.getByRole("textbox", { name: "Назва посади" }), " Завідувач ");
        await page.user.click(screen.getByRole("button", { name: "Створити обліковий запис" }));

        expect(await navigatedTo()).toBe("/admin/users/u-new");
        expect(created[0]?.body).toEqual({
            email: "oleg@odc.local",
            lastName: "Бондар",
            firstName: "Олег",
            middleName: null,
            phoneNumber: null,
            positions: [{ unitId: ids.department, roleId: ids.otherRole, title: "Завідувач", isPrimary: true }],
            grantedPermissions: [],
            deniedPermissions: [],
        });
        expect(created[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("asks for the names and a valid e-mail", async () => {
        const created = mockApi("post", "/api/v1/users", { id: "never" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Електронна пошта/ }), "не пошта");
        await page.user.click(screen.getByRole("button", { name: "Створити обліковий запис" }));

        expect(await screen.findByText("Вкажіть прізвище.")).toBeInTheDocument();
        expect(screen.getByText("Вкажіть ім'я.")).toBeInTheDocument();
        expect(screen.getByText("Некоректна адреса.")).toBeInTheDocument();
        expect(created).toHaveLength(0);
    });

    it("refuses a position without a unit or role", async () => {
        const created = mockApi("post", "/api/v1/users", { id: "never" });
        const page = renderNew();

        await page.user.type(await screen.findByRole("textbox", { name: /Прізвище/ }), "Бондар");
        await page.user.type(screen.getByRole("textbox", { name: /Ім'я/ }), "Олег");
        await page.user.type(screen.getByRole("textbox", { name: /Електронна пошта/ }), "oleg@odc.local");
        await page.user.click(screen.getByRole("button", { name: "Додати посаду" }));
        await page.user.click(screen.getByRole("button", { name: "Створити обліковий запис" }));

        await findToast("Для кожної посади оберіть підрозділ і роль.");
        expect(created).toHaveLength(0);
    });
});

describe("UserEditorPage — an existing account", () => {
    it("shows the account state and saves changes", async () => {
        const updated = mockApi("put", "/api/v1/users/{id}");
        const page = renderExisting();

        expect(await screen.findByRole("heading", { name: "Коваленко Марія Андріївна" })).toBeInTheDocument();
        expect(screen.getByText("Активний")).toBeInTheDocument();
        expect(screen.getByText("2FA увімкнено")).toBeInTheDocument();
        const phone = screen.getByRole("textbox", { name: "Телефон" });
        await page.user.clear(phone);
        await page.user.click(screen.getByRole("button", { name: "Зберегти зміни" }));

        await findToast("Зміни збережено.");
        expect(updated[0]?.body).toMatchObject({
            email: "maria@odc.local",
            phoneNumber: null,
            positions: [{ unitId: ids.department, roleId: ids.role, title: "Доцент", isPrimary: true }],
        });
    });

    it("grants and denies individual permissions on top of the roles", async () => {
        const updated = mockApi("put", "/api/v1/users/{id}");
        const page = renderExisting();

        await choose(page.user, await screen.findByRole("combobox", { name: /Створення документів/ }), "Надати");
        await choose(page.user, screen.getByRole("combobox", { name: /Перегляд документів/ }), "Заборонити");
        await page.user.click(screen.getByRole("button", { name: "Зберегти зміни" }));

        await waitFor(() =>
            expect(updated[0]?.body).toMatchObject({ grantedPermissions: ["documents.create"], deniedPermissions: ["documents.view"] }),
        );
    });

    it("hides individual permissions from someone who does not manage roles", async () => {
        const permissions = mockApi("get", "/api/v1/permissions", makeCatalog());
        mockApi("get", "/api/v1/users/{id}", makeUserDetails());
        mockApi("get", "/api/v1/users/units", makeUnitOptions());
        mockApi("get", "/api/v1/users/roles", makeRoleOptions());
        renderPage(<UserEditorPage />, { path: "/admin/users/:id", url: `/admin/users/${ids.user}`, user: hr });

        await screen.findByRole("heading", { name: "Коваленко Марія Андріївна" });
        expect(screen.queryByText("Індивідуальні дозволи")).not.toBeInTheDocument();
        expect(permissions).toHaveLength(0);
    });

    it("blocks an account after confirmation", async () => {
        const status = mockApi("put", "/api/v1/users/{id}/status");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Заблокувати" }));
        expect(await answerConfirm(page.user, "Заблокувати")).toBe("Заблокувати обліковий запис? Усі сесії буде завершено.");

        await findToast("Обліковий запис заблоковано.");
        expect(status[0]?.body).toEqual({ isActive: false });
    });

    it("unblocks a blocked account", async () => {
        const status = mockApi("put", "/api/v1/users/{id}/status");
        const page = renderExisting(makeUserDetails({ isActive: false }));

        expect(await screen.findByText("Заблоковано")).toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Розблокувати" }));
        await answerConfirm(page.user, "Розблокувати");

        await findToast("Обліковий запис розблоковано.");
        expect(status[0]?.body).toEqual({ isActive: true });
    });

    it("sends the invitation again to an account awaiting activation, which has no password to reset", async () => {
        const invitation = mockApi("post", "/api/v1/users/{id}/invitations");
        const page = renderExisting(makeUserDetails({ awaitsActivation: true, twoFactorEnabled: false }));

        expect(await screen.findByText("Очікує активації")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Скинути пароль" })).not.toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Надіслати запрошення ще раз" }));

        await findToast("Запрошення надіслано на maria@odc.local.");
        expect(invitation).toHaveLength(1);
    });

    it("tells when e-mail is not configured for the invitation", async () => {
        mockApi("post", "/api/v1/users/{id}/invitations", () => problem(503, "email_unavailable", "Надсилання листів не налаштовано."));
        const page = renderExisting(makeUserDetails({ awaitsActivation: true }));

        await page.user.click(await screen.findByRole("button", { name: "Надіслати запрошення ще раз" }));

        await findToast("Надсилання листів не налаштовано.");
    });

    it("resets the password, the authenticator and the sessions after confirmations", async () => {
        const reset = mockApi("post", "/api/v1/users/{id}/password-resets");
        const authenticator = mockApi("delete", "/api/v1/users/{id}/authenticator");
        const sessions = mockApi("delete", "/api/v1/users/{id}/sessions");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Скинути пароль" }));
        expect(await answerConfirm(page.user, "Скинути пароль")).toMatch(/^Скинути пароль\? Поточний пароль перестане діяти/);
        await findToast("Пароль скинуто. Користувач отримає лист з інструкцією.");

        await page.user.click(screen.getByRole("button", { name: "Скинути автентифікатор" }));
        await answerConfirm(page.user, "Скинути автентифікатор");
        await findToast("Автентифікатор скинуто.");

        await page.user.click(screen.getByRole("button", { name: "Завершити всі сесії" }));
        await answerConfirm(page.user, "Завершити всі сесії");
        await findToast("Сесії завершено.");

        expect([reset.length, authenticator.length, sessions.length]).toEqual([1, 1, 1]);
    });

    it("does nothing when a security action is cancelled", async () => {
        const reset = mockApi("post", "/api/v1/users/{id}/password-resets");
        const page = renderExisting();

        await page.user.click(await screen.findByRole("button", { name: "Скинути пароль" }));
        await answerConfirm(page.user, "Скасувати");

        expect(reset).toHaveLength(0);
    });

    it("limits a shared account to the positions of the administrator's organizations", async () => {
        renderExisting(
            makeUserDetails({
                canEditAccount: false,
                canManageSecurity: false,
                positions: [
                    makePosition(),
                    makePosition({ unitId: ids.foreignUnit, unitName: "Ліцей", isManaged: false, isPrimary: false, title: "Вчитель" }),
                ],
            }),
        );

        expect(await screen.findByText("Обліковий запис кількох організацій")).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: /Прізвище/ })).toBeDisabled();
        expect(screen.getByText("Посада в іншій організації. Змінює адміністратор цієї організації.")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Заблокувати" })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Скинути пароль" })).not.toBeInTheDocument();
    });

    it("hides the security actions from someone without the security permission", async () => {
        renderExisting(makeUserDetails(), userWith(Permissions.usersView, Permissions.usersManage));

        expect(await screen.findByRole("button", { name: "Заблокувати" })).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Скинути пароль" })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Завершити всі сесії" })).not.toBeInTheDocument();
    });

    it("reports a refused save", async () => {
        mockApi("put", "/api/v1/users/{id}", () => problem(409, "conflict", "Адреса вже використовується."));
        const page = renderExisting();

        await page.user.clear(await screen.findByRole("textbox", { name: /Електронна пошта/ }));
        await page.user.type(screen.getByRole("textbox", { name: /Електронна пошта/ }), "busy@odc.local");
        await page.user.click(screen.getByRole("button", { name: "Зберегти зміни" }));

        await findToast("Адреса вже використовується.");
    });

    it("shows the server's message when the account cannot be loaded", async () => {
        mockApi("get", "/api/v1/users/{id}", () => problem(404, "not_found", "Користувача не знайдено."));
        mockChoices();
        renderPage(<UserEditorPage />, { path: "/admin/users/:id", url: "/admin/users/missing" });

        expect(await screen.findByRole("alert")).toHaveTextContent("Користувача не знайдено.");
        expect(screen.getByRole("heading", { level: 1, name: "Обліковий запис" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Користувачі" })).toHaveAttribute("href", "/admin/users");
        expect(screen.queryByRole("button", { name: "Зберегти" })).not.toBeInTheDocument();
    });
});
