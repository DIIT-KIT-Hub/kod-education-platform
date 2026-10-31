import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import styles from "./Badge.module.css";
import { Icon } from "./Icon";

/**
 * Badge looks.
 */
export type BadgeTone = "neutral" | "inverse" | "outline" | "dashed" | "danger";

/**
 * Props of {@link Badge}.
 */
export interface BadgeProps {
    /** The look. */
    readonly tone?: BadgeTone;
    /** An icon before the text. */
    readonly icon?: LucideIcon;
    /** A pulsing dot before the text (something in progress). */
    readonly dot?: boolean;
    /** The text. */
    readonly children: ReactNode;
    /** An extra class. */
    readonly className?: string;
}

/**
 * A small pill-shaped label: a status, a count, a step.
 * @param props The props.
 * @returns The badge.
 */
export function Badge({ tone = "neutral", icon, dot, children, className }: BadgeProps) {
    return (
        <span className={[styles.badge, styles[tone], dot && styles.dot, className].filter(Boolean).join(" ")}>
            {icon && <Icon icon={icon} className={styles.icon} />}
            {children}
        </span>
    );
}
