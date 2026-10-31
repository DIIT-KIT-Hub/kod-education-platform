import { useLingui } from "@lingui/react/macro";
import { X } from "lucide-react";
import { useEffect, useRef, type ReactNode } from "react";
import { Button } from "./Button";
import styles from "./Dialog.module.css";

/**
 * Props of {@link Dialog}.
 */
export interface DialogProps {
    /** Whether the dialog is shown. */
    readonly open: boolean;
    /** Called when the user closes it (Escape, the close button, the backdrop). */
    readonly onClose: () => void;
    /** The title. */
    readonly title: ReactNode;
    /** The content. */
    readonly children?: ReactNode;
    /** The buttons at the bottom. */
    readonly actions?: ReactNode;
    /** A wider dialog (forms, previews). */
    readonly wide?: boolean;
    /** A line under the title. */
    readonly description?: ReactNode;
    /** Keeps the dialog open while an action runs (Escape, the backdrop and the close button do nothing). */
    readonly locked?: boolean;
}

/**
 * A modal dialog built on the native dialog element (focus trap, Escape and the top layer come from the browser).
 * @param props The props.
 * @returns The dialog.
 */
export function Dialog({ open, onClose, title, children, actions, wide, description, locked }: DialogProps) {
    const { t } = useLingui();
    const ref = useRef<HTMLDialogElement>(null);

    /**
     * Closes the dialog unless it is locked.
     */
    function close(): void {
        if (!locked) {
            onClose();
        }
    }

    useEffect(() => {
        const dialog = ref.current;
        if (!dialog) {
            return;
        }

        if (open && !dialog.open) {
            dialog.showModal();
        } else if (!open && dialog.open) {
            dialog.close();
        }
    }, [open]);

    return (
        <dialog
            ref={ref}
            className={[styles.dialog, wide && styles.wide].filter(Boolean).join(" ")}
            onCancel={(event) => {
                event.preventDefault();
                close();
            }}
            onClick={(event) => {
                // A click on the dialog element itself (not its content) is a click on the backdrop.
                if (event.target === event.currentTarget) {
                    close();
                }
            }}
        >
            <div className={styles.inner}>
                <header className={styles.header}>
                    <div className={styles.heading}>
                        <h2 className={styles.title}>{title}</h2>
                        {description && <p className={styles.description}>{description}</p>}
                    </div>
                    <Button variant="ghost" size="sm" iconOnly icon={X} disabled={locked} onClick={close}>
                        {t`Закрити`}
                    </Button>
                </header>
                {children}
                {actions && <div className={styles.actions}>{actions}</div>}
            </div>
        </dialog>
    );
}
