import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, CheckCheck } from "lucide-react";
import { useRef, useState, type FocusEvent, type MouseEvent } from "react";
import { Link } from "react-router";
import { describeError } from "@/shared/api/problem";
import { Button } from "@/shared/ui/Button";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { SkeletonList } from "@/shared/ui/Skeleton";
import { toast } from "@/shared/ui/Toast";
import { notificationApi, notificationKeys, notificationListQuery } from "@/features/notifications/api";
import { NotificationItem } from "./NotificationItem";
import styles from "./notifications.module.css";
import { useUnreadNotifications } from "./NotificationSync";

/**
 * The id of the panel (the triggers point to it).
 */
const panelId = "notification-panel";

/**
 * How many of the newest notifications the panel shows.
 */
const panelSize = 8;

/**
 * Props of {@link NotificationTrigger}.
 */
export interface NotificationTriggerProps {
    /** `menu`: an entry of the sidebar; `icon`: a round button of the top bar. */
    readonly variant: "menu" | "icon";
    /** The class of the menu entry (the sidebar's link look). */
    readonly className?: string;
    /** The class of the number. */
    readonly countClassName?: string;
    /** Called when the pointer or the focus reaches the entry (rail tooltips). */
    readonly onHint?: (event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>, text: string) => void;
}

/**
 * The bell that opens the notification panel, with the number of unread notifications (updated live).
 * @param props The props.
 * @returns The button.
 */
export function NotificationTrigger({ variant, className, countClassName, onHint }: NotificationTriggerProps) {
    const { t } = useLingui();
    const unread = useUnreadNotifications();
    const text = t`Сповіщення`;
    const label = unread > 0 ? t`Сповіщення, непрочитаних: ${unread}` : text;
    const count = unread > 0 && (
        <span className={[countClassName, variant === "icon" && styles.badge].filter(Boolean).join(" ")} aria-hidden="true">
            {unread > 99 ? "99+" : unread}
        </span>
    );
    const hint = onHint ? (event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>) => onHint(event, text) : undefined;

    return variant === "menu" ? (
        <button type="button" className={className} popoverTarget={panelId} aria-label={label} onMouseEnter={hint} onFocus={hint}>
            <Icon icon={Bell} />
            <span>{text}</span>
            {count}
        </button>
    ) : (
        <span className={styles.iconTrigger}>
            <Button variant="ghost" size="sm" iconOnly icon={Bell} popoverTarget={panelId}>
                {label}
            </Button>
            {count}
        </span>
    );
}

/**
 * The notification panel: the newest notifications, "mark all read" and the way to the full list. A light-dismiss
 * popover in the top layer, next to the sidebar on desktops and under the top bar elsewhere; it loads only while open.
 * @returns The panel.
 */
export function NotificationPanel() {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const panel = useRef<HTMLDivElement>(null);
    const [open, setOpen] = useState(false);
    const unread = useUnreadNotifications();
    const list = useQuery({ ...notificationListQuery({ page: 1, pageSize: panelSize, unreadOnly: false }), enabled: open });

    /**
     * Marks every notification read.
     */
    async function markAll(): Promise<void> {
        try {
            await notificationApi.markAllRead();
            await queryClient.invalidateQueries({ queryKey: notificationKeys.all });
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити сповіщення.`), { tone: "error" });
        }
    }

    /**
     * Closes the panel.
     */
    function close(): void {
        panel.current?.hidePopover();
    }

    return (
        <div
            id={panelId}
            ref={panel}
            className={styles.panel}
            popover="auto"
            role="dialog"
            aria-label={t`Сповіщення`}
            onToggle={(event) => setOpen(event.newState === "open")}
        >
            <div className={styles.panelHeader}>
                <h2 className={styles.panelTitle}>{t`Сповіщення`}</h2>
                {unread > 0 && (
                    <Button variant="ghost" size="sm" icon={CheckCheck} onClick={() => void markAll()}>
                        {t`Прочитати всі`}
                    </Button>
                )}
            </div>
            <div className={styles.panelBody}>
                {list.isPending && <SkeletonList rows={5} media="icon" />}
                {list.isError && <p className={styles.panelError}>{describeError(list.error, t`Не вдалося завантажити сповіщення.`)}</p>}
                {list.data && list.data.items.length === 0 && (
                    <EmptyState
                        icon={Bell}
                        title={t`Сповіщень немає`}
                        text={t`Тут з'являться відповіді, документи на підпис, повідомлення й звернення.`}
                    />
                )}
                {list.data && list.data.items.length > 0 && (
                    <ul className={styles.list}>
                        {list.data.items.map((notification) => (
                            <NotificationItem key={notification.id} notification={notification} onOpen={close} />
                        ))}
                    </ul>
                )}
            </div>
            <Link className={styles.panelFooter} to="/notifications" onClick={close}>
                {t`Усі сповіщення`}
            </Link>
        </div>
    );
}
