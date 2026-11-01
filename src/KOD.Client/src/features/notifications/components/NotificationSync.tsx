import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation } from "react-router";
import type { NotificationDto } from "@/shared/api/schema";
import { useLiveEvent } from "@/shared/realtime/live";
import { toast } from "@/shared/ui/Toast";
import { applyUnreadCount, notificationKeys, unreadCountQuery } from "@/features/notifications/api";
import { notificationTitle } from "@/features/notifications/display";

/**
 * Keeps the notifications current while the user is signed in, on every page: a new one updates the badge and the
 * lists, refreshes the page it is about (a request, a document) and shows a short message — except chat messages, which
 * the chats show themselves. Renders nothing.
 * @returns Nothing.
 */
export function NotificationSync() {
    const { i18n } = useLingui();
    const queryClient = useQueryClient();
    const { pathname } = useLocation();

    /**
     * Applies a new notification.
     * @param notification The notification.
     * @param unreadCount The number of unread notifications now.
     */
    function onReceived(notification: NotificationDto, unreadCount: number): void {
        applyUnreadCount(queryClient, unreadCount);
        if (notification.kind.startsWith("Ticket")) {
            void queryClient.invalidateQueries({ queryKey: ["tickets"] });
        } else if (notification.kind.startsWith("Document")) {
            void queryClient.invalidateQueries({ queryKey: ["documents"] });
            void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
        }

        // The page of the notification is open: it refreshes itself, no message needed.
        if (notification.kind !== "ChatMessage" && pathname !== notification.link.split("#")[0]) {
            toast(`${notificationTitle(notification, i18n)}: ${notification.subject}`, { tone: "info" });
        }
    }

    useLiveEvent("NotificationReceived", onReceived, () => void queryClient.invalidateQueries({ queryKey: notificationKeys.all }));
    useLiveEvent("NotificationsChanged", (unreadCount: number) => applyUnreadCount(queryClient, unreadCount));

    return null;
}

/**
 * The number of unread notifications (the badge of the bell).
 * @returns The number.
 */
export function useUnreadNotifications(): number {
    return useQuery(unreadCountQuery).data ?? 0;
}
