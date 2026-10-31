import type { ReactNode } from "react";
import styles from "./Card.module.css";

/**
 * Card looks.
 */
export type CardVariant = "default" | "muted" | "inverse" | "flush";

/**
 * Props of {@link Card}.
 */
export interface CardProps {
    /** The title. */
    readonly title?: ReactNode;
    /** A line under the title. */
    readonly subtitle?: ReactNode;
    /** Actions at the right of the header. */
    readonly actions?: ReactNode;
    /** The look. */
    readonly variant?: CardVariant;
    /** The heading level of the title. */
    readonly headingLevel?: 2 | 3;
    /** The content. */
    readonly children?: ReactNode;
    /** An extra class. */
    readonly className?: string;
}

/**
 * A surface that groups content, with an optional header.
 * @param props The props.
 * @returns The card.
 */
export function Card({ title, subtitle, actions, variant = "default", headingLevel = 2, children, className }: CardProps) {
    const Heading = headingLevel === 2 ? "h2" : "h3";
    return (
        <section
            className={[styles.card, styles[variant], className].filter(Boolean).join(" ")}
            data-inverse={variant === "inverse" || undefined}
        >
            {(title || actions) && (
                <header className={styles.header}>
                    <div>
                        {title && <Heading className={styles.title}>{title}</Heading>}
                        {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
                    </div>
                    {actions}
                </header>
            )}
            {children}
        </section>
    );
}
