import { useLingui } from "@lingui/react/macro";
import { ArrowDown, ArrowUp, ArrowUpDown, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { Link, useSearchParams } from "react-router";
import styles from "./DataGrid.module.css";
import { EmptyState } from "./EmptyState";
import { Icon } from "./Icon";
import { Pagination } from "./Pagination";

/**
 * A column of {@link DataGrid}.
 */
export interface GridColumn<T> {
    /** A unique key. */
    readonly key: string;
    /** The header, also the label of the cell on phones. */
    readonly title: string;
    /** Renders the cell. */
    readonly render: (item: T) => ReactNode;
    /** The `sortBy` value of the API; the header then sorts the list. */
    readonly sortKey?: string;
    /** The main column: shown as the card title on phones. */
    readonly primary?: boolean;
    /** The actions column: right-aligned, as narrow as possible. */
    readonly actions?: boolean;
    /** Numbers: tabular figures, no wrapping. */
    readonly numeric?: boolean;
    /** Keeps the cell on one line. */
    readonly nowrap?: boolean;
}

/**
 * A page of rows.
 */
export interface GridPage<T> {
    readonly items: readonly T[];
    readonly page: number;
    readonly pageSize: number;
    readonly totalCount: number;
}

/**
 * Props of {@link DataGrid}.
 */
export interface DataGridProps<T> {
    /** The columns. */
    readonly columns: readonly GridColumn<T>[];
    /** The page of rows. */
    readonly result: GridPage<T>;
    /** The row key. */
    readonly rowKey: (item: T) => string;
    /** The table caption (read by assistive technology). */
    readonly caption: string;
    /** The icon of the empty state. */
    readonly emptyIcon: LucideIcon;
    /** The title of the empty state. */
    readonly emptyTitle: string;
    /** The text of the empty state. */
    readonly emptyText?: string;
    /** Shows the pagination (off for short lists loaded at once). */
    readonly paged?: boolean;
}

/**
 * A table of server-side paged rows; on phones every row becomes a card. Sorting and paging live in the address
 * (`sortBy`, `descending`, `page`, `pageSize`), so links, reloads and the back button keep the view.
 * @param props The props.
 * @returns The grid.
 */
export function DataGrid<T>({ columns, result, rowKey, caption, emptyIcon, emptyTitle, emptyText, paged = true }: DataGridProps<T>) {
    const { t } = useLingui();
    const [params] = useSearchParams();
    const sortBy = params.get("sortBy");
    const descending = params.get("descending") === "true";

    /**
     * The address that sorts by a column: ascending first, then descending.
     * @param key The sort key.
     * @returns The query string.
     */
    function sortHref(key: string): string {
        const next = new URLSearchParams(params);
        next.set("sortBy", key);
        next.set("descending", String(sortBy === key && !descending));
        next.delete("page");
        return `?${next.toString()}`;
    }

    if (result.items.length === 0) {
        return (
            <div className={styles.grid}>
                <EmptyState icon={emptyIcon} title={emptyTitle} text={emptyText} />
            </div>
        );
    }

    return (
        <>
            <div className={styles.grid}>
                <table className={styles.table}>
                    <caption className={styles.caption}>{caption}</caption>
                    <thead>
                        <tr>
                            {columns.map((column) => {
                                const sorted = column.sortKey !== undefined && column.sortKey === sortBy;
                                return (
                                    <th
                                        key={column.key}
                                        scope="col"
                                        className={column.actions ? styles.actions : undefined}
                                        aria-sort={sorted ? (descending ? "descending" : "ascending") : undefined}
                                    >
                                        {column.actions ? (
                                            <span className={styles.caption}>{column.title}</span>
                                        ) : column.sortKey ? (
                                            <Link to={sortHref(column.sortKey)} title={t`Сортувати`}>
                                                {column.title}
                                                <Icon icon={sorted ? (descending ? ArrowDown : ArrowUp) : ArrowUpDown} size="sm" />
                                            </Link>
                                        ) : (
                                            column.title
                                        )}
                                    </th>
                                );
                            })}
                        </tr>
                    </thead>
                    <tbody>
                        {result.items.map((item) => (
                            <tr key={rowKey(item)}>
                                {columns.map((column) => (
                                    <td
                                        key={column.key}
                                        data-label={column.title}
                                        data-primary={column.primary || undefined}
                                        className={
                                            [
                                                column.actions && styles.actions,
                                                column.numeric && styles.numeric,
                                                column.nowrap && styles.nowrap,
                                            ]
                                                .filter(Boolean)
                                                .join(" ") || undefined
                                        }
                                    >
                                        {column.render(item)}
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            {paged && <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount} />}
        </>
    );
}

/**
 * Props of {@link GridPrimary}.
 */
export interface GridPrimaryProps {
    /** The application route of the item. */
    readonly to: string;
    /** The main text. */
    readonly children: ReactNode;
    /** A muted line under it. */
    readonly secondary?: ReactNode;
}

/**
 * The content of a primary cell: a link to the item and a muted line.
 * @param props The props.
 * @returns The cell content.
 */
export function GridPrimary({ to, children, secondary }: GridPrimaryProps) {
    return (
        <>
            <Link className={styles.primary} to={to}>
                {children}
            </Link>
            {secondary && <span className={styles.secondary}>{secondary}</span>}
        </>
    );
}
