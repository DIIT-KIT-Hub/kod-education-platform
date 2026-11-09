import { useLingui } from "@lingui/react/macro";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useId } from "react";
import { Link } from "react-router";
import { formatNumber } from "@/shared/i18n/format";
import { Icon } from "./Icon";
import styles from "./Pagination.module.css";
import { Select } from "./Select";
import { useViewParams } from "@/shared/lib/viewParams";

/** The page sizes offered. */
const defaultPageSizes: readonly number[] = [10, 20, 50, 100];

/**
 * Props of {@link Pagination}.
 */
export interface PaginationProps {
    /** The 1-based current page. */
    readonly page: number;
    /** The page size. */
    readonly pageSize: number;
    /** The number of items. */
    readonly totalCount: number;
    /** The query parameter of the page (another one when a page has two lists). */
    readonly pageParam?: string;
    /** The page sizes offered; null hides the choice. */
    readonly pageSizes?: readonly number[] | null;
}

/**
 * The pages to show: the first, the last and two around the current one; null marks a gap of two pages or more.
 * @param page The current page.
 * @param total The number of pages.
 * @returns The page numbers.
 */
export function pageWindow(page: number, total: number): (number | null)[] {
    const pages = new Set([1, total]);
    for (let p = page - 2; p <= page + 2; p++) {
        if (p >= 1 && p <= total) {
            pages.add(p);
        }
    }

    const result: (number | null)[] = [];
    let previous = 0;
    for (const p of [...pages].sort((a, b) => a - b)) {
        if (p - previous === 2) {
            // A gap of one page shows that page: the ellipsis would take the same room.
            result.push(p - 1);
        } else if (p - previous > 2) {
            result.push(null);
        }

        result.push(p);
        previous = p;
    }

    return result;
}

/**
 * The page links and the page size of a list; both live in the view state of the page (`page`, `pageSize`, see
 * `useViewParams`).
 * @param props The props.
 * @returns The pagination.
 */
export function Pagination({ page, pageSize, totalCount, pageParam = "page", pageSizes = defaultPageSizes }: PaginationProps) {
    const { t } = useLingui();
    const sizeId = useId();
    const [params, setParams, link] = useViewParams();
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
    const last = Math.min(page * pageSize, totalCount);

    /**
     * The link to a page.
     * @param target The page.
     * @returns The link target.
     */
    function pageLink(target: number) {
        const next = new URLSearchParams(params);
        next.set(pageParam, String(Math.min(Math.max(target, 1), totalPages)));
        return link(next);
    }

    const firstText = formatNumber(first);
    const lastText = formatNumber(last);
    const totalText = formatNumber(totalCount);

    return (
        <nav className={styles.pagination} aria-label={t`Сторінки`}>
            <span className={styles.summary}>{t`${firstText}–${lastText} з ${totalText}`}</span>

            {totalPages > 1 && (
                <div className={styles.pages}>
                    <Link
                        className={styles.page}
                        preventScrollReset={pageParam !== "page"}
                        {...pageLink(page - 1)}
                        aria-disabled={page <= 1 || undefined}
                        aria-label={t`Попередня сторінка`}
                    >
                        <Icon icon={ChevronLeft} />
                    </Link>
                    {pageWindow(page, totalPages).map((item, index) =>
                        item === null ? (
                            <span key={`gap-${index}`} className={styles.gap}>
                                …
                            </span>
                        ) : (
                            <Link
                                key={item}
                                className={styles.page}
                                preventScrollReset={pageParam !== "page"}
                                {...pageLink(item)}
                                aria-current={item === page ? "page" : undefined}
                                data-far={(Math.abs(item - page) > 1 && item !== 1 && item !== totalPages) || undefined}
                            >
                                {item}
                            </Link>
                        ),
                    )}
                    <Link
                        className={styles.page}
                        preventScrollReset={pageParam !== "page"}
                        {...pageLink(page + 1)}
                        aria-disabled={page >= totalPages || undefined}
                        aria-label={t`Наступна сторінка`}
                    >
                        <Icon icon={ChevronRight} />
                    </Link>
                </div>
            )}

            {pageSizes && (
                <div className={styles.size}>
                    <label htmlFor={sizeId} className={styles.sizeLabel}>
                        {t`На сторінці`}
                    </label>
                    <Select
                        id={sizeId}
                        compact
                        options={pageSizes.map((size) => ({ value: String(size), label: String(size) }))}
                        value={String(pageSize)}
                        onChange={(value) => {
                            const next = new URLSearchParams(params);
                            next.set("pageSize", value);
                            next.delete(pageParam);
                            setParams(next);
                        }}
                    />
                </div>
            )}
        </nav>
    );
}
