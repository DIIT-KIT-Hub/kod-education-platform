import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { LifeBuoy, Plus } from "lucide-react";
import { Link } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { TicketListItem } from "@/shared/api/schema";
import { ticketCategoryNames, ticketStatusNames } from "@/shared/i18n/display";
import { formatAgo, formatDateTime } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { myTicketsQuery } from "@/features/tickets/api";
import { TicketStatusBadge } from "@/features/tickets/components/TicketStatusBadge";
import styles from "@/features/tickets/components/tickets.module.css";
import { useTicketFilters } from "@/features/tickets/components/useTicketFilters";

/**
 * The requests of the current user: bugs, suggestions and questions they sent, with their states.
 * @returns The page.
 */
export function TicketsPage() {
    const { t } = useLingui();
    const { filters, setFilter } = useTicketFilters();
    const tickets = useQuery(myTicketsQuery(filters));

    const columns: GridColumn<TicketListItem>[] = [
        {
            key: "subject",
            title: t`Звернення`,
            sortKey: "number",
            primary: true,
            render: (ticket) => (
                <Link className={styles.subjectLink} to={`/requests/${ticket.id}`}>
                    <span className={styles.number}>№ {ticket.number}</span>
                    <span className={layout.strong}>{ticket.subject}</span>
                </Link>
            ),
        },
        { key: "category", title: t`Категорія`, render: (ticket) => t(ticketCategoryNames[ticket.category]) },
        { key: "status", title: t`Стан`, sortKey: "status", render: (ticket) => <TicketStatusBadge status={ticket.status} /> },
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
                subtitle={t`Повідомте про помилку, запропонуйте покращення або поставте запитання — відповідь прийде сюди й у сповіщення.`}
                actions={
                    <ButtonLink to="/requests/new" icon={Plus}>
                        {t`Нове звернення`}
                    </ButtonLink>
                }
            />

            <div className={styles.toolbar}>
                <SearchInput
                    value={filters.search ?? ""}
                    placeholder={t`Номер або тема`}
                    onSearch={(value) => setFilter("search", value)}
                />
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
            </div>

            {tickets.isPending && <Spinner block />}
            {tickets.isError && <Alert tone="danger">{describeError(tickets.error, t`Не вдалося завантажити звернення.`)}</Alert>}
            {tickets.data && (
                <DataGrid
                    columns={columns}
                    result={tickets.data}
                    rowKey={(ticket) => ticket.id}
                    caption={t`Мої звернення`}
                    emptyIcon={LifeBuoy}
                    emptyTitle={t`Звернень ще немає`}
                    emptyText={t`Знайшли помилку чи маєте ідею? Створіть звернення — його побачать ті, хто опрацьовує звернення.`}
                />
            )}
        </>
    );
}
