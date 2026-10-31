import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import styles from "./EmptyState.module.css";
import { Icon } from "./Icon";

/**
 * Props of {@link EmptyState}.
 */
export interface EmptyStateProps {
    /** The icon. */
    readonly icon: LucideIcon;
    /** The title. */
    readonly title: ReactNode;
    /** An explanation. */
    readonly text?: ReactNode;
    /** Actions. */
    readonly children?: ReactNode;
}

/**
 * A placeholder for an empty list or section.
 * @param props The props.
 * @returns The placeholder.
 */
export function EmptyState({ icon, title, text, children }: EmptyStateProps) {
    return (
        <div className={styles.empty}>
            <div className={styles.icon}>
                <Icon icon={icon} size="lg" />
            </div>
            <p className={styles.title}>{title}</p>
            {text && <p className={styles.text}>{text}</p>}
            {children && <div className={styles.actions}>{children}</div>}
        </div>
    );
}
