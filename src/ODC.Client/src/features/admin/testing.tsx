import { act, screen, waitFor, within } from "@testing-library/react";
import type { UserEvent } from "@testing-library/user-event";
import { afterEach, expect } from "vitest";
import { makeUser, type Schemas } from "@/test/factories";

/**
 * Test helpers of the administration pages: fixtures typed by the API schemas, the drop-down lists of the design system
 * and the notifications (a module store that outlives a rendered page).
 */

/** Identifiers used by the fixtures. */
export const ids = {
    user: "00000000-0000-7000-8000-000000000101",
    otherUser: "00000000-0000-7000-8000-000000000102",
    organization: "00000000-0000-7000-8000-000000000201",
    otherOrganization: "00000000-0000-7000-8000-000000000202",
    department: "00000000-0000-7000-8000-000000000203",
    foreignUnit: "00000000-0000-7000-8000-000000000204",
    role: "00000000-0000-7000-8000-000000000301",
    otherRole: "00000000-0000-7000-8000-000000000302",
    organizationType: "00000000-0000-7000-8000-000000000401",
    departmentType: "00000000-0000-7000-8000-000000000402",
} as const;

/**
 * A signed-in user with only the given permissions.
 * @param permissions The permissions.
 * @returns The user of the auth context.
 */
export function userWith(...permissions: string[]): Schemas["CurrentUserResponse"] {
    return makeUser({ permissions });
}

/**
 * Removes every notification after each test of the file: toasts live in a module store, so a toast of one test would
 * otherwise still be on screen in the next one.
 */
export function clearToastsAfterEach(): void {
    afterEach(async () => {
        const toasts = () => [...document.querySelectorAll<HTMLElement>("[data-tone]")];
        for (const item of toasts()) {
            const close = item.querySelector("button");
            if (close) {
                act(() => close.click());
            }
        }

        await waitFor(() => expect(toasts()).toHaveLength(0));
    });
}

/**
 * Waits for a notification with the text.
 * @param text The text of the notification.
 * @returns The notification element.
 */
export async function findToast(text: string | RegExp): Promise<HTMLElement> {
    return await waitFor(() => {
        const match = [...document.querySelectorAll<HTMLElement>("[data-tone]")].find((item) =>
            typeof text === "string" ? item.textContent === text : text.test(item.textContent ?? ""),
        );
        expect(match, `toast "${String(text)}"`).toBeDefined();
        return match as HTMLElement;
    });
}

/**
 * The tone of a notification (`success`, `error`, `warning`, `info`).
 * @param text The text of the notification.
 * @returns The tone.
 */
export async function toastTone(text: string | RegExp): Promise<string | undefined> {
    return (await findToast(text)).dataset.tone;
}

/**
 * Chooses an option of a design-system drop-down list.
 * @param user The simulated user.
 * @param combobox The list button.
 * @param option The visible text of the option.
 */
export async function choose(user: UserEvent, combobox: HTMLElement, option: string | RegExp): Promise<void> {
    await user.click(combobox);
    const listbox = document.getElementById(combobox.getAttribute("aria-controls") ?? "");
    await user.click(within(listbox as HTMLElement).getByRole("option", { name: option }));
}

/**
 * The texts of the options of a drop-down list (opens and closes it).
 * @param user The simulated user.
 * @param combobox The list button.
 * @returns The option texts.
 */
export async function optionsOf(user: UserEvent, combobox: HTMLElement): Promise<string[]> {
    await user.click(combobox);
    const listbox = document.getElementById(combobox.getAttribute("aria-controls") ?? "");
    const texts = within(listbox as HTMLElement)
        .getAllByRole("option")
        .map((option) => option.textContent ?? "");
    await user.click(combobox);
    return texts;
}

/**
 * Answers the open confirmation dialog.
 * @param user The simulated user.
 * @param button The button to press (the confirming text or «Скасувати»).
 * @returns The question of the dialog.
 */
export async function answerConfirm(user: UserEvent, button: string): Promise<string> {
    const dialog = await waitFor(() => {
        const element = document.querySelector<HTMLDialogElement>("dialog[open]");
        expect(element).not.toBeNull();
        return element as HTMLDialogElement;
    });
    const title = within(dialog).getByRole("heading").textContent ?? "";
    await user.click(within(dialog).getByRole("button", { name: button }));
    return title;
}

