import { Trans, useLingui } from "@lingui/react/macro";
import styles from "./Brand.module.css";

/**
 * Props of {@link BrandMark}.
 */
export interface BrandMarkProps {
    /** The smaller mark of the top bar. */
    readonly compact?: boolean;
    /** An extra class. */
    readonly className?: string;
}

/**
 * The square product mark.
 * @param props The props.
 * @returns The mark.
 */
export function BrandMark({ compact, className }: BrandMarkProps) {
    const { t } = useLingui();
    return (
        <span className={[styles.mark, compact && styles.compact, className].filter(Boolean).join(" ")} aria-hidden="true">
            {t`К`}
        </span>
    );
}

/**
 * The full product name, broken into two lines.
 * @returns The name.
 */
export function ProductName() {
    return (
        <Trans>
            Кабінет організаційних
            <br />
            документів
        </Trans>
    );
}
