import type { ReactNode } from "react";
import styles from "./DescriptionList.module.css";

/**
 * Props of {@link DescriptionList}.
 */
export interface DescriptionListProps {
    /** Term / value pairs; pairs without a value are skipped. */
    readonly items: readonly (readonly [ReactNode, ReactNode])[];
    /** Muted terms on an inverse surface. */
    readonly inverse?: boolean;
}

/**
 * Terms and their values in two columns (one column on phones).
 * @param props The props.
 * @returns The list.
 */
export function DescriptionList({ items, inverse }: DescriptionListProps) {
    return (
        <dl className={[styles.list, inverse && styles.inverse].filter(Boolean).join(" ")}>
            {items
                .filter(([, value]) => value !== null && value !== undefined && value !== "")
                .map(([term, value], index) => (
                    <div key={index} className={styles.row}>
                        <dt>{term}</dt>
                        <dd>{value}</dd>
                    </div>
                ))}
        </dl>
    );
}