/**
 * The view state of the current history entry (list pages keep their filters there).
 * @param router The router of the rendered page.
 * @param router.state The router state.
 * @param router.state.location The current location.
 * @returns The view parameters.
 */
export function viewOf(router: { state: { location: { state: unknown } } }): URLSearchParams {
    const state = router.state.location.state as { view?: string } | null;
    return new URLSearchParams(state?.view ?? "");
}

/**
 * Waits until the loading skeleton is gone.
 */
export async function loaded(): Promise<void> {
    await waitFor(() => expect(screen.queryByRole("status", { name: "Завантаження" })).not.toBeInTheDocument());
}

/**
 * The permission catalog of the tests: two modules, one permission without a title.
 * @returns The catalog.
 */
export function makeCatalog(): Schemas["PermissionDefinition"][] {
    return [
        { code: "documents.view", module: "Documents", title: "Перегляд документів", description: "Відкривати документи." },
        { code: "documents.create", module: "Documents", title: "Створення документів", description: "Реєструвати нові." },
        { code: "users.view", module: "Users", title: "Перегляд користувачів", description: "Бачити облікові записи." },
        { code: "users.manage", module: "Users", title: null, description: null },
    ];
}

/**
 * A row of the user list.
 * @param overrides Changed fields.
 * @returns The row.
 */
export function makeUserListItem(overrides: Partial<Schemas["UserListItem"]> = {}): Schemas["UserListItem"] {
    return {
        id: ids.user,
        fullName: "Коваленко Марія Андріївна",
        email: "maria@odc.local",
        position: "Доцент",
        unit: "Кафедра права",
        isActive: true,
        awaitsActivation: false,
        twoFactorEnabled: true,
        isLockedOut: false,
        lastSignInAt: "2026-11-01T08:30:00Z",
        photoId: null,
        ...overrides,
    };
}

/**
 * A position of an account.
 * @param overrides Changed fields.
 * @returns The position.
 */
export function makePosition(overrides: Partial<Schemas["UserPositionDto"]> = {}): Schemas["UserPositionDto"] {
    return {
        unitId: ids.department,
        unitName: "Кафедра права",
        organization: { id: ids.organization, name: "Університет" },
        roleId: ids.role,
        roleName: "Працівник",
        title: "Доцент",
        isPrimary: true,
        isManaged: true,
        ...overrides,
    };
}

/**
 * An account in the editor.
 * @param overrides Changed fields.
 * @returns The account.
 */
export function makeUserDetails(overrides: Partial<Schemas["UserDetails"]> = {}): Schemas["UserDetails"] {
    return {
        id: ids.user,
        email: "maria@odc.local",
        lastName: "Коваленко",
        firstName: "Марія",
        middleName: "Андріївна",
        fullName: "Коваленко Марія Андріївна",
        phoneNumber: "+380501234567",
        isActive: true,
        twoFactorEnabled: true,
        awaitsActivation: false,
        isLockedOut: false,
        createdAt: "2026-01-15T10:00:00Z",
        lastSignInAt: "2026-11-01T08:30:00Z",
        positions: [makePosition()],
        grantedPermissions: [],
        deniedPermissions: [],
        effectivePermissions: ["documents.view"],
        canEditAccount: true,
        canEditPositions: true,
        canManageSecurity: true,
        photoId: "00000000-0000-7000-8000-0000000000bb",
        ...overrides,
    };
}

/**
 * The units of the user filters and the position editor.
 * @returns The units in tree order.
 */
export function makeUnitOptions(): Schemas["UnitOption"][] {
    return [
        { id: ids.organization, name: "Університет", code: "UNI", isOrganization: true, depth: 0 },
        { id: ids.department, name: "Кафедра права", code: "LAW", isOrganization: false, depth: 1 },
        { id: ids.otherOrganization, name: "Ліцей", code: "LYC", isOrganization: true, depth: 0 },
    ];
}

/**
 * The roles of the user filters and the position editor.
 * @returns The roles.
 */
