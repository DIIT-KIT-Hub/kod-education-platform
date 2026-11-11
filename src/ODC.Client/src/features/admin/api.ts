import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { SaveRoleCommand, SaveUnitCommand, SaveUnitTypeCommand, SaveUserCommand, UserStatusFilter } from "@/shared/api/schema";

/**
 * Sorting and paging of a list (from the address).
 */
export interface ListParams {
    readonly search?: string | undefined;
    readonly page: number;
    readonly pageSize: number;
    readonly sortBy?: string | undefined;
    readonly descending: boolean;
}

/**
 * The filters of the unit list.
 */
export interface UnitFilters extends ListParams {
    readonly typeId?: string | undefined;
    readonly organizationId?: string | undefined;
}

/**
 * The filters of the user list (also used by the export).
 */
export interface UserFilters extends ListParams {
    readonly status: UserStatusFilter;
    readonly unitId?: string | undefined;
    readonly roleId?: string | undefined;
}

/**
 * The filters of the audit log.
 */
export interface AuditFilters extends ListParams {
    readonly category?: string | undefined;
    readonly from?: string | undefined;
    readonly to?: string | undefined;
}

/**
 * The keys of the administration queries (invalidated after changes).
 */
export const adminKeys = {
    all: ["admin"] as const,
    units: (filters: UnitFilters) => ["admin", "units", filters] as const,
    unitOptions: ["admin", "unit-options"] as const,
    unit: (id: string) => ["admin", "unit", id] as const,
    unitTypes: ["admin", "unit-types"] as const,
    unitType: (id: string) => ["admin", "unit-type", id] as const,
    users: (filters: UserFilters) => ["admin", "users", filters] as const,
    userUnits: (manage: boolean) => ["admin", "user-units", manage] as const,
    userRoles: ["admin", "user-roles"] as const,
    user: (id: string) => ["admin", "user", id] as const,
    roles: (params: ListParams) => ["admin", "roles", params] as const,
    role: (id: string) => ["admin", "role", id] as const,
    permissions: ["admin", "permissions"] as const,
    importRoles: ["admin", "import-roles"] as const,
    audit: (filters: AuditFilters) => ["admin", "audit", filters] as const,
    organizations: ["admin", "organizations"] as const,
};

/**
 * The query parameters of a list in the API's spelling.
 * @param params The sorting and paging.
 * @returns The parameters.
 */
function listQuery(params: ListParams) {
    return {
        Search: params.search,
        Page: params.page,
        PageSize: params.pageSize,
        SortBy: params.sortBy,
        Descending: params.descending,
    };
}

/**
 * A page of units for management.
 * @param filters The filters.
 * @returns The query.
 */
