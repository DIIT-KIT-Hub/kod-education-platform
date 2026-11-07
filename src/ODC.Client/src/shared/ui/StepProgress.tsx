import { useLingui } from "@lingui/react/macro";
import { Check } from "lucide-react";
import styles from "./StepProgress.module.css";

/**
 * Props of {@link StepProgress}.
 */
export interface StepProgressProps {
    /** The accessible name of the progress. */
    readonly label: string;
    /** The titles of the steps in order. */
    readonly steps: readonly string[];
    /** The index of the current step; earlier steps are shown as done. */
    readonly current: number;
}

/**
 * A read-only stepper for flows the server drives one step at a time (no going back): numbered circles joined by
 * lines, done steps with a check, the current one highlighted. On narrow screens only the circles and a "step N of M"
 * line show. The look matches the stepper of {@link Wizard}.
 * @param props The props.
 * @returns The stepper.
 */
export function StepProgress({ label, steps, current }: StepProgressProps) {
    const { t } = useLingui();
    const number = Math.min(current, steps.length - 1) + 1;
    const total = steps.length;
    const title = steps[number - 1] ?? "";
    return (
        <nav className={styles.progress} aria-label={label}>
            <ol className={styles.list}>
                {steps.map((step, index) => {
                    const state = index < current ? "done" : index === current ? "current" : "upcoming";
                    return (
                        <li key={step} className={styles.item} data-state={state} aria-current={state === "current" ? "step" : undefined}>
                            <span className={styles.marker} aria-hidden="true">
                                <span className={styles.number}>{index + 1}</span>
                                <Check className={styles.check} />
                            </span>
                            <span className={styles.title}>{step}</span>
                        </li>
                    );
                })}
            </ol>
            <p className={styles.counter}>{t`Крок ${number} з ${total} · ${title}`}</p>
        </nav>
    );
}