export function makeRoleOptions(): Schemas["RoleOption"][] {
    return [
        { id: ids.role, code: "employee", name: "Працівник" },
        { id: ids.otherRole, code: "head", name: "Керівник" },
    ];
}

/**
 * A row of the role list.
 * @param overrides Changed fields.
 * @returns The row.
 */
export function makeRoleListItem(overrides: Partial<Schemas["RoleListItem"]> = {}): Schemas["RoleListItem"] {
    return {
        id: ids.role,
        code: "employee",
        name: "Працівник",
        description: "Звичайна посада",
        isSystem: false,
        isLeadership: false,
        permissionCount: 3,
        userCount: 12,
        ...overrides,
    };
}

/**
 * A role in the editor.
 * @param overrides Changed fields.
 * @returns The role.
 */
export function makeRoleDetails(overrides: Partial<Schemas["RoleDetails"]> = {}): Schemas["RoleDetails"] {
    return {
        id: ids.role,
        code: "employee",
        name: "Працівник",
        description: "Звичайна посада",
        isSystem: false,
        isAdministrator: false,
        isLeadership: false,
        permissions: ["documents.view"],
        userCount: 0,
        ...overrides,
    };
}

/**
 * A unit type.
 * @param overrides Changed fields.
 * @returns The type.
 */
export function makeUnitType(overrides: Partial<Schemas["UnitTypeItem"]> = {}): Schemas["UnitTypeItem"] {
    return {
        id: ids.departmentType,
        code: "DEPARTMENT",
        name: "Кафедра",
        description: "Навчальний підрозділ",
        icon: "graduation-cap",
        isOrganization: false,
        sortOrder: 20,
        unitCount: 0,
        ...overrides,
    };
}

/**
 * The unit types of the tests: a department level first, then an organization.
 * @returns The types.
 */
export function makeUnitTypes(): Schemas["UnitTypeItem"][] {
    return [
        makeUnitType({ unitCount: 4 }),
        makeUnitType({
            id: ids.organizationType,
            code: "ORG",
            name: "Заклад",
            description: null,
            icon: "landmark",
            isOrganization: true,
            sortOrder: 10,
            unitCount: 2,
        }),
    ];
}

/**
 * A row of the unit list.
 * @param overrides Changed fields.
 * @returns The row.
 */
export function makeUnitListItem(overrides: Partial<Schemas["UnitListItem"]> = {}): Schemas["UnitListItem"] {
    return {
        id: ids.department,
        code: "LAW",
        name: "Кафедра права",
        type: { id: ids.departmentType, name: "Кафедра", icon: "graduation-cap", isOrganization: false },
        parentName: "Університет",
        isActive: true,
        staffCount: 7,
        ...overrides,
    };
}

/**
 * A unit in the editor.
 * @param overrides Changed fields.
 * @returns The unit.
 */
export function makeUnitDetails(overrides: Partial<Schemas["UnitDetails"]> = {}): Schemas["UnitDetails"] {
    return {
        id: ids.department,
        code: "LAW",
        name: "Кафедра права",
        shortName: "КП",
        type: { id: ids.departmentType, name: "Кафедра", icon: "graduation-cap", isOrganization: false },
        isActive: true,
        parentId: ids.organization,
        sortOrder: 5,
        path: [{ id: ids.organization, name: "Університет" }],
        organization: { id: ids.organization, name: "Університет" },
        children: [],
        staffCount: 7,
        canManage: true,
        ...overrides,
    };
}

/**
 * An entry of the audit log.
 * @param overrides Changed fields.
 * @returns The entry.
 */
export function makeAuditEntry(overrides: Partial<Schemas["AuditEntryDto"]> = {}): Schemas["AuditEntryDto"] {
    return {
        id: "00000000-0000-7000-8000-000000000501",
        occurredAt: "2026-11-02T09:15:00Z",
        action: "auth.sign-in",
        userId: ids.user,
        userName: "Коваленко Марія Андріївна",
        entityType: null,
        entityId: null,
        details: "Chrome, Windows",
        ipAddress: "10.0.0.7",
        userPhotoId: null,
        organizationCode: null,
        ...overrides,
    };
}
