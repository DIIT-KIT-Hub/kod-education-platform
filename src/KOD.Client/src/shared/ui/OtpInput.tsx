import type { FocusEvent, Ref } from "react";
import styles from "./OtpInput.module.css";

/**
 * Props of {@link OtpInput}.
 */
export interface OtpInputProps {
    /** The digits typed so far. */
    readonly value: string;
    /** Called with the new digits (non-digits are dropped, the length is capped). */
    readonly onChange: (value: string) => void;
    /** Called once every cell is filled (e.g. to submit the form). */
    readonly onComplete?: (value: string) => void;
    /** Called when the input loses focus (react-hook-form touches the field). */
    readonly onBlur?: () => void;
    /** The input element. */
    readonly ref?: Ref<HTMLInputElement>;
    /** The number of digits. */
    readonly length?: number;
    /** The input id (the field label points to it). */
    readonly id?: string;
    /** The input name. */
    readonly name?: string;
    /** The ids of the hint and the error. */
    readonly "aria-describedby"?: string | undefined;
    /** Whether the value is invalid. */
    readonly "aria-invalid"?: boolean | undefined;
    /** Focuses the input on mount. */
    readonly autoFocus?: boolean;
    /** Blocks the input. */
    readonly disabled?: boolean;
}

/**
 * Keeps the caret at the end of the input, where the next digit goes.
 * @param input The input.
 */
function caretToEnd(input: HTMLInputElement): void {
    requestAnimationFrame(() => input.setSelectionRange(input.value.length, input.value.length));
}

/**
 * A one-time code drawn as digit cells. The real input lies transparent over the cells, so typing, paste and
 * one-time-code autofill work as usual; the cells only draw its digits and the next free one is active.
 * @param props The props.
 * @returns The input.
 */
export function OtpInput({ value, onChange, onComplete, onBlur, ref, length = 6, ...input }: OtpInputProps) {
    const digits = value.slice(0, length);
    const active = Math.min(digits.length, length - 1);

    return (
        <div className={styles.otp}>
            <input
                {...input}
                ref={ref}
                className={styles.input}
                value={digits}
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={length + 1}
                onChange={(event) => {
                    const next = event.target.value.replace(/\D/g, "").slice(0, length);
                    onChange(next);
                    if (next.length === length && next !== digits) {
                        onComplete?.(next);
                    }
                }}
                onFocus={(event: FocusEvent<HTMLInputElement>) => caretToEnd(event.currentTarget)}
                onClick={(event) => caretToEnd(event.currentTarget)}
                onBlur={onBlur}
            />
            <div className={styles.cells} aria-hidden="true">
                {Array.from({ length }, (_, index) => {
                    const digit = digits.charAt(index);
                    return (
                        // The key changes with the digit, so a newly typed digit pops in.
                        <span
                            key={`${index}-${digit}`}
                            className={styles.cell}
                            data-filled={digit ? "" : undefined}
                            data-active={index === active ? "" : undefined}
                        >
                            {digit}
                        </span>
                    );
                })}
            </div>
        </div>
    );
}
