import type { InputHTMLAttributes, ReactNode, Ref } from "react";
import styles from "./Checkbox.module.css";

/**
 * Props of {@link Checkbox}.
 */
export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "children"> {
    /** The input element (react-hook-form registers it). */
    readonly ref?: Ref<HTMLInputElement>;
    /** The label. */
    readonly children: ReactNode;
    /** A hint under the label. */
    readonly hint?: ReactNode;
    /** A radio button instead of a check box. */
    readonly radio?: boolean;
}

/**
 * A check box (or radio button) with its label and an optional hint.
 * @param props The props.
 * @returns The check box.
 */
export function Checkbox({ children, hint, radio, className, ...input }: CheckboxProps) {
    return (
        <label className={[styles.check, className].filter(Boolean).join(" ")}>
            <input {...input} type={radio ? "radio" : "checkbox"} className={styles.input} />
            <span className={styles.text}>
                <span>{children}</span>
                {hint && <span className={styles.hint}>{hint}</span>}
            </span>
        </label>
    );
}
