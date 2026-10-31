import { CircleAlert, CircleCheck } from "lucide-react";
import { useSyncExternalStore } from "react";
import { Icon } from "./Icon";
import styles from "./Toast.module.css";

/**
 * A short notification.
 */
interface ToastMessage {
    /** The identifier. */
    readonly id: number;
    /** The text. */
    readonly text: string;
    /** Whether it reports a failure. */
    readonly danger: boolean;
    /** Whether it is fading out. */
    readonly leaving: boolean;
}

/** How long a notification stays, in milliseconds. */
const visibleFor = 4500;

/** How long the fade-out takes. */
const leavingFor = 220;

let toasts: readonly ToastMessage[] = [];
let nextId = 1;
const listeners = new Set<() => void>();

/**
 * Replaces the notifications and tells the region.
 * @param next The new list.
 */
function update(next: readonly ToastMessage[]): void {
    toasts = next;
    listeners.forEach((listener) => listener());
}

/**
 * Shows a notification at the bottom of the screen for a few seconds.
 * @param text The text (already translated).
 * @param options Whether it reports a failure.
 */
export function toast(text: string, options: { readonly danger?: boolean } = {}): void {
    const id = nextId++;
    update([...toasts, { id, text, danger: options.danger ?? false, leaving: false }]);
    setTimeout(() => {
        update(toasts.map((item) => (item.id === id ? { ...item, leaving: true } : item)));
        setTimeout(() => update(toasts.filter((item) => item.id !== id)), leavingFor);
    }, visibleFor);
}

/**
 * Subscribes to the notifications.
 * @param listener Called on every change.
 * @returns The unsubscribe function.
 */
function subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/**
 * The place where notifications appear; rendered once by the application root.
 * @returns The region.
 */
export function ToastRegion() {
    const items = useSyncExternalStore(subscribe, () => toasts);
    return (
        <div className={styles.region} role="status" aria-live="polite">
            {items.map((item) => (
                <div
                    key={item.id}
                    className={[styles.toast, item.danger && styles.danger].filter(Boolean).join(" ")}
                    data-leaving={item.leaving || undefined}
                >
                    <Icon icon={item.danger ? CircleAlert : CircleCheck} />
                    <span>{item.text}</span>
                </div>
            ))}
        </div>
    );
}
