import { useLingui } from "@lingui/react/macro";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { DocumentTitle } from "./DocumentTitle";
import styles from "./PageHeader.module.css";

/**
 * A step of the breadcrumbs.
 */
export interface Breadcrumb {
    /** The text. */
    readonly text: string;
    /** The application route; the current page has none. */
    readonly to?: string;
}

/**
 * Props of {@link PageHeader}.
 */
export interface PageHeaderProps {
    /** The page heading, also the document title. */
    readonly title: string;
    /** A line under the heading. */
    readonly subtitle?: ReactNode;
    /** The way to this page. */
    readonly breadcrumbs?: readonly Breadcrumb[];
    /** Buttons at the right. */
    readonly actions?: ReactNode;
    /** Extra content under the heading (badges, meta). */
    readonly children?: ReactNode;
}

/**
 * The heading of a page: breadcrumbs, title, subtitle and actions. Also sets the document title.
 * @param props The props.
 * @returns The header.
 */
export function PageHeader({ title, subtitle, breadcrumbs, actions, children }: PageHeaderProps) {
    const { t } = useLingui();
    return (
        <header className={styles.header}>
            <DocumentTitle>{title}</DocumentTitle>
            <div className={styles.text}>
                {breadcrumbs && breadcrumbs.length > 0 && (
                    <nav aria-label={t`Навігаційний ланцюжок`}>
                        <ol className={styles.breadcrumbs}>
                            {breadcrumbs.map((item) => (
                                <li key={item.text}>
                                    {item.to ? <Link to={item.to}>{item.text}</Link> : <span aria-current="page">{item.text}</span>}
                                </li>
                            ))}
                        </ol>
                    </nav>
                )}
                <h1 tabIndex={-1}>{title}</h1>
                {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
                {children}
            </div>
            {actions && <div className={styles.actions}>{actions}</div>}
        </header>
    );
}
