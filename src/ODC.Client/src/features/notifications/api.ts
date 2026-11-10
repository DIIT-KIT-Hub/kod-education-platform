import { queryOptions, type QueryClient } from "@tanstack/react-query";
import { api, organizationApi } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";

/**
 * The paging and filter of the notification list.
 */
export interface NotificationFilters {
    readonly page: number;
    readonly pageSize: number;
    readonly unreadOnly: boolean;
}

/**
 * The keys of the notification queries.
 */
export const notificationKeys = {
    all: ["notifications"] as const,
    unread: ["notifications", "unread"] as const,
    lists: ["notifications", "list"] as const,
    list: (filters: NotificationFilters) => ["notifications", "list", filters] as const,
};

/**
 * The number of unread notifications (the badge); kept current by the live events.
 */
export const unreadCountQuery = queryOptions({
    queryKey: notificationKeys.unread,
    queryFn: async ({ signal }) => (await unwrap(api.GET("/api/v1/notifications/unread-count", { signal }))).count,
    staleTime: 5 * 60_000,
});

/**
 * A page of notifications, the newest first.
 * @param filters The paging and filter.
 * @returns The query.
 */
export function notificationListQuery(filters: NotificationFilters) {
    return queryOptions({
        queryKey: notificationKeys.list(filters),
        queryFn: ({ signal }) =>
            unwrap(
                api.GET("/api/v1/notifications", {
                    signal,
                    params: { query: { Page: filters.page, PageSize: filters.pageSize, UnreadOnly: filters.unreadOnly } },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * Sets the unread count and reloads the lists.
 * @param queryClient The query client.
 * @param count The number of unread notifications.
 */
export function applyUnreadCount(queryClient: QueryClient, count: number): void {
    queryClient.setQueryData(notificationKeys.unread, count);
    void queryClient.invalidateQueries({ queryKey: notificationKeys.lists });
}

/**
 * The request option that sends a call to another organization.
 * @param code The organization's code, or nothing for the page's organization.
 * @returns The option.
 */
function inOrganization(code: string | undefined): { baseUrl?: string } {
    return code ? { baseUrl: organizationApi(code) } : {};
}

/**
 * The changes of notifications (each one answers with nothing; the server sends the new unread count to every page).
 */
export const notificationApi = {
    /**
     * Marks a notification read (in the organization it comes from).
     * @param id The notification.
     * @param elsewhere The code of its organization when it is not the page's.
     * @returns Resolves when saved.
     */
    markRead: (id: string, elsewhere?: string) =>
        unwrap(api.PUT("/api/v1/notifications/{id}/read", { ...inOrganization(elsewhere), params: { path: { id } } })),

    /**
     * Marks every notification read.
     * @returns Resolves when saved.
     */
    markAllRead: () => unwrap(api.PUT("/api/v1/notifications/read")),

    /**
     * Deletes a notification (in the organization it comes from).
     * @param id The notification.
     * @param elsewhere The code of its organization when it is not the page's.
     * @returns Resolves when deleted.
     */
    remove: (id: string, elsewhere?: string) =>
        unwrap(api.DELETE("/api/v1/notifications/{id}", { ...inOrganization(elsewhere), params: { path: { id } } })),

    /**
     * Deletes the read notifications.
     * @returns Resolves when deleted.
     */
    removeRead: () => unwrap(api.DELETE("/api/v1/notifications/read")),
};
