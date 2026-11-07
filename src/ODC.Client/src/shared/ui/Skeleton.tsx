import { useLingui } from "@lingui/react/macro";
import type { CSSProperties, ReactNode } from "react";
import styles from "./Skeleton.module.css";

/**
 * Widths of text lines, taken in turn so that a placeholder looks like real text.
 */
const lineWidths = ["92%", "76%", "84%", "64%", "88%", "70%"] as const;

/**
 * A line width for a position.
 * @param index The position.
 * @returns The width.
 */
function lineWidth(index: number): string {
    return lineWidths[index % lineWidths.length] ?? "80%";
}

/**
 * Props of {@link Skeleton}.
 */
export interface SkeletonProps {
    /** The width (CSS length); a line fills its container by default. */
    readonly width?: string;
    /** The height (CSS length); a line of text by default. */
    readonly height?: string;
    /** The shape: a rounded line, a circle (avatars) or a block with rounded corners (images, inputs). */
    readonly shape?: "line" | "circle" | "block";
    /** An extra class. */
    readonly className?: string;
}

/**
 * One placeholder shape. Compose them inside a {@link SkeletonGroup}, which announces the loading.
 * @param props The props.
 * @returns The placeholder.
 */
export function Skeleton({ width, height, shape = "line", className }: SkeletonProps) {
    const size: CSSProperties = { width: width ?? (shape === "circle" ? height : undefined), height };
    return (
        <span
            className={[styles.bone, shape === "circle" && styles.circle, shape === "block" && styles.block, className]
                .filter(Boolean)
                .join(" ")}
            style={size}
            aria-hidden="true"
        />
    );
}

/**
 * Props of {@link SkeletonGroup}.
 */
export interface SkeletonGroupProps {
    /** The placeholder shapes. */
    readonly children: ReactNode;
    /** An extra class. */
    readonly className?: string;
}

/**
 * The container of a placeholder: tells assistive technology that content is loading and fades the shapes in after a
 * short delay, so fast answers do not flash them.
 * @param props The props.
 * @returns The container.
 */
export function SkeletonGroup({ children, className }: SkeletonGroupProps) {
    const { t } = useLingui();
    return (
        <div className={[styles.skeleton, className].filter(Boolean).join(" ")} role="status" aria-busy="true" aria-label={t`Завантаження`}>
            {children}
        </div>
    );
}

/**
 * Lines of text.
 * @param props The props.
 * @param props.lines The number of lines.
 * @returns The lines (without a group: use inside one).
 */
function Lines({ lines }: { readonly lines: number }) {
    return (
        <div className={styles.lines}>
            {Array.from({ length: lines }, (_, index) => (
                <Skeleton key={index} width={index === lines - 1 && lines > 1 ? "48%" : lineWidth(index)} />
            ))}
        </div>
    );
}

/**
 * The header of a page: breadcrumbs, title and subtitle.
 * @returns The header shapes.
 */
function Header() {
    return (
        <div className={styles.header}>
            <Skeleton width="10rem" height="0.75rem" />
            <Skeleton width="min(24rem, 80%)" className={styles.title} />
            <Skeleton width="min(32rem, 95%)" height="1rem" />
        </div>
    );
}

/**
 * Props of {@link SkeletonText}.
 */
export interface SkeletonTextProps {
    /** The number of lines. */
    readonly lines?: number;
}

/**
 * A paragraph of text that is loading.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonText({ lines = 4 }: SkeletonTextProps) {
    return (
        <SkeletonGroup>
            <Lines lines={lines} />
        </SkeletonGroup>
    );
}

/**
 * Props of {@link SkeletonPage}.
 */
export interface SkeletonPageProps {
    /**
     * The layout: a card page with a side column (`detail`), a form (`form`) or an article with a cover (`article`).
     */
    readonly variant?: "detail" | "form" | "article";
    /** Draws the page header too (off when the page already shows its own). */
    readonly header?: boolean;
    /** Pads the page on its own, outside the application shell (the first load of the application). */
    readonly screen?: boolean;
}

/**
 * A whole page that is loading.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonPage({ variant = "detail", header = true, screen = false }: SkeletonPageProps) {
    return (
        <SkeletonGroup className={screen ? styles.screen : undefined}>
            {header && <Header />}
            {variant === "form" && (
                <div className={styles.panel}>
                    {Array.from({ length: 4 }, (_, index) => (
                        <div key={index} className={styles.field}>
                            <Skeleton width={index % 2 === 0 ? "8rem" : "11rem"} height="0.75rem" />
                            <Skeleton shape="block" className={styles.input} />
                        </div>
                    ))}
                    <Skeleton width="9rem" className={styles.button} />
                </div>
            )}
            {variant === "article" && (
                <div className={styles.panel}>
                    <Skeleton shape="block" className={styles.cover} />
                    <Lines lines={6} />
                    <Lines lines={4} />
                </div>
            )}
            {variant === "detail" && (
                <div className={styles.split}>
                    <div className={styles.column}>
                        <div className={styles.panel}>
                            <Skeleton width="12rem" height="1.25rem" />
                            <Lines lines={4} />
                        </div>
                        <div className={styles.panel}>
                            <Skeleton width="9rem" height="1.25rem" />
                            <Lines lines={3} />
                        </div>
                    </div>
                    <div className={styles.panel}>
                        <Skeleton width="8rem" height="1.25rem" />
                        {Array.from({ length: 5 }, (_, index) => (
                            <div key={index} className={styles.pair}>
                                <Skeleton width="80%" height="0.75rem" />
                                <Skeleton width={lineWidth(index + 1)} />
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </SkeletonGroup>
    );
}

/**
 * Props of {@link SkeletonTable}.
 */
