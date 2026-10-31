import { useLingui } from "@lingui/react/macro";
import { useSyncExternalStore } from "react";
import { Button } from "./Button";
import { Dialog } from "./Dialog";

/**
 * A question waiting for the user's answer.
 */
interface ConfirmRequest {
    /** The question. */
    readonly title: string;
    /** An explanation. */
    readonly text?: string;
    /** The label of the confirming button. */
    readonly confirmText?: string;
    /** Whether confirming destroys something (red button). */
    readonly danger?: boolean;
    /** Settles the promise returned by {@link confirm}. */
    readonly resolve: (confirmed: boolean) => void;
}

let current: ConfirmRequest | null = null;
const listeners = new Set<() => void>();

/**
 * Replaces the open question and tells the host.
 * @param next The question, or null to close.
 */
function update(next: ConfirmRequest | null): void {
    current = next;
    listeners.forEach((listener) => listener());
}

/**
 * Asks the user to confirm an action in a modal dialog.
 * @param options The question, an explanation, the button label and whether the action is destructive.
 * @returns Whether the user confirmed.
 */
export function confirm(options: Omit<ConfirmRequest, "resolve">): Promise<boolean> {
    current?.resolve(false);
    return new Promise((resolve) => update({ ...options, resolve }));
}

/**
 * Subscribes to the open question.
 * @param listener Called on every change.
 * @returns The unsubscribe function.
 */
function subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/**
 * Renders the confirmation dialog; rendered once by the application root.
 * @returns The dialog.
 */
export function ConfirmHost() {
    const { t } = useLingui();
    const request = useSyncExternalStore(subscribe, () => current);

    /**
     * Closes the dialog with the answer.
     * @param confirmed The answer.
     */
    function answer(confirmed: boolean): void {
        request?.resolve(confirmed);
        update(null);
    }

    return (
        <Dialog
            open={request !== null}
            onClose={() => answer(false)}
            title={request?.title ?? ""}
            actions={
                <>
                    <Button variant="secondary" onClick={() => answer(false)}>
                        {t`Скасувати`}
                    </Button>
                    <Button variant={request?.danger ? "danger" : "primary"} onClick={() => answer(true)} autoFocus>
                        {request?.confirmText ?? t`Підтвердити`}
                    </Button>
                </>
            }
        >
            {request?.text && <p>{request.text}</p>}
        </Dialog>
    );
}
