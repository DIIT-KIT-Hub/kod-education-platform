import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, BadgeCheck, FilePlus, FileText, Layers, PenLine, Signature, Users } from "lucide-react";
import { useSearchParams } from "react-router";
import type { DocumentKind, DocumentListItem, DocumentScope, DocumentStatus } from "@/shared/api/schema";
import { describeError } from "@/shared/api/problem";
import { documentKindNames, documentStatusNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, GridPrimary, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { Tab, Tabs } from "@/shared/ui/Tabs";
import { documentListQuery, type DocumentFilters } from "../api";
import { DocumentStatusBadge } from "../components/DocumentStatusBadge";
import styles from "./documents.module.css";

/** The sections of the registry. */
const scopes: readonly DocumentScope[] = ["AwaitingMe", "Authored", "Involved", "SignedByMe", "Accessible"];

/** The states, in the order of the filter. */
const statuses: readonly DocumentStatus[] = ["Draft", "InSigning", "Signed", "Rejected", "Archived"];

/** The kinds, in the order of the filter. */
const kinds = Object.keys(documentKindNames) as DocumentKind[];

/**
 * Reads an allowed value from the address.
 * @param value The value.
 * @param allowed The allowed values.
 * @returns The value, or undefined.
 */
function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
    return allowed.find((item) => item === value);
}

/**
 * Reads a positive number from the address.
 * @param value The value.
 * @param fallback The default.
 * @returns The number.
 */
function positive(value: string | null, fallback: number): number {
    const number = Number(value);
    return Number.isInteger(number) && number > 0 ? number : fallback;
}

/**
 * The document registry: sections, search, filters, sorting and pages, all kept in the address.
 * @returns The page.
 */
export function DocumentsPage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const [params, setParams] = useSearchParams();
    const filters: DocumentFilters = {
        scope: oneOf(params.get("scope"), scopes) ?? "AwaitingMe",
        status: oneOf(params.get("status"), statuses),
        kind: oneOf(params.get("kind"), kinds),
        search: params.get("search") ?? undefined,
        page: positive(params.get("page"), 1),
        pageSize: positive(params.get("pageSize"), 20),
        sortBy: params.get("sortBy") ?? undefined,
        descending: params.get("descending") === "true",
    };
    const result = useQuery(documentListQuery(filters));

    /**
     * Changes a filter and returns to the first page.
     * @param key The query parameter.
     * @param value The value ("" removes it).
     */
    function filter(key: string, value: string): void {
        const next = new URLSearchParams(params);
        if (value) {
            next.set(key, value);
        } else {
            next.delete(key);
        }

        next.delete("page");
        setParams(next, { replace: true });
    }

    /**
     * The address of a section (filters and paging start over).
     * @param scope The section.
     * @returns The address.
     */
    const scopeHref = (scope: DocumentScope) => `?scope=${scope}`;

    const columns: GridColumn<DocumentListItem>[] = [
        {
            key: "title",
            title: t`Документ`,
            sortKey: "title",
            primary: true,
            render: (item) => (
                <GridPrimary to={`/documents/${item.id}`} secondary={`${t(documentKindNames[item.kind])} · ${item.unitName}`}>
                    {item.title}
                </GridPrimary>
            ),
        },
        { key: "number", title: t`Номер`, sortKey: "number", numeric: true, render: (item) => item.registrationNumber },
        {
            key: "status",
            title: t`Статус`,
            sortKey: "status",
            render: (item) => (
                <div className={layout.cluster}>
                    <DocumentStatusBadge status={item.status} />
                    {item.awaitingMe && (
                        <Badge tone="inverse" icon={Signature}>
                            {t`Ваш підпис`}
                        </Badge>
                    )}
                </div>
            ),
        },
        { key: "signatures", title: t`Підписи`, nowrap: true, render: (item) => `${item.signedCount} / ${item.signersCount}` },
        { key: "author", title: t`Автор`, render: (item) => <PersonName name={item.authorName} photoId={item.authorPhotoId} /> },
        { key: "created", title: t`Створено`, sortKey: "created", nowrap: true, render: (item) => formatDateTime(item.createdAt) },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (item) => (
                <ButtonLink to={`/documents/${item.id}`} variant={item.awaitingMe ? "primary" : "ghost"} size="sm" iconEnd={ArrowRight}>
                    {item.awaitingMe ? t`Підписати` : t`Відкрити`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Документи`}
                subtitle={t`Реєстр документів, маршрути погодження та підписання.`}
                actions={
                    hasPermission(user, Permissions.documentsCreate) && (
                        <ButtonLink to="/documents/new" icon={FilePlus}>
                            {t`Новий документ`}
                        </ButtonLink>
                    )
                }
            />

            <Tabs label={t`Розділи документів`}>
                <Tab to={scopeHref("AwaitingMe")} active={filters.scope === "AwaitingMe"} icon={Signature}>
                    {t`Очікують мого підпису`}
                </Tab>
                <Tab to={scopeHref("Authored")} active={filters.scope === "Authored"} icon={PenLine}>
                    {t`Створені мною`}
                </Tab>
                <Tab to={scopeHref("Involved")} active={filters.scope === "Involved"} icon={Users}>
                    {t`Я в маршруті`}
                </Tab>
                <Tab to={scopeHref("SignedByMe")} active={filters.scope === "SignedByMe"} icon={BadgeCheck}>
                    {t`Підписані мною`}
                </Tab>
                <Tab to={scopeHref("Accessible")} active={filters.scope === "Accessible"} icon={Layers}>
                    {t`Усі доступні`}
                </Tab>
            </Tabs>

            <div className={styles.toolbar}>
                <SearchInput
                    value={filters.search ?? ""}
                    placeholder={t`Назва або реєстраційний номер`}
                    onSearch={(value) => filter("search", value)}
                />
                <Select
                    compact
                    aria-label={t`Статус`}
                    value={filters.status ?? ""}
                    onChange={(value) => filter("status", value)}
                    options={[
                        { value: "", label: t`Усі статуси` },
                        ...statuses.map((status) => ({ value: status, label: t(documentStatusNames[status]) })),
                    ]}
                />
                <Select
                    compact
                    aria-label={t`Вид документа`}
                    value={filters.kind ?? ""}
                    onChange={(value) => filter("kind", value)}
                    options={[
                        { value: "", label: t`Усі види` },
                        ...kinds.map((kind) => ({ value: kind, label: t(documentKindNames[kind]) })),
                    ]}
                />
            </div>

            {result.isPending && <Spinner block />}
            {result.isError && <Alert tone="danger">{describeError(result.error, t`Не вдалося завантажити документи.`)}</Alert>}
            {result.data && (
                <DataGrid
                    columns={columns}
                    result={result.data}
                    rowKey={(item) => item.id}
                    caption={t`Документи`}
                    emptyIcon={FileText}
                    emptyTitle={t`Документів не знайдено`}
                    emptyText={
                        filters.scope === "AwaitingMe" && !filters.search
                            ? t`Документів, що очікують вашого підпису, немає.`
                            : t`Спробуйте змінити параметри пошуку або фільтри.`
                    }
                />
            )}
        </>
    );
}
