import { useLingui } from "@lingui/react/macro";
import { useState, useSyncExternalStore } from "react";
import { Button } from "./Button";
import { Checkbox } from "./Checkbox";
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
    /** A check box that widens the action ("also delete for…"). */
    readonly option?: ConfirmOption;
    /** Settles the promise: whether the user confirmed and whether the check box was ticked. */
    readonly resolve: (confirmed: boolean, option: boolean) => void;
}

/**
 * The check box of a confirmation.
 */
export interface ConfirmOption {
    /** The label. */
    readonly label: string;
    /** A hint under the label. */
    readonly hint?: string;
    /** Whether it starts ticked. */
    readonly checked?: boolean;
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
export function confirm(options: Omit<ConfirmRequest, "resolve" | "option">): Promise<boolean> {
    current?.resolve(false, false);
    return new Promise((resolve) => update({ ...options, resolve: (confirmed) => resolve(confirmed) }));
}

/**
 * Asks the user to confirm an action with a check box that widens it (like "also delete for the other person").
 * @param options The question, an explanation, the button label, whether the action is destructive and the check box.
 * @returns Whether the check box was ticked, or null when the user cancelled.
 */
export function confirmWithOption(options: Omit<ConfirmRequest, "resolve"> & { readonly option: ConfirmOption }): Promise<boolean | null> {
    current?.resolve(false, false);
    return new Promise((resolve) => update({ ...options, resolve: (confirmed, option) => resolve(confirmed ? option : null) }));
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
    const [shown, setShown] = useState(request);
    const [checked, setChecked] = useState(false);

    // A new question starts with its own check box state.
    if (shown !== request) {
        setShown(request);
        setChecked(request?.option?.checked ?? false);
    }

    /**
     * Closes the dialog with the answer.
     * @param confirmed The answer.
     */
    function answer(confirmed: boolean): void {
        request?.resolve(confirmed, checked);
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
            {request?.option && (
                <Checkbox checked={checked} onChange={(event) => setChecked(event.target.checked)} hint={request.option.hint}>
                    {request.option.label}
                </Checkbox>
            )}
        </Dialog>
    );
}
