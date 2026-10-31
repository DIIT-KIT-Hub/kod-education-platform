import { useLingui } from "@lingui/react/macro";
import { Copy } from "lucide-react";
import { Button, type ButtonSize, type ButtonVariant } from "./Button";
import { toast } from "./Toast";

/**
 * Props of {@link CopyButton}.
 */
export interface CopyButtonProps {
    /** The text put on the clipboard. */
    readonly value: string;
    /** The label. */
    readonly children: string;
    /** The look. */
    readonly variant?: ButtonVariant;
    /** The size. */
    readonly size?: ButtonSize | undefined;
}

/**
 * Copies a text to the clipboard and confirms with a notification.
 * @param props The props.
 * @returns The button.
 */
export function CopyButton({ value, children, variant = "secondary", size }: CopyButtonProps) {
    const { t } = useLingui();
    return (
        <Button
            variant={variant}
            size={size}
            icon={Copy}
            onClick={async () => {
                try {
                    await navigator.clipboard.writeText(value);
                    toast(t`Скопійовано`);
                } catch {
                    toast(t`Не вдалося скопіювати`, { danger: true });
                }
            }}
        >
            {children}
        </Button>
    );
}
