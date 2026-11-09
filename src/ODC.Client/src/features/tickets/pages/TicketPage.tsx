import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Lock, UserCheck } from "lucide-react";
import { useParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { TicketCategory, TicketDetails, TicketPriority, TicketStatus } from "@/shared/api/schema";
import { ticketCategoryNames, ticketPriorityNames, ticketStatusNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { confirm } from "@/shared/ui/ConfirmHost";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { Field } from "@/shared/ui/Field";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { Select } from "@/shared/ui/Select";
import { SkeletonPage } from "@/shared/ui/Skeleton";
import { toast } from "@/shared/ui/Toast";
import { handlersQuery, ticketApi, ticketKeys, ticketQuery } from "@/features/tickets/api";
import { TicketStatusBadge } from "@/features/tickets/components/TicketStatusBadge";
import { ReplyBox, TicketThread } from "@/features/tickets/components/TicketThread";

/**
 * A request with its history: the author and the handlers write in it; a handler changes the state, the handler, the
 * category and the priority; the author may close it. Live notifications refresh it.
 * @returns The page.
 */
export function TicketPage() {
    const { t } = useLingui();
    const { id = "" } = useParams();
    const ticket = useQuery(ticketQuery(id));

    if (ticket.isPending) {
        return <SkeletonPage />;
    }

    if (ticket.isError) {
        // The reason is a toast (the query cache); the page keeps its frame.
        return <PageHeader title={t`Звернення`} breadcrumbs={[{ text: t`Звернення`, to: "/requests" }]} />;
    }

    const data = ticket.data;
    const fromAdmin = data.canManage && !data.isMine;
    return (
        <>
            <PageHeader
                title={data.subject}
                breadcrumbs={
                    fromAdmin
                        ? [
                              { text: t`Адміністрування`, to: "/admin" },
                              { text: t`Звернення`, to: "/admin/requests" },
                              { text: `№ ${data.number}` },
                          ]
                        : [{ text: t`Звернення`, to: "/requests" }, { text: `№ ${data.number}` }]
                }
            >
                <div className={layout.cluster}>
                    <TicketStatusBadge status={data.status} />
                    <span className={[layout.textSm, layout.muted].join(" ")}>
                        № {data.number} · {t(ticketCategoryNames[data.category])} · {formatDateTime(data.createdAt)}
                    </span>
                </div>
            </PageHeader>

            <div className={layout.split}>
                <div className={layout.stackLg}>
                    <TicketThread messages={data.messages} />
                    {data.canReply ? (
                        <ReplyBox
                            ticketId={data.id}
                            placeholder={data.isMine ? t`Додайте подробиці або дайте відповідь…` : t`Відповідь автору звернення…`}
                        />
                    ) : (
                        <Alert tone="outline" icon={Lock}>
                            {t`Звернення закрито: відповісти на нього не можна. Якщо питання лишилося, створіть нове звернення.`}
                        </Alert>
                    )}
                </div>
                <div className={layout.stackLg}>
                    <DetailsCard ticket={data} />
                    {data.canManage && <ManageCard ticket={data} />}
                    {!data.canManage && data.canClose && <CloseCard ticketId={data.id} />}
                </div>
            </div>
        </>
    );
}

/**
 * The facts of a request.
 * @param props The request.
 * @returns The card.
 */
function DetailsCard({ ticket }: { readonly ticket: TicketDetails }) {
    const { t } = useLingui();
    return (
        <Card title={t`Відомості`}>
            <DescriptionList
                items={[
                    [
                        t`Автор`,
                        <PersonName
                            name={ticket.author.fullName}
                            photoId={ticket.author.photoId}
                            meta={ticket.authorPosition ?? undefined}
                        />,
                    ],
                    ...(ticket.authorEmail
                        ? [[t`Електронна пошта`, <a href={`mailto:${ticket.authorEmail}`}>{ticket.authorEmail}</a>] as const]
                        : []),
                    [
                        t`Виконавець`,
                        ticket.assignee ? (
                            <PersonName name={ticket.assignee.fullName} photoId={ticket.assignee.photoId} />
                        ) : (
                            t`Не призначено`
                        ),
                    ],
                    [t`Категорія`, t(ticketCategoryNames[ticket.category])],
                    [t`Пріоритет`, t(ticketPriorityNames[ticket.priority])],
                    [t`Створено`, formatDateTime(ticket.createdAt)],
                    [t`Остання зміна`, formatDateTime(ticket.lastActivityAt)],
                ]}
            />
        </Card>
    );
}

/**
 * The controls of a handler: the state, the handler, the category and the priority.
 * @param props The request.
 * @returns The card.
 */
function ManageCard({ ticket }: { readonly ticket: TicketDetails }) {
    const { t } = useLingui();
    const me = useCurrentUser().id;
    const queryClient = useQueryClient();
    const handlers = useQuery(handlersQuery);

    /**
     * Runs a change and refreshes the request.
     * @param change The change.
     */
    async function run(change: () => Promise<unknown>): Promise<void> {
        try {
            await change();
            await queryClient.invalidateQueries({ queryKey: ticketKeys.all });
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити звернення.`), { tone: "error" });
        }
    }

    return (
        <Card title={t`Опрацювання`} variant="muted">
            <div className={layout.stack}>
                <Field label={t`Стан`}>
                    {(ids) => (
                        <Select
                            {...ids}
                            value={ticket.status}
                            onChange={(value) => void run(() => ticketApi.changeStatus(ticket.id, value as TicketStatus))}
                            options={(Object.keys(ticketStatusNames) as TicketStatus[]).map((status) => ({
                                value: status,
                                label: t(ticketStatusNames[status]),
                            }))}
                        />
                    )}
                </Field>
                <Field label={t`Виконавець`}>
                    {(ids) => (
                        <Select
                            {...ids}
                            value={ticket.assignee?.id ?? ""}
                            onChange={(value) => void run(() => ticketApi.assign(ticket.id, value || null))}
                            options={[
                                { value: "", label: t`Не призначено` },
                                ...(handlers.data ?? []).map((handler) => ({ value: handler.id, label: handler.fullName })),
                            ]}
                        />
                    )}
                </Field>
                {ticket.assignee?.id !== me && (
                    <Button variant="secondary" block icon={UserCheck} onClick={() => void run(() => ticketApi.assign(ticket.id, me))}>
                        {t`Взяти собі`}
                    </Button>
                )}
                <Field label={t`Категорія`}>
                    {(ids) => (
                        <Select
                            {...ids}
                            value={ticket.category}
                            onChange={(value) => void run(() => ticketApi.classify(ticket.id, value as TicketCategory, ticket.priority))}
                            options={(Object.keys(ticketCategoryNames) as TicketCategory[]).map((category) => ({
                                value: category,
                                label: t(ticketCategoryNames[category]),
                            }))}
                        />
                    )}
                </Field>
                <Field label={t`Пріоритет`}>
                    {(ids) => (
                        <Select
                            {...ids}
                            value={ticket.priority}
                            onChange={(value) => void run(() => ticketApi.classify(ticket.id, ticket.category, value as TicketPriority))}
                            options={(Object.keys(ticketPriorityNames) as TicketPriority[]).map((priority) => ({
                                value: priority,
                                label: t(ticketPriorityNames[priority]),
                            }))}
                        />
                    )}
                </Field>
            </div>
        </Card>
    );
}

/**
 * Lets the author close (withdraw) their request.
 * @param props The request.
 * @returns The card.
 */
function CloseCard({ ticketId }: { readonly ticketId: string }) {
    const { t } = useLingui();
    const queryClient = useQueryClient();

    /**
     * Closes the request after a confirmation.
     */
    async function close(): Promise<void> {
        if (
            !(await confirm({
                title: t`Закрити звернення?`,
                text: t`Після закриття в ньому не можна буде писати.`,
                confirmText: t`Закрити`,
            }))
        ) {
            return;
        }

        try {
            await ticketApi.changeStatus(ticketId, "Closed");
            await queryClient.invalidateQueries({ queryKey: ticketKeys.all });
            toast(t`Звернення закрито`);
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити звернення.`), { tone: "error" });
        }
    }

    return (
        <Card variant="muted">
            <div className={layout.stack}>
                <span className={[layout.textSm, layout.muted].join(" ")}>{t`Питання вирішилося або звернення більше не актуальне?`}</span>
                <Button variant="secondary" block icon={Lock} onClick={() => void close()}>
                    {t`Закрити звернення`}
                </Button>
            </div>
        </Card>
    );
}
