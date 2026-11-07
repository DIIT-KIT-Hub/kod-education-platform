import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";

/**
 * The search and paging of a unit's staff (from the address).
 */
export interface StaffFilters {
    readonly search?: string | undefined;
    readonly page: number;
    readonly pageSize: number;
}

/**
 * The keys of the structure queries.
 */
export const structureKeys = {
    all: ["structure"] as const,
    tree: ["structure", "tree"] as const,
    unit: (id: string) => ["structure", "unit", id] as const,
    staff: (id: string, filters: StaffFilters) => ["structure", "staff", id, filters] as const,
};

/**
 * Every organization with its active units.
 */
export const unitTreeQuery = queryOptions({
    queryKey: structureKeys.tree,
    queryFn: () => unwrap(api.GET("/api/v1/organization-units", { params: { query: { includeInactive: false } } })),
});

/**
 * A unit: its path, organization and child units.
 * @param id The unit.
 * @returns The query.
 */
export function unitQuery(id: string) {
    return queryOptions({
        queryKey: structureKeys.unit(id),
        queryFn: () => unwrap(api.GET("/api/v1/organization-units/{id}", { params: { path: { id } } })),
    });
}

/**
 * A page of a unit's staff.
 * @param id The unit.
 * @param filters The search and paging.
 * @returns The query.
 */
export function unitStaffQuery(id: string, filters: StaffFilters) {
    return queryOptions({
        queryKey: structureKeys.staff(id, filters),
        queryFn: () =>
            unwrap(
                api.GET("/api/v1/organization-units/{id}/staff", {
                    params: { path: { id }, query: { Search: filters.search, Page: filters.page, PageSize: filters.pageSize } },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}
