import { useId, type ReactNode } from "react";
import { inkHandlers } from "@/shared/lib/ink";
import styles from "./Segmented.module.css";

/**
 * An option of {@link Segmented}.
 */
export interface SegmentedOption<T extends string> {
    /** The value. */
    readonly value: T;
    /** The label. */
    readonly label: ReactNode;
    /** A small line under the label. */
    readonly hint?: ReactNode;
    /** Makes the option unavailable. */
    readonly disabled?: boolean;
}

/**
 * Props of {@link Segmented}.
 */
export interface SegmentedProps<T extends string> {
    /** The accessible name of the group. */
    readonly label: string;
    /** The options. */
    readonly options: readonly SegmentedOption<T>[];
    /** The chosen value. */
    readonly value: T;
    /** Called with the new value. */
    readonly onChange: (value: T) => void;
}

/**
 * A choice of one option shown as cards (radio buttons); the chosen card is filled and carries a check mark.
 * @param props The props.
 * @returns The group.
 */
export function Segmented<T extends string>({ label, options, value, onChange }: SegmentedProps<T>) {
    const name = useId();
    return (
        <div className={styles.segmented} role="radiogroup" aria-label={label}>
            {options.map((option) => (
                <label key={option.value} className={styles.option} {...inkHandlers}>
                    <input
                        type="radio"
                        name={name}
                        value={option.value}
                        checked={option.value === value}
                        disabled={option.disabled}
                        onChange={() => onChange(option.value)}
                    />
                    <span className={styles.label}>
                        {option.label}
                        {option.hint && <small>{option.hint}</small>}
                    </span>
                </label>
            ))}
        </div>
    );
}
