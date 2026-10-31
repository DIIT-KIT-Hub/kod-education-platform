import { useId, type ReactNode } from "react";
import styles from "./Field.module.css";

/**
 * The ids a field gives to its control.
 */
export interface FieldControlProps {
    /** The control id (the label points to it). */
    readonly id: string;
    /** The ids of the hint and the error, for aria-describedby. */
    readonly "aria-describedby": string | undefined;
    /** Whether the value is invalid. */
    readonly "aria-invalid": boolean | undefined;
}

/**
 * Props of {@link Field}.
 */
export interface FieldProps {
    /** The label. */
    readonly label: ReactNode;
    /** A hint under the control. */
    readonly hint?: ReactNode;
    /** The validation error, shown instead of nothing under the control. */
    readonly error?: string | undefined;
    /** Marks the field as required. */
    readonly required?: boolean;
    /** Renders the control with the ids it needs. */
    readonly children: (control: FieldControlProps) => ReactNode;
    /** An extra class. */
    readonly className?: string;
}

/**
 * A form field: label, control, hint and validation error, wired for assistive technology.
 * @param props The props.
 * @returns The field.
 */
export function Field({ label, hint, error, required, children, className }: FieldProps) {
    const id = useId();
    const hintId = hint ? `${id}-hint` : undefined;
    const errorId = error ? `${id}-error` : undefined;
    const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

    return (
        <div className={[styles.field, className].filter(Boolean).join(" ")}>
            <label className={styles.label} htmlFor={id}>
                {label}
                {required && (
                    <span className={styles.required} aria-hidden="true">
                        *
                    </span>
                )}
            </label>
            {children({ id, "aria-describedby": describedBy, "aria-invalid": error ? true : undefined })}
            {hint && (
                <span id={hintId} className={styles.hint}>
                    {hint}
                </span>
            )}
            {error && (
                <span id={errorId} className={styles.error} role="alert">
                    {error}
                </span>
            )}
        </div>
    );
}
