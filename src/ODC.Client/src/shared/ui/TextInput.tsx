import type { InputHTMLAttributes, Ref } from "react";
import styles from "./TextInput.module.css";

/**
 * Props of {@link TextInput}.
 */
export interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
    /** The input element (react-hook-form registers it). */
    readonly ref?: Ref<HTMLInputElement>;
    /** The compact size. */
    readonly compact?: boolean;
}

/**
 * A text input. Focus is shown by the border alone.
 * @param props The props.
 * @returns The input.
 */
export function TextInput({ compact, className, ...rest }: TextInputProps) {
    return <input {...rest} className={[styles.input, compact && styles.compact, className].filter(Boolean).join(" ")} />;
}
