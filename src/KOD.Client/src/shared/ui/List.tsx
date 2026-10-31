import type { ReactNode } from "react";
import { Link } from "react-router";
import layout from "@/shared/styles/layout.module.css";
import styles from "./List.module.css";

/**
 * Props of {@link List}.
 */
export interface ListProps {
    /** The rows ({@link ListItem}). */
    readonly children: ReactNode;
}

/**
 * A list of rows separated by lines; the rows fade in one after another.
 * @param props The props.
 * @returns The list.
 */
export function List({ children }: ListProps) {
    return <ul className={[styles.list, layout.stagger].join(" ")}>{children}</ul>;
}

/**
 * Props of {@link ListItem}.
 */
export interface ListItemProps {
    /** The bold first line. */
    readonly title: ReactNode;
    /** The muted second line. */
    readonly meta?: ReactNode;
    /** Content before the text (an avatar, an icon). */
    readonly start?: ReactNode;
    /** Content after the text (an icon, a badge). */
    readonly end?: ReactNode;
    /** An application route: the whole row becomes a link. */
    readonly to?: string;
    /** A URL outside the application (full page load). */
    readonly href?: string;
}

/**
 * A row of a {@link List}.
 * @param props The props.
 * @returns The row.
 */
export function ListItem({ title, meta, start, end, to, href }: ListItemProps) {
    const content = (
        <>
            {start}
            <span className={styles.body}>
                <span className={styles.title}>{title}</span>
                {meta && <span className={styles.meta}>{meta}</span>}
            </span>
            {end}
        </>
    );

    return (
        <li className={styles.row}>
            {to ? (
                <Link className={[styles.item, styles.link].join(" ")} to={to}>
                    {content}
                </Link>
            ) : href ? (
                <a className={[styles.item, styles.link].join(" ")} href={href}>
                    {content}
                </a>
            ) : (
                <div className={styles.item}>{content}</div>
            )}
        </li>
    );
}
