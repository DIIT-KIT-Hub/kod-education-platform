import type { Ref, TextareaHTMLAttributes } from "react";
import styles from "./TextInput.module.css";

/**
 * Props of {@link Textarea}.
 */
export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
    /** The element (react-hook-form registers it). */
    readonly ref?: Ref<HTMLTextAreaElement>;
}

/**
 * A multi-line text input; looks like {@link TextInput}.
 * @param props The props.
 * @returns The text area.
 */
export function Textarea({ className, ...rest }: TextareaProps) {
    return <textarea {...rest} className={[styles.input, styles.textarea, className].filter(Boolean).join(" ")} />;
}
