import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { LifeBuoy } from "lucide-react";
import { Link } from "react-router";
import type { TicketListItem } from "@/shared/api/schema";
import { ticketCategoryNames, ticketPriorityNames, ticketStatusNames } from "@/shared/i18n/display";
import { formatAgo, formatDateTime } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { allTicketsQuery } from "@/features/tickets/api";
import { TicketStatusBadge } from "@/features/tickets/components/TicketStatusBadge";
import styles from "@/features/tickets/components/tickets.module.css";
import { useTicketFilters } from "@/features/tickets/components/useTicketFilters";
import { SkeletonTable } from "@/shared/ui/Skeleton";

/**
 * The requests of all users for the people who handle them: open ones, assigned to me or all; by state and category;
 * the ones waiting for an answer are marked.
 * @returns The page.
 */
export function TicketsAdminPage() {
    const { t } = useLingui();
    const { filters, scope, setFilter } = useTicketFilters();
    const tickets = useQuery(allTicketsQuery(filters));

    const columns: GridColumn<TicketListItem>[] = [
        {
            key: "subject",
            title: t`Звернення`,
            sortKey: "number",
            primary: true,
            render: (ticket) => (
                <Link className={styles.subjectLink} to={`/requests/${ticket.id}`}>
                    <span className={styles.number}>
                        № {ticket.number}
                        {ticket.awaitingAnswer && (ticket.status === "New" || ticket.status === "InProgress") && (
                            <span className={styles.awaiting} title={t`Остання відповідь — від автора`}>
                                {t`Очікує відповіді`}
                            </span>
                        )}
                    </span>
                    <span className={layout.strong}>{ticket.subject}</span>
                    <span className={[layout.textXs, layout.muted].join(" ")}>{t(ticketCategoryNames[ticket.category])}</span>
                </Link>
            ),
        },
        {
            key: "author",
            title: t`Автор`,
            render: (ticket) => <PersonName name={ticket.author.fullName} photoId={ticket.author.photoId} />,
        },
        {
            key: "priority",
            title: t`Пріоритет`,
            sortKey: "priority",
            render: (ticket) => (
                <span className={styles.priority} data-priority={ticket.priority}>
                    {t(ticketPriorityNames[ticket.priority])}
                </span>
            ),
        },
        { key: "status", title: t`Стан`, sortKey: "status", render: (ticket) => <TicketStatusBadge status={ticket.status} /> },
        {
            key: "assignee",
            title: t`Виконавець`,
            render: (ticket) => (ticket.assignee ? <PersonName name={ticket.assignee.fullName} photoId={ticket.assignee.photoId} /> : "—"),
        },
        {
            key: "updated",
            title: t`Оновлено`,
            sortKey: "updated",
            nowrap: true,
            render: (ticket) => <span title={formatDateTime(ticket.lastActivityAt)}>{formatAgo(ticket.lastActivityAt)}</span>,
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Звернення`}
                subtitle={t`Помилки, пропозиції та запитання користувачів: відповідайте, призначайте виконавців і змінюйте стан.`}
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Звернення` }]}
            />

            <div className={styles.toolbar}>
                <SearchInput
                    value={filters.search ?? ""}
                    placeholder={t`Номер, тема або автор`}
                    onSearch={(value) => setFilter("search", value)}
                />
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Показати`}
                        value={scope === "open" || scope === "assigned" ? scope : ""}
                        onChange={(value) => setFilter("scope", value)}
                        options={[
                            { value: "", label: t`Усі звернення` },
                            { value: "open", label: t`Відкриті` },
                            { value: "assigned", label: t`Призначені мені` },
                        ]}
                    />
                </div>
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Стан`}
                        value={filters.status ?? ""}
                        onChange={(value) => setFilter("status", value)}
                        options={[
                            { value: "", label: t`Усі стани` },
                            ...Object.entries(ticketStatusNames).map(([value, name]) => ({ value, label: t(name) })),
                        ]}
                    />
                </div>
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Категорія`}
                        value={filters.category ?? ""}
                        onChange={(value) => setFilter("category", value)}
                        options={[
                            { value: "", label: t`Усі категорії` },
                            ...Object.entries(ticketCategoryNames).map(([value, name]) => ({ value, label: t(name) })),
                        ]}
                    />
                </div>
            </div>

            {tickets.isPending && <SkeletonTable rows={8} columns={4} />}
            {tickets.data && (
                <DataGrid
                    columns={columns}
                    result={tickets.data}
                    rowKey={(ticket) => ticket.id}
                    caption={t`Звернення користувачів`}
                    emptyIcon={LifeBuoy}
                    emptyTitle={t`Звернень не знайдено`}
                />
            )}
        </>
    );
}
