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
    /** An icon beside the heading. */
    readonly icon?: LucideIcon;
    /** The icon of a finished step beside the heading, on an inverse circle (in place of {@link icon}). */
    readonly doneIcon?: LucideIcon;
    /** A badge above the heading (e.g. the step). */
    readonly badge?: ReactNode;
}

/**
 * The heading of a sign-in page card.
 * @param props The props.
 * @returns The header.
 */
export function AuthHeader({ title, pageTitle, text, icon, doneIcon, badge }: AuthHeaderProps) {
    return (
        <header className={styles.header}>
            <DocumentTitle>{pageTitle ?? title}</DocumentTitle>
            {badge && <span className={styles.badge}>{badge}</span>}
            <div className={styles.title}>
                {icon && !doneIcon && (
                    <span className={styles.icon}>
                        <Icon icon={icon} />
                    </span>
                )}
                {doneIcon && (
                    <span className={styles.doneIcon}>
                        <Icon icon={doneIcon} />
                    </span>
                )}
                <h1 tabIndex={-1}>{title}</h1>
            </div>
            {text && <p>{text}</p>}
        </header>
    );
}
