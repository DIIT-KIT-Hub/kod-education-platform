import { useLingui } from "@lingui/react/macro";
import styles from "./Progress.module.css";

/**
 * A progress bar of unknown length (an action is running).
 * @returns The bar.
 */
export function Progress() {
    const { t } = useLingui();
    return (
        <div className={styles.progress} role="progressbar" aria-label={t`Завантаження`}>
            <div className={styles.bar} />
        </div>
    );
}
