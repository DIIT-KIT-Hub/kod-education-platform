import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { Icon } from "./Icon";
import styles from "./Tabs.module.css";

/**
 * Props of {@link Tabs}.
 */
export interface TabsProps {
    /** The accessible name of the navigation. */
    readonly label: string;
    /** The tabs ({@link Tab}). */
    readonly children: ReactNode;
}

/**
 * A row of pill-shaped tabs that link to sections (each tab is an address, so the browser history works).
 * @param props The props.
 * @returns The tabs.
 */
export function Tabs({ label, children }: TabsProps) {
    return (
        <nav className={styles.tabs} aria-label={label}>
            {children}
        </nav>
    );
}

/**
 * Props of {@link Tab}.
 */
export interface TabProps {
    /** The application route. */
    readonly to: string;
    /** Whether this tab is the current section. */
    readonly active: boolean;
    /** An icon before the label. */
    readonly icon?: LucideIcon;
    /** The label. */
    readonly children: ReactNode;
}

/**
 * A tab of {@link Tabs}.
 * @param props The props.
 * @returns The tab.
 */
export function Tab({ to, active, icon, children }: TabProps) {
    return (
        <Link to={to} className={styles.tab} aria-current={active ? "page" : undefined}>
            {icon && <Icon icon={icon} />}
            {children}
        </Link>
    );
}