export function unitListQuery(filters: UnitFilters) {
    return queryOptions({
        queryKey: adminKeys.units(filters),
        queryFn: ({ signal }) =>
            unwrap(
                api.GET("/api/v1/organization-units/list", {
                    signal,
                    params: { query: { ...listQuery(filters), TypeId: filters.typeId, OrganizationId: filters.organizationId } },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * The units whose structure the user manages, in tree order.
 */
export const unitOptionsQuery = queryOptions({
    queryKey: adminKeys.unitOptions,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/organization-units/options", { signal })),
});

/**
 * A unit for the editor.
 * @param id The unit.
 * @returns The query.
 */
export function unitQuery(id: string) {
    return queryOptions({
        queryKey: adminKeys.unit(id),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/organization-units/{id}", { signal, params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * Every unit type with its number of units.
 */
export const unitTypesQuery = queryOptions({
    queryKey: adminKeys.unitTypes,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/unit-types", { signal })),
});

/**
 * A unit type for the editor.
 * @param id The type.
 * @returns The query.
 */
export function unitTypeQuery(id: string) {
    return queryOptions({
        queryKey: adminKeys.unitType(id),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/unit-types/{id}", { signal, params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * A page of users.
 * @param filters The filters.
 * @returns The query.
 */
export function userListQuery(filters: UserFilters) {
    return queryOptions({
        queryKey: adminKeys.users(filters),
        queryFn: ({ signal }) =>
            unwrap(
                api.GET("/api/v1/users", {
                    signal,
                    params: {
                        query: { ...listQuery(filters), Status: filters.status, UnitId: filters.unitId, RoleId: filters.roleId },
                    },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * The units of the user filters (`manage` false) or of the position editor (`manage` true).
 * @param manage Whether the units are for assigning positions.
 * @returns The query.
 */
export function userUnitsQuery(manage: boolean) {
    return queryOptions({
        queryKey: adminKeys.userUnits(manage),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/users/units", { signal, params: { query: { manage } } })),
    });
}

/**
 * The roles of the user filters and the position editor.
 */
export const userRolesQuery = queryOptions({
    queryKey: adminKeys.userRoles,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/users/roles", { signal })),
});

/**
 * A user account with positions, permissions and what the administrator may change.
 * @param id The user.
 * @returns The query.
 */
export function userQuery(id: string) {
    return queryOptions({
        queryKey: adminKeys.user(id),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/users/{id}", { signal, params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * A page of roles.
 * @param params The search, sorting and paging.
 * @returns The query.
 */
export function roleListQuery(params: ListParams) {
    return queryOptions({
        queryKey: adminKeys.roles(params),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/roles", { signal, params: { query: listQuery(params) } })),
        placeholderData: (previous) => previous,
    });
}

/**
 * A role for the editor.
 * @param id The role.
 * @returns The query.
 */
export function roleQuery(id: string) {
    return queryOptions({
        queryKey: adminKeys.role(id),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/roles/{id}", { signal, params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * The permission catalog with titles and descriptions in the interface language.
 */
export const permissionsQuery = queryOptions({
    queryKey: adminKeys.permissions,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/permissions", { signal })),
    staleTime: Infinity,
});

/**
 * The role codes an import file may use.
 */
export const importRolesQuery = queryOptions({
    queryKey: adminKeys.importRoles,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/user-imports/roles", { signal })),
});

/**
 * A page of the audit log.
 * @param filters The filters.
 * @returns The query.
 */
export function auditQuery(filters: AuditFilters) {
    return queryOptions({
        queryKey: adminKeys.audit(filters),
        queryFn: ({ signal }) =>
            unwrap(
                api.GET("/api/v1/audit-entries", {
                    signal,
                    params: { query: { ...listQuery(filters), Action: filters.category, From: filters.from, To: filters.to } },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * The identifier of a created item (`201 { id }`).
 * @param result The response body.
 * @returns The identifier.
 */
function createdId(result: unknown): string {
    return (result as { id: string }).id;
}

/**
 * Changes of the administration data.
 */
/**
 * The registry of organizations (platform administrators).
 */
export const organizationsQuery = queryOptions({
    queryKey: adminKeys.organizations,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/organizations", { signal })),
});

export const adminApi = {
    createUnit: async (command: SaveUnitCommand) => createdId(await unwrap(api.POST("/api/v1/organization-units", { body: command }))),
    updateUnit: (id: string, command: SaveUnitCommand) =>
        unwrap(api.PUT("/api/v1/organization-units/{id}", { params: { path: { id } }, body: command })),
    createUnitType: async (command: SaveUnitTypeCommand) => createdId(await unwrap(api.POST("/api/v1/unit-types", { body: command }))),
    updateUnitType: (id: string, command: SaveUnitTypeCommand) =>
        unwrap(api.PUT("/api/v1/unit-types/{id}", { params: { path: { id } }, body: command })),
    deleteUnitType: (id: string) => unwrap(api.DELETE("/api/v1/unit-types/{id}", { params: { path: { id } } })),
    createRole: async (command: SaveRoleCommand) => createdId(await unwrap(api.POST("/api/v1/roles", { body: command }))),
    updateRole: (id: string, command: SaveRoleCommand) =>
        unwrap(api.PUT("/api/v1/roles/{id}", { params: { path: { id } }, body: command })),
    deleteRole: (id: string) => unwrap(api.DELETE("/api/v1/roles/{id}", { params: { path: { id } } })),
    createUser: async (command: SaveUserCommand) => createdId(await unwrap(api.POST("/api/v1/users", { body: command }))),
    updateUser: (id: string, command: SaveUserCommand) =>
        unwrap(api.PUT("/api/v1/users/{id}", { params: { path: { id } }, body: command })),
    setUserActive: (id: string, isActive: boolean) =>
        unwrap(api.PUT("/api/v1/users/{id}/status", { params: { path: { id } }, body: { isActive } })),
    resetPassword: (id: string) => unwrap(api.POST("/api/v1/users/{id}/password-resets", { params: { path: { id } } })),
    sendInvitation: (id: string) => unwrap(api.POST("/api/v1/users/{id}/invitations", { params: { path: { id } } })),
    resetAuthenticator: (id: string) => unwrap(api.DELETE("/api/v1/users/{id}/authenticator", { params: { path: { id } } })),
    revokeSessions: (id: string) => unwrap(api.DELETE("/api/v1/users/{id}/sessions", { params: { path: { id } } })),
    suspendOrganization: (id: string) => unwrap(api.POST("/api/v1/organizations/{id}/suspend", { params: { path: { id } } })),
    resumeOrganization: (id: string) => unwrap(api.POST("/api/v1/organizations/{id}/resume", { params: { path: { id } } })),
    rotateOrganizationPasswords: (id: string) =>
        unwrap(api.POST("/api/v1/organizations/{id}/rotate-passwords", { params: { path: { id } } })),
    deleteOrganization: (id: string, confirmation: string) =>
        unwrap(api.DELETE("/api/v1/organizations/{id}", { params: { path: { id }, query: { confirmation } } })),
    importUsers: (file: File, dryRun: boolean) => {
        const form = new FormData();
        form.append("file", file);
        return unwrap(api.POST("/api/v1/user-imports", { params: { query: { dryRun } }, body: {} as never, bodySerializer: () => form }));
    },
};
