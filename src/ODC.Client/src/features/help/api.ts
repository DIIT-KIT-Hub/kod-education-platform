import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";

/**
 * The permission catalog with titles and descriptions in the interface language.
 */
export const helpPermissionsQuery = queryOptions({
    queryKey: ["help", "permissions"],
    queryFn: () => unwrap(api.GET("/api/v1/permissions")),
    staleTime: Infinity,
});

/**
 * The standard roles created at deployment with their default permissions.
 */
export const systemRolesQuery = queryOptions({
    queryKey: ["help", "system-roles"],
    queryFn: () => unwrap(api.GET("/api/v1/permissions/system-roles")),
    staleTime: Infinity,
});
