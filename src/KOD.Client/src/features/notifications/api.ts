import { queryOptions, type QueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
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
    queryFn: async () => (await unwrap(api.GET("/api/v1/notifications/unread-count"))).count,
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
        queryFn: () =>
            unwrap(
                api.GET("/api/v1/notifications", {
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
 * The changes of notifications (each one answers with nothing; the server sends the new unread count to every page).
 */
export const notificationApi = {
    /**
     * Marks a notification read.
     * @param id The notification.
     * @returns Resolves when saved.
     */
    markRead: (id: string) => unwrap(api.PUT("/api/v1/notifications/{id}/read", { params: { path: { id } } })),

    /**
     * Marks every notification read.
     * @returns Resolves when saved.
     */
    markAllRead: () => unwrap(api.PUT("/api/v1/notifications/read")),

    /**
     * Deletes a notification.
     * @param id The notification.
     * @returns Resolves when deleted.
     */
    remove: (id: string) => unwrap(api.DELETE("/api/v1/notifications/{id}", { params: { path: { id } } })),

    /**
     * Deletes the read notifications.
     * @returns Resolves when deleted.
     */
    removeRead: () => unwrap(api.DELETE("/api/v1/notifications/read")),
};
