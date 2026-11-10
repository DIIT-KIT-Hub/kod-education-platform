import { queryOptions, useSuspenseQuery, type QueryClient } from "@tanstack/react-query";
import { api, setAntiforgeryToken } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { AuthContextResponse, CurrentUserResponse } from "@/shared/api/schema";
import { activateLocale, toLocale } from "@/shared/i18n/i18n";

/**
 * The signed-in user as the application sees it.
 */
export type CurrentUser = CurrentUserResponse;

/**
 * The start context: language, antiforgery token and the signed-in user (null when signed out). Loaded once by the
 * root route and again after every sign-in, sign-out and language change.
 */
export const authContextQuery = queryOptions({
    queryKey: ["auth", "context"],
    queryFn: async ({ signal }): Promise<AuthContextResponse> => {
        const context = await unwrap(api.GET("/api/v1/auth/context", { signal }));
        setAntiforgeryToken(context.antiforgeryToken);
        await activateLocale(toLocale(context.language));
        return context;
    },
    staleTime: Infinity,
    // Without the context no page can show: its failure is the route error page, not a toast.
    meta: { loadErrors: "quiet" },
});

/**
 * Loads the start context again: the antiforgery token is bound to the identity, the claims carry the language.
 * @param queryClient The query client.
 * @returns The new context.
 */
export async function reloadAuthContext(queryClient: QueryClient): Promise<AuthContextResponse> {
    await queryClient.invalidateQueries({ queryKey: authContextQuery.queryKey, refetchType: "none" });
    return queryClient.fetchQuery(authContextQuery);
}

/**
 * The start context (the root route loads it before anything renders).
 * @returns The context.
 */
export function useAuthContext(): AuthContextResponse {
    return useSuspenseQuery(authContextQuery).data;
}

/**
 * The organization the page works in (null before signing in or without one).
 * @returns The organization.
 */
export function useOrganization(): AuthContextResponse["organization"] {
    return useAuthContext().organization;
}

/**
 * Names organizations by code for the labels of lists gathered from several (the abbreviation when there is one).
 * @returns The function that names an organization (its code when it is not among the user's).
 */
export function useOrganizationNames(): (code: string | null | undefined) => string {
    const { organizations } = useAuthContext();
    return (code) => {
        const found = organizations.find((o) => o.code.toLowerCase() === (code ?? "").toLowerCase());
        return found ? (found.shortName ?? found.name) : (code ?? "");
    };
}

/**
 * The signed-in user; only for routes behind the sign-in guard.
 * @returns The user.
 */
export function useCurrentUser(): CurrentUser {
    const { user } = useAuthContext();
    if (!user) {
        throw new Error("The route needs a signed-in user.");
    }

    return user;
}

/**
 * Whether the user holds a permission.
 * @param user The user.
 * @param permission The permission code (e.g. `documents.view`).
 * @returns True when granted.
 */
export function hasPermission(user: CurrentUser, permission: string): boolean {
    return user.permissions.includes(permission);
}

/**
 * Permission codes used by the interface (the server catalog is ODC.Domain.Access.Permissions).
 */
export const Permissions = {
    documentsView: "documents.view",
    documentsCreate: "documents.create",
    signingUse: "signing.use",
    newsView: "news.view",
    newsManage: "news.manage",
    structureView: "structure.view",
    structureManage: "structure.manage",
    usersView: "users.view",
    rolesManage: "roles.manage",
    auditView: "audit.view",
    usersManage: "users.manage",
    usersSecurity: "users.security",
    usersImport: "users.import",
    usersExport: "users.export",
    administrationAll: "administration.all",
    chatsUse: "chats.use",
    ticketsManage: "tickets.manage",
} as const;
