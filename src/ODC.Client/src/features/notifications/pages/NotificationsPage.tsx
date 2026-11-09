import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, CheckCheck, Trash2 } from "lucide-react";
import { describeError } from "@/shared/api/problem";
import { useViewParams } from "@/shared/lib/viewParams";
import layout from "@/shared/styles/layout.module.css";
import { Button } from "@/shared/ui/Button";
import { confirm } from "@/shared/ui/ConfirmHost";
import { EmptyState } from "@/shared/ui/EmptyState";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Pagination } from "@/shared/ui/Pagination";
import { Select } from "@/shared/ui/Select";
import { SkeletonList } from "@/shared/ui/Skeleton";
import { toast } from "@/shared/ui/Toast";
import { notificationApi, notificationKeys, notificationListQuery } from "@/features/notifications/api";
import { NotificationItem } from "@/features/notifications/components/NotificationItem";
import styles from "@/features/notifications/components/notifications.module.css";
import { useUnreadNotifications } from "@/features/notifications/components/NotificationSync";

/**
 * All notifications of the user, the newest first: all or only unread (in the view state), marking read and clearing.
 * @returns The page.
 */
export function NotificationsPage() {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const [params, setParams] = useViewParams();
    const unreadOnly = params.get("filter") === "unread";
    const page = Math.max(1, Number(params.get("page")) || 1);
    const pageSize = Number(params.get("pageSize")) || 20;
    const unread = useUnreadNotifications();
    const list = useQuery(notificationListQuery({ page, pageSize, unreadOnly }));

    /**
     * Runs a change of many notifications and reports a failure.
     * @param change The change.
     */
    async function run(change: () => Promise<unknown>): Promise<void> {
        try {
            await change();
            await queryClient.invalidateQueries({ queryKey: notificationKeys.all });
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити сповіщення.`), { tone: "error" });
        }
    }

    /**
     * Deletes the read notifications after a confirmation.
     */
    async function clearRead(): Promise<void> {
        if (await confirm({ title: t`Видалити прочитані сповіщення?`, confirmText: t`Видалити`, danger: true })) {
            await run(notificationApi.removeRead);
        }
    }

    return (
        <>
            <PageHeader
                title={t`Сповіщення`}
                subtitle={t`Відповіді на Ваші коментарі, документи на підпис, нові повідомлення й відповіді у зверненнях.`}
                actions={
                    <>
                        <Button
                            variant="secondary"
                            icon={CheckCheck}
                            disabled={unread === 0}
                            onClick={() => void run(notificationApi.markAllRead)}
                        >
                            {t`Прочитати всі`}
                        </Button>
                        <Button variant="ghost" icon={Trash2} onClick={() => void clearRead()}>
                            {t`Видалити прочитані`}
                        </Button>
                    </>
                }
            />

            <div className={styles.filters}>
                <Select
                    compact
                    aria-label={t`Показати`}
                    value={unreadOnly ? "unread" : ""}
                    onChange={(value) => setParams(value === "unread" ? { filter: "unread" } : {}, { replace: true })}
                    options={[
                        { value: "", label: t`Усі сповіщення` },
                        { value: "unread", label: unread > 0 ? t`Непрочитані (${unread})` : t`Непрочитані` },
                    ]}
                />
            </div>

            {list.isPending && <SkeletonList rows={8} media="icon" framed />}
            {list.data && list.data.items.length === 0 && (
                <EmptyState
                    icon={Bell}
                    title={unreadOnly ? t`Непрочитаних сповіщень немає` : t`Сповіщень немає`}
                    text={t`Тут з'являться відповіді, документи на підпис, повідомлення й звернення.`}
                />
            )}
            {list.data && list.data.items.length > 0 && (
                <div className={layout.stack}>
                    <ul className={[styles.list, styles.pageList].join(" ")}>
                        {list.data.items.map((notification) => (
                            <NotificationItem key={notification.id} notification={notification} actions />
                        ))}
                    </ul>
                    <Pagination page={list.data.page} pageSize={list.data.pageSize} totalCount={list.data.totalCount} />
                </div>
            )}
        </>
    );
}