export interface SkeletonTableProps {
    /** The number of rows. */
    readonly rows?: number;
    /** The number of columns after the main one. */
    readonly columns?: number;
    /** Draws an avatar in the main column. */
    readonly avatar?: boolean;
}

/**
 * A table of rows that is loading, shaped like `DataGrid`.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonTable({ rows = 6, columns = 3, avatar = false }: SkeletonTableProps) {
    const style = { "--_columns": columns } as CSSProperties;
    return (
        <SkeletonGroup>
            <div className={styles.table} style={style}>
                <div className={[styles.tableRow, styles.tableHead].join(" ")}>
                    {Array.from({ length: columns + 1 }, (_, index) => (
                        <Skeleton key={index} width={index === 0 ? "6rem" : "4rem"} height="0.625rem" />
                    ))}
                </div>
                {Array.from({ length: rows }, (_, row) => (
                    <div key={row} className={styles.tableRow}>
                        <div className={styles.cell}>
                            {avatar && <Skeleton shape="circle" height="2.25rem" />}
                            <div className={styles.lines}>
                                <Skeleton width={lineWidth(row)} />
                                <Skeleton width="45%" height="0.625rem" />
                            </div>
                        </div>
                        {Array.from({ length: columns }, (_, column) => (
                            <Skeleton key={column} width={lineWidth(row + column + 2)} />
                        ))}
                    </div>
                ))}
            </div>
        </SkeletonGroup>
    );
}

/**
 * Props of {@link SkeletonList}.
 */
export interface SkeletonListProps {
    /** The number of rows. */
    readonly rows?: number;
    /** The picture at the start of a row: an avatar, an icon or none. */
    readonly media?: "avatar" | "icon" | "none";
    /** The number of text lines in a row. */
    readonly lines?: number;
    /** Indents some rows, like replies in a discussion or branches of a tree. */
    readonly nested?: boolean;
    /** Draws the list on a card surface. */
    readonly framed?: boolean;
}

/**
 * Depths of rows in a nested list, taken in turn.
 */
const depths = [0, 1, 2, 1, 0, 1] as const;

/**
 * A list of rows that is loading: chats, notifications, people, comments, a tree.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonList({ rows = 5, media = "avatar", lines = 2, nested = false, framed = false }: SkeletonListProps) {
    return (
        <SkeletonGroup>
            <div className={[styles.list, framed && styles.panel].filter(Boolean).join(" ")}>
                {Array.from({ length: rows }, (_, row) => (
                    <div
                        key={row}
                        className={styles.listRow}
                        data-indent={nested ? (depths[row % depths.length] ?? 0) : 0}
                        data-top={lines > 2 || undefined}
                    >
                        {media === "avatar" && <Skeleton shape="circle" height="2.5rem" />}
                        {media === "icon" && <Skeleton shape="block" width="1.75rem" height="1.75rem" />}
                        <Lines lines={lines} />
                    </div>
                ))}
            </div>
        </SkeletonGroup>
    );
}

/**
 * Props of {@link SkeletonCards}.
 */
export interface SkeletonCardsProps {
    /** The number of cards. */
    readonly count?: number;
    /** The cards: news cards with a cover or statistic tiles. */
    readonly variant?: "news" | "stats";
}

/**
 * A grid of cards that is loading.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonCards({ count = 6, variant = "news" }: SkeletonCardsProps) {
    const style = { "--_card-min": variant === "news" ? "19rem" : "13rem" } as CSSProperties;
    return (
        <SkeletonGroup>
            <div className={styles.cards} style={style}>
                {Array.from({ length: count }, (_, index) => (
                    <div key={index} className={styles.card}>
                        {variant === "news" && <Skeleton shape="block" className={styles.cover} />}
                        <div className={styles.cardBody}>
                            {variant === "news" ? (
                                <>
                                    <Skeleton width="6rem" height="0.625rem" />
                                    <Skeleton width={lineWidth(index)} height="1.125rem" />
                                    <Lines lines={2} />
                                </>
                            ) : (
                                <>
                                    <Skeleton shape="block" width="2.5rem" height="2.5rem" />
                                    <Skeleton width="4rem" height="1.75rem" />
                                    <Skeleton width={lineWidth(index)} height="0.75rem" />
                                </>
                            )}
                        </div>
                    </div>
                ))}
            </div>
        </SkeletonGroup>
    );
}

/**
 * Sides and widths of message bubbles, taken in turn.
 */
const bubbles = [
    { mine: false, width: "14rem" },
    { mine: false, width: "20rem" },
    { mine: true, width: "11rem" },
    { mine: false, width: "17rem" },
    { mine: true, width: "22rem" },
    { mine: true, width: "9rem" },
] as const;

/**
 * Props of {@link SkeletonMessages}.
 */
export interface SkeletonMessagesProps {
    /** The number of messages. */
    readonly count?: number;
    /** An extra class. */
    readonly className?: string;
}

/**
 * Messages of a conversation that are loading: bubbles on both sides.
 * @param props The props.
 * @returns The placeholder.
 */
export function SkeletonMessages({ count = 6, className }: SkeletonMessagesProps) {
    return (
        <SkeletonGroup className={[styles.messages, className].filter(Boolean).join(" ")}>
            {Array.from({ length: count }, (_, index) => {
                const bubble = bubbles[index % bubbles.length] ?? bubbles[0];
                return (
                    <div key={index} className={styles.message} data-mine={bubble.mine || undefined}>
                        {!bubble.mine && <Skeleton shape="circle" height="2rem" />}
                        <Skeleton shape="block" width={bubble.width} className={styles.bubble} />
                    </div>
                );
            })}
        </SkeletonGroup>
    );
}
