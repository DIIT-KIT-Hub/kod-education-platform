import { useLingui } from "@lingui/react/macro";
import styles from "./Spinner.module.css";

/**
 * Props of {@link Spinner}.
 */
export interface SpinnerProps {
    /** Centres the spinner in a padded block (a page or a section that is loading). */
    readonly block?: boolean;
}

/**
 * A loading indicator.
 * @param props The props.
 * @returns The spinner.
 */
export function Spinner({ block }: SpinnerProps) {
    const { t } = useLingui();
    const spinner = <span className={styles.spinner} role="status" aria-label={t`Завантаження`} />;
    return block ? <div className={styles.block}>{spinner}</div> : spinner;
}
