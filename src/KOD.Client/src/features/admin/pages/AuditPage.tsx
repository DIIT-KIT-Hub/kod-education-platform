import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { History } from "lucide-react";
import { describeError } from "@/shared/api/problem";
import type { AuditEntryDto } from "@/shared/api/schema";
import { auditActionNames, auditCategories } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { DatePicker } from "@/shared/ui/DatePicker";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { auditQuery } from "../api";
import { useListParams } from "../components/useListParams";
import styles from "./admin.module.css";

/**
 * The audit log: sign-ins, changes of rights and accounts, document actions; filtered by category, dates and text.
 * @returns The page.
 */
export function AuditPage() {
    const { t } = useLingui();
    const { list, get, setFilter } = useListParams(50);
    const category = auditCategories.find((item) => item.prefix === get("category"))?.prefix;
    const from = get("from");
    const to = get("to");
    const entries = useQuery(auditQuery({ ...list, category, from, to }));

    const columns: GridColumn<AuditEntryDto>[] = [
        { key: "time", title: t`Час`, sortKey: "occurred", nowrap: true, render: (entry) => formatDateTime(entry.occurredAt) },
        {
            key: "event",
            title: t`Подія`,
            primary: true,
            render: (entry) => {
                const name = auditActionNames[entry.action];
                return (
                    <span className={layout.stackSm}>
                        <span className={layout.strong}>{name ? t(name) : entry.action}</span>
                        {entry.details && <span className={[layout.textSm, layout.muted].join(" ")}>{entry.details}</span>}
                    </span>
                );
            },
        },
        {
            key: "user",
            title: t`Користувач`,
            render: (entry) => (entry.userName ? <PersonName name={entry.userName} photoId={entry.userPhotoId} /> : "—"),
        },
        { key: "ip", title: "IP", render: (entry) => <span className={layout.mono}>{entry.ipAddress ?? "—"}</span> },
    ];

    return (
        <>
            <PageHeader
                title={t`Журнал аудиту`}
                subtitle={t`Входи в систему, зміни прав і облікових записів, дії з документами.`}
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Журнал аудиту` }]}
            />

            <div className={styles.toolbar}>
                <SearchInput
                    value={list.search ?? ""}
                    placeholder={t`Користувач, деталі або IP`}
                    onSearch={(value) => setFilter("search", value)}
                />
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Категорія`}
                        value={category ?? ""}
                        onChange={(value) => setFilter("category", value)}
                        options={[
                            { value: "", label: t`Усі події` },
                            ...auditCategories.map((item) => ({ value: item.prefix, label: t(item.name) })),
                        ]}
                    />
                </div>
                <div className={styles.filter}>
                    <DatePicker
                        compact
                        aria-label={t`З дати`}
                        placeholder={t`З дати`}
                        value={from ?? ""}
                        max={to}
                        onChange={(value) => setFilter("from", value)}
                    />
                </div>
                <div className={styles.filter}>
                    <DatePicker
                        compact
                        aria-label={t`По дату`}
                        placeholder={t`По дату`}
                        value={to ?? ""}
                        min={from}
                        onChange={(value) => setFilter("to", value)}
                    />
                </div>
            </div>

            {entries.isPending && <Spinner block />}
            {entries.isError && <Alert tone="danger">{describeError(entries.error, t`Не вдалося завантажити журнал.`)}</Alert>}
            {entries.data && (
                <DataGrid
                    columns={columns}
                    result={entries.data}
                    rowKey={(entry) => entry.id}
                    caption={t`Журнал аудиту`}
                    emptyIcon={History}
                    emptyTitle={t`Подій не знайдено`}
                />
            )}
        </>
    );
}
