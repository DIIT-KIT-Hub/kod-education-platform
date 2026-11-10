import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Check, Trash2 } from "lucide-react";
import { useNavigate } from "react-router";
import { isElsewhere, openInOrganization } from "@/shared/api/organization";
import { describeError } from "@/shared/api/problem";
import type { NotificationDto } from "@/shared/api/schema";
import { formatAgo, formatDateTime } from "@/shared/i18n/format";
import { useOrganization, useOrganizationNames } from "@/shared/session/session";
import { Avatar } from "@/shared/ui/Avatar";
import { Button } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { toast } from "@/shared/ui/Toast";
import { notificationApi } from "@/features/notifications/api";
import { notificationIcons, notificationTitle } from "@/features/notifications/display";
import styles from "./notifications.module.css";

/**
 * Props of {@link NotificationItem}.
 */
export interface NotificationItemProps {
    /** The notification. */
    readonly notification: NotificationDto;
    /** Shows the buttons to mark it read and to delete it (the full list). */
    readonly actions?: boolean;
    /** Called after the user opened it (closes the panel). */
    readonly onOpen?: () => void;
}

/**
 * A notification: who did what, about what, when. Choosing it marks it read and opens its page.
 * @param props The props.
 * @returns The row.
 */
export function NotificationItem({ notification, actions, onOpen }: NotificationItemProps) {
    const { t, i18n } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const unread = !notification.readAt;
    const title = notificationTitle(notification, i18n);
    const organization = useOrganization();
    const nameOf = useOrganizationNames();
    const elsewhere = isElsewhere(notification.organizationCode, organization?.code);

    /**
     * Marks the notification read and opens its page (a notification of another organization opens that organization).
     */
    async function open(): Promise<void> {
        if (unread) {
            const marking = notificationApi
                .markRead(notification.id, elsewhere ? notification.organizationCode : undefined)
                .catch(() => undefined);
            if (elsewhere) {
                await marking;
            }
        }

        onOpen?.();
        if (elsewhere) {
            openInOrganization(notification.organizationCode, notification.link);
        } else {
            void navigate(notification.link);
        }
    }

    /**
     * Runs a change and reports a failure.
     * @param change The change.
     */
    async function run(change: () => Promise<unknown>): Promise<void> {
        try {
            await change();
            await queryClient.invalidateQueries({ queryKey: ["notifications"] });
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити сповіщення.`), { tone: "error" });
        }
    }

    return (
        <li className={styles.item} data-unread={unread || undefined}>
            <button type="button" className={styles.itemMain} onClick={() => void open()}>
                <span className={styles.itemIcon}>
                    {notification.actor ? (
                        <Avatar name={notification.actor.fullName} photoId={notification.actor.photoId} size="sm" decorative />
                    ) : (
                        <Icon icon={notificationIcons[notification.kind]} />
                    )}
                    {notification.actor && (
                        <span className={styles.itemKind}>
                            <Icon icon={notificationIcons[notification.kind]} />
                        </span>
                    )}
                </span>
                <span className={styles.itemText}>
                    <span className={styles.itemTitle}>{title}</span>
                    <span className={styles.itemSubject}>
                        {elsewhere ? `${nameOf(notification.organizationCode)} · ${notification.subject}` : notification.subject}
                    </span>
                    {notification.preview && (
                        <span className={styles.itemPreview}>
                            {notification.actor && <strong>{notification.actor.fullName}: </strong>}
                            {notification.preview}
                        </span>
                    )}
                    <time className={styles.itemTime} dateTime={notification.createdAt} title={formatDateTime(notification.createdAt)}>
                        {formatAgo(notification.createdAt)}
                    </time>
                </span>
                {unread && <span className={styles.dot} aria-label={t`Непрочитане`} />}
            </button>
            {actions && (
                <span className={styles.itemActions}>
                    {unread && (
                        <Button
                            variant="ghost"
                            size="sm"
                            iconOnly
                            icon={Check}
                            onClick={() =>
                                void run(() =>
                                    notificationApi.markRead(notification.id, elsewhere ? notification.organizationCode : undefined),
                                )
                            }
                        >
                            {t`Позначити прочитаним`}
                        </Button>
                    )}
                    <Button
                        variant="ghost"
                        size="sm"
                        iconOnly
                        icon={Trash2}
                        onClick={() =>
                            void run(() => notificationApi.remove(notification.id, elsewhere ? notification.organizationCode : undefined))
                        }
                    >
                        {t`Видалити сповіщення`}
                    </Button>
                </span>
            )}
        </li>
    );
}
