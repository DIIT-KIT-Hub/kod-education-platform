import type { components } from "@/shared/api/schema";
import { Permissions } from "@/shared/session/session";

/**
 * The schemas of the API.
 */
export type Schemas = components["schemas"];

/**
 * Every permission of the cabinet (a system administrator).
 */
export const allPermissions: readonly string[] = Object.values(Permissions);

/**
 * A signed-in user.
 * @param overrides Changed fields (e.g. fewer permissions).
 * @returns The user of the auth context.
 */
export function makeUser(overrides: Partial<Schemas["CurrentUserResponse"]> = {}): Schemas["CurrentUserResponse"] {
    return {
        id: "00000000-0000-7000-8000-000000000001",
        email: "admin@odc.local",
        fullName: "Шевченко Тарас Григорович",
        shortName: "Тарас",
        photoId: "00000000-0000-7000-8000-0000000000aa",
        permissions: [...allPermissions],
        ...overrides,
    };
}

/**
 * The auth context of a signed-in user or of a visitor.
 * @param user The user, or null for a visitor.
 * @param language The interface language.
 * @returns The context.
 */
export function makeAuthContext(user: Schemas["CurrentUserResponse"] | null = makeUser(), language = "uk"): Schemas["AuthContextResponse"] {
    return { language, antiforgeryToken: "csrf-test-token", user };
}

/**
 * A reference to a person.
 * @param overrides Changed fields.
 * @returns The reference.
 */
export function makeUserRef(overrides: Partial<Schemas["UserRef"]> = {}): Schemas["UserRef"] {
    return { id: "00000000-0000-7000-8000-000000000002", fullName: "Коваленко Марія Андріївна", photoId: null, ...overrides };
}

/**
 * A page of a list.
 * @param items The items.
 * @param overrides Changed paging fields.
 * @returns The page as the API returns it.
 */
export function makePage<T>(items: readonly T[], overrides: { page?: number; pageSize?: number; totalCount?: number } = {}) {
    const pageSize = overrides.pageSize ?? 20;
    const page = overrides.page ?? 1;
    const totalCount = overrides.totalCount ?? items.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    return { items: [...items], page, pageSize, totalCount, totalPages, hasPrevious: page > 1, hasNext: page < totalPages };
}
