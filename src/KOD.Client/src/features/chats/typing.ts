import { useSyncExternalStore } from "react";
import type { UserRef } from "@/shared/api/schema";

/**
 * How long a "typing" signal is shown (the server lets a page send one every two seconds while the person types).
 */
const showFor = 4500;

/**
 * Who is typing in which chat, until when.
 */
const typing = new Map<string, Map<string, { readonly user: UserRef; readonly until: number }>>();

/**
 * The components that show typing people.
 */
const listeners = new Set<() => void>();

/**
 * The snapshots per chat (stable while nothing changes, as useSyncExternalStore needs).
 */
const snapshots = new Map<string, readonly UserRef[]>();

/**
 * The timer that hides expired signals.
 */
let sweepTimer: ReturnType<typeof setTimeout> | undefined;

/**
 * Rebuilds the snapshots and tells the components.
 */
function publish(): void {
    const now = Date.now();
    snapshots.clear();
    for (const [chatId, people] of typing) {
        for (const [userId, entry] of people) {
            if (entry.until <= now) {
                people.delete(userId);
            }
        }

        if (people.size === 0) {
            typing.delete(chatId);
        } else {
            snapshots.set(
                chatId,
                [...people.values()].map((entry) => entry.user),
            );
        }
    }

    listeners.forEach((listener) => listener());
    clearTimeout(sweepTimer);
    if (typing.size > 0) {
        sweepTimer = setTimeout(publish, 1000);
    }
}

/**
 * Notes that a person is typing in a chat.
 * @param chatId The chat.
 * @param user The person.
 */
export function noteTyping(chatId: string, user: UserRef): void {
    const people = typing.get(chatId) ?? new Map<string, { readonly user: UserRef; readonly until: number }>();
    people.set(user.id, { user, until: Date.now() + showFor });
    typing.set(chatId, people);
    publish();
}

/**
 * Notes that a person stopped typing (their message arrived).
 * @param chatId The chat.
 * @param userId The person.
 */
export function clearTyping(chatId: string, userId: string): void {
    if (typing.get(chatId)?.delete(userId)) {
        publish();
    }
}

/**
 * The empty list (a stable snapshot).
 */
const nobody: readonly UserRef[] = [];

/**
 * The people typing in a chat now.
 * @param chatId The chat.
 * @returns The people.
 */
export function useTyping(chatId: string): readonly UserRef[] {
    return useSyncExternalStore(
        (listener) => {
            listeners.add(listener);
            return () => listeners.delete(listener);
        },
        () => snapshots.get(chatId) ?? nobody,
    );
}
