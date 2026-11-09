/*
 * A stand-in for `@/shared/realtime/live` in tests: components subscribe as with the real module, and the test pushes
 * hub messages and reconnects by hand. Use it with `vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"))`.
 */
import { act } from "@testing-library/react";
import { useEffect } from "react";
import { vi } from "vitest";

/**
 * A component that listens to a hub message.
 */
interface Subscription {
    /** Called with the message arguments. */
    readonly handler: (...args: never[]) => void;
    /** Called after a reconnect. */
    readonly onReconnected?: () => void;
}

/**
 * The listeners per hub message.
 */
const subscriptions = new Map<string, Set<Subscription>>();

/**
 * Listens to a hub message while the component is shown (same contract as the real hook).
 * @param message The hub message.
 * @param handler Called with the message arguments.
 * @param onReconnected Called after the connection came back.
 */
export function useLiveEvent(message: string, handler: (...args: never[]) => void, onReconnected?: () => void): void {
    useEffect(() => {
        const entry: Subscription = { handler, onReconnected };
        const set = subscriptions.get(message) ?? new Set<Subscription>();
        set.add(entry);
        subscriptions.set(message, set);
        return () => {
            set.delete(entry);
        };
    }, [message, handler, onReconnected]);
}

/**
 * Listens to a hub message of a group (the handler is also called without arguments after a reconnect).
 * @param _group The group key.
 * @param _join Joins the group.
 * @param _leave Leaves the group.
 * @param message The hub message.
 * @param handler The handler.
 */
export function useLiveGroup(
    _group: string,
    _join: unknown,
    _leave: unknown,
    message: string,
    handler: (...args: unknown[]) => void,
): void {
    useLiveEvent(message, handler, handler);
}

/**
 * The recorded hub calls ("Typing").
 */
export const sendLive = vi.fn();

/**
 * The recorded sign-out disconnects.
 */
export const disconnectLive = vi.fn(async () => undefined);

/**
 * Pushes a hub message to every listening component.
 * @param message The hub message.
 * @param args The arguments.
 */
export function emitLive(message: string, ...args: unknown[]): void {
    act(() => {
        subscriptions.get(message)?.forEach((entry) => (entry.handler as (...values: unknown[]) => void)(...args));
    });
}

/**
 * Simulates the connection coming back: every listener catches up.
 */
export function reconnectLive(): void {
    act(() => {
        subscriptions.forEach((set) => set.forEach((entry) => entry.onReconnected?.()));
    });
}

/**
 * The number of components listening to a hub message.
 * @param message The hub message.
 * @returns The number.
 */
export function listenerCount(message: string): number {
    return subscriptions.get(message)?.size ?? 0;
}
