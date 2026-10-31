import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { Icon } from "@/shared/ui/Icon";
import styles from "./AuthHeader.module.css";

/**
 * Props of {@link AuthHeader}.
 */
export interface AuthHeaderProps {
    /** The heading. */
    readonly title: string;
    /** The document title; the heading when omitted. */
    readonly pageTitle?: string;
    /** The explanation under the heading. */
    readonly text?: ReactNode;
    /** An icon above the heading. */
    readonly icon?: LucideIcon;
    /** A large icon above the heading (a finished step). */
    readonly bigIcon?: LucideIcon;
    /** A badge above the heading (e.g. the step). */
    readonly badge?: ReactNode;
}

/**
 * The heading of a sign-in page card.
 * @param props The props.
 * @returns The header.
 */
export function AuthHeader({ title, pageTitle, text, icon, bigIcon, badge }: AuthHeaderProps) {
    return (
        <header className={styles.header}>
            <DocumentTitle>{pageTitle ?? title}</DocumentTitle>
            {icon && (
                <span className={styles.icon}>
                    <Icon icon={icon} />
                </span>
            )}
            {bigIcon && (
                <span className={styles.bigIcon}>
                    <Icon icon={bigIcon} size="xl" />
                </span>
            )}
            {badge && <span className={styles.badge}>{badge}</span>}
            <h1 tabIndex={-1}>{title}</h1>
            {text && <p>{text}</p>}
        </header>
    );
}
