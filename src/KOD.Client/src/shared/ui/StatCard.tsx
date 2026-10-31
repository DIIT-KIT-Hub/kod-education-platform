import { ArrowRight, type LucideIcon } from "lucide-react";
import { Link } from "react-router";
import { Icon } from "./Icon";
import styles from "./StatCard.module.css";

/**
 * Props of {@link StatCard}.
 */
export interface StatCardProps {
    /** What the number counts. */
    readonly label: string;
    /** The number. */
    readonly value: string;
    /** The icon. */
    readonly icon: LucideIcon;
    /** An application route with the counted items. */
    readonly to?: string;
    /** A URL outside the application with the counted items (full page load). */
    readonly href?: string;
    /** Highlights the card (something needs attention). */
    readonly inverse?: boolean;
}

/**
 * A key figure on a card; links to the counted items.
 * @param props The props.
 * @returns The card.
 */
export function StatCard({ label, value, icon, to, href, inverse }: StatCardProps) {
    const className = [styles.card, inverse && styles.inverse].filter(Boolean).join(" ");
    const content = (
        <>
            <span className={styles.top}>
                <span className={styles.icon}>
                    <Icon icon={icon} />
                </span>
                {(to ?? href) && <Icon icon={ArrowRight} className={styles.arrow} />}
            </span>
            <span className={styles.value}>{value}</span>
            <span className={styles.label}>{label}</span>
        </>
    );

    if (to) {
        return (
            <Link to={to} className={className} data-inverse={inverse || undefined}>
                {content}
            </Link>
        );
    }

    return href ? (
        <a href={href} className={className} data-inverse={inverse || undefined}>
            {content}
        </a>
    ) : (
        <div className={className} data-inverse={inverse || undefined}>
            {content}
        </div>
    );
}
