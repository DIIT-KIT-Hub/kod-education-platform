import { useLingui } from "@lingui/react/macro";
import { CircleAlert, CircleCheck, Info, TriangleAlert, X, type LucideIcon } from "lucide-react";
import { useEffect, useRef, useSyncExternalStore, type CSSProperties } from "react";
import { Icon } from "./Icon";
import styles from "./Toast.module.css";

/**
 * What a notification reports: a finished action, a failure, something to watch out for, or news.
 */
export type ToastTone = "success" | "error" | "warning" | "info";

/**
 * Options of {@link toast}.
 */
export interface ToastOptions {
    /** What the notification reports (a finished action by default). */
    readonly tone?: ToastTone;
}

/**
 * A short notification.
 */
interface ToastMessage {
    /** The identifier. */
    readonly id: number;
    /** The text. */
    readonly text: string;
    /** What it reports. */
    readonly tone: ToastTone;
    /** How long it stays, in milliseconds. */
    readonly duration: number;
    /** Whether it is fading out. */
    readonly leaving: boolean;
}

/** How long a notification stays, in milliseconds; failures stay longer so they can be read. */
const durations: Record<ToastTone, number> = { success: 4500, info: 5000, warning: 6500, error: 7500 };

/** The icons of the tones. */
const icons: Record<ToastTone, LucideIcon> = { success: CircleCheck, error: CircleAlert, warning: TriangleAlert, info: Info };

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
 * Fades a notification out and removes it.
 * @param id The notification.
 */
function dismiss(id: number): void {
    if (!toasts.some((item) => item.id === id && !item.leaving)) {
        return;
    }

    update(toasts.map((item) => (item.id === id ? { ...item, leaving: true } : item)));
    setTimeout(() => update(toasts.filter((item) => item.id !== id)), leavingFor);
}

/**
 * Shows a notification at the bottom of the screen for a few seconds. Every outcome of an action (saved, deleted,
 * failed) is reported this way, not by a message on the page.
 * @param text The text (already translated).
 * @param options What it reports.
 */
export function toast(text: string, options: ToastOptions = {}): void {
    const id = nextId++;
    const tone = options.tone ?? "success";
    const duration = durations[tone];
    update([...toasts, { id, text, tone, duration, leaving: false }]);
    setTimeout(() => dismiss(id), duration);
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
 * The place where notifications appear; rendered once by the application root. Failures and warnings are announced at
 * once (role="alert"), the rest politely. The region is a popover in the top layer, so notifications also show above
 * modal dialogs (an error of an action in a dialog); a new one raises it above a dialog opened since.
 * @returns The region.
 */
export function ToastRegion() {
    const { t } = useLingui();
    const items = useSyncExternalStore(subscribe, () => toasts);
    const region = useRef<HTMLDivElement>(null);
    const newest = items.at(-1)?.id ?? 0;

    useEffect(() => {
        const element = region.current;
        if (!element) {
            return;
        }

        const open = element.matches(":popover-open");
        if (newest === 0) {
            if (open) {
                element.hidePopover();
            }

            return;
        }

        // Showing again puts the region on top of the top layer; only needed when a modal dialog may cover it.
        if (open && document.querySelector("dialog:modal")) {
            element.hidePopover();
        }

        if (!element.matches(":popover-open")) {
            element.showPopover();
        }
    }, [newest]);

    return (
        <div ref={region} className={styles.region} popover="manual" aria-live="polite">
            {items.map((item) => (
                <div
                    key={item.id}
                    className={styles.toast}
                    data-tone={item.tone}
                    data-leaving={item.leaving || undefined}
                    role={item.tone === "error" || item.tone === "warning" ? "alert" : "status"}
                    style={{ "--_duration": `${item.duration}ms` } as CSSProperties}
                >
                    <span className={styles.icon}>
                        <Icon icon={icons[item.tone]} />
                    </span>
                    <span className={styles.text}>{item.text}</span>
                    <button type="button" className={styles.close} aria-label={t`Закрити`} onClick={() => dismiss(item.id)}>
                        <Icon icon={X} size="sm" />
                    </button>
                </div>
            ))}
        </div>
    );
}
