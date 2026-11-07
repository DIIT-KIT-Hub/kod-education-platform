import { queryOptions, type QueryClient } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { ChatDetails, ChatMessageDto, ChatSummary, ReactionKind } from "@/shared/api/schema";

/**
 * A message as the conversation shows it: from the server, or still being sent from this page.
 */
export interface ChatMessage extends ChatMessageDto {
    /** Set while the message is on its way, or when sending failed (the files stay for a retry). */
    readonly pending?: { readonly failed: boolean; readonly files: readonly File[] };
}

/**
 * The loaded part of a conversation: consecutive messages up to the newest one, oldest first.
 */
export interface ChatThread {
    /** The messages (pending ones at the end). */
    readonly messages: readonly ChatMessage[];
    /** Whether older messages exist on the server. */
    readonly hasOlder: boolean;
}

/**
 * The number of messages loaded at a time.
 */
const pageSize = 50;

/**
 * The keys of the chat queries.
 */
export const chatKeys = {
    all: ["chats"] as const,
    list: ["chats", "list"] as const,
    details: (chatId: string) => ["chats", "details", chatId] as const,
    thread: (chatId: string) => ["chats", "thread", chatId] as const,
    people: (term: string) => ["chats", "people", term] as const,
};

/**
 * The chats of the user, the most recently active first.
 */
export const chatsQuery = queryOptions({
    queryKey: chatKeys.list,
    queryFn: () => unwrap(api.GET("/api/v1/chats")),
    staleTime: 60_000,
});

/**
 * A chat with its members.
 * @param chatId The chat.
 * @returns The query.
 */
export function chatQuery(chatId: string) {
    return queryOptions({
        queryKey: chatKeys.details(chatId),
        queryFn: () => unwrap(api.GET("/api/v1/chats/{id}", { params: { path: { id: chatId } } })),
        staleTime: 60_000,
    });
}

/**
 * The newest messages of a chat; older ones are added by {@link loadOlder}, new ones arrive live.
 * @param chatId The chat.
 * @returns The query.
 */
export function threadQuery(chatId: string) {
    return queryOptions({
        queryKey: chatKeys.thread(chatId),
        queryFn: async (): Promise<ChatThread> => {
            const page = await unwrap(
                api.GET("/api/v1/chats/{id}/messages", { params: { path: { id: chatId }, query: { limit: pageSize } } }),
            );
            return { messages: page.messages, hasOlder: page.hasMore };
        },
        // Live messages keep it current; a reconnect catches up.
        staleTime: Infinity,
        gcTime: 10 * 60_000,
    });
}

/**
 * People to chat with.
 * @param term The search text.
 * @returns The query.
 */
export function peopleQuery(term: string) {
    return queryOptions({
        queryKey: chatKeys.people(term),
        queryFn: () => unwrap(api.GET("/api/v1/chats/people", { params: { query: { term } } })),
        enabled: term.trim().length >= 2,
        staleTime: 30_000,
    });
}

/**
 * Orders messages: by number, the ones being sent last.
 * @param a A message.
 * @param b Another message.
 * @returns The order.
 */
function bySequence(a: ChatMessage, b: ChatMessage): number {
    if (a.pending || b.pending) {
        return (a.pending ? 1 : 0) - (b.pending ? 1 : 0) || a.createdAt.localeCompare(b.createdAt);
    }

    return a.sequence - b.sequence;
}

/**
 * Puts messages into a thread: replaces the same message (by id, or a pending one by its client id) and keeps the order.
 * @param thread The thread.
 * @param incoming The messages.
 * @returns The new thread.
 */
export function mergeMessages(thread: ChatThread, incoming: readonly ChatMessage[]): ChatThread {
    const messages = [...thread.messages];
    for (const message of incoming) {
        const index = messages.findIndex(
            (m) => m.id === message.id || (message.clientId !== null && m.clientId === message.clientId && (m.pending || message.pending)),
        );
        if (index >= 0) {
            // A late echo of a message must not bring back its pending state, and a message the user hid stays hidden
            // when the others change it.
            const current = messages[index];
            if (!(message.pending && !current?.pending) && !(current?.isHidden && !message.isHidden)) {
                messages[index] = message;
            }
        } else {
            messages.push(message);
        }
    }

    messages.sort(bySequence);
    return { ...thread, messages };
}

/**
 * Finds the number after which messages are missing (a live message arrived after a gap).
 * @param thread The thread.
 * @returns The number of the last message before the gap, or null.
 */
function gapAfter(thread: ChatThread): number | null {
    const numbers = thread.messages.filter((m) => !m.pending).map((m) => m.sequence);
    const gap = numbers.findIndex((sequence, index) => index > 0 && sequence > (numbers[index - 1] ?? sequence) + 1);
    return gap > 0 ? (numbers[gap - 1] ?? null) : null;
}

/**
 * Loads the messages after a number until the newest one and merges them.
 * @param queryClient The query client.
 * @param chatId The chat.
 * @param after The number of the last message known in order.
 */
async function fetchAfter(queryClient: QueryClient, chatId: string, after: number): Promise<void> {
    let from = after;
    for (let round = 0; round < 20; round++) {
        const page = await unwrap(
            api.GET("/api/v1/chats/{id}/messages", { params: { path: { id: chatId }, query: { after: from, limit: 100 } } }),
        );
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), (thread) => (thread ? mergeMessages(thread, page.messages) : thread));
        const last = page.messages.at(-1);
        if (!page.hasMore || !last) {
            return;
        }

        from = last.sequence;
    }
}

/**
 * Catches a loaded conversation up with the server (after a reconnect, or when a live message shows a gap).
 * @param queryClient The query client.
 * @param chatId The chat.
 */
export async function catchUp(queryClient: QueryClient, chatId: string): Promise<void> {
    const thread = queryClient.getQueryData<ChatThread>(chatKeys.thread(chatId));
    if (!thread) {
        return;
    }

    const newest = thread.messages.filter((m) => !m.pending).at(-1);
    if (!newest) {
        await queryClient.invalidateQueries({ queryKey: chatKeys.thread(chatId) });
        return;
    }

    await fetchAfter(queryClient, chatId, gapAfter(thread) ?? newest.sequence);
}

/**
 * Applies a message pushed by the server to the loaded conversation, catching up when messages are missing before it.
 * @param queryClient The query client.
 * @param message The message.
 */
export function receiveMessage(queryClient: QueryClient, message: ChatMessageDto): void {
    const key = chatKeys.thread(message.chatId);
    const thread = queryClient.getQueryData<ChatThread>(key);
    if (!thread) {
        return;
    }

    const merged = mergeMessages(thread, [message]);
    queryClient.setQueryData(key, merged);
    if (gapAfter(merged) !== null) {
        void catchUp(queryClient, message.chatId).catch(() => undefined);
    }
}

/**
 * Loads the page of messages before the oldest loaded one.
 * @param queryClient The query client.
 * @param chatId The chat.
 */
export async function loadOlder(queryClient: QueryClient, chatId: string): Promise<void> {
    const thread = queryClient.getQueryData<ChatThread>(chatKeys.thread(chatId));
    const oldest = thread?.messages.find((m) => !m.pending);
    if (!thread?.hasOlder || !oldest) {
        return;
    }

    const page = await unwrap(
        api.GET("/api/v1/chats/{id}/messages", { params: { path: { id: chatId }, query: { before: oldest.sequence, limit: pageSize } } }),
    );
    queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), (current) =>
        current ? { ...mergeMessages(current, page.messages), hasOlder: page.hasMore } : current,
    );
}

/**
 * Updates the chat list after a new message: the chat moves to the top with its preview and unread count.
 * @param queryClient The query client.
 * @param message The message.
 * @param me The current user.
 * @returns Whether the chat was in the list (otherwise the list must be loaded again).
 */
export function touchList(queryClient: QueryClient, message: ChatMessageDto, me: string): boolean {
    const list = queryClient.getQueryData<ChatSummary[]>(chatKeys.list);
    const chat = list?.find((c) => c.id === message.chatId);
    if (!list || !chat || message.sequence <= chat.lastSequence) {
        return Boolean(chat);
    }

    const mine = message.author.id === me;
    const updated: ChatSummary = {
        ...chat,
        lastSequence: message.sequence,
        lastActivityAt: message.createdAt,
        lastReadSequence: mine ? message.sequence : chat.lastReadSequence,
        unreadCount: mine ? 0 : chat.unreadCount + (message.kind === "Text" ? 1 : 0),
        lastMessage: {
            author: message.author,
            kind: message.kind,
            event: message.event,
            text: message.text,
            isDeleted: message.isDeleted,
            attachmentCount: message.attachments.length,
            createdAt: message.createdAt,
        },
    };
    queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [updated, ...list.filter((c) => c.id !== chat.id)]);
    return true;
}

/**
 * Notes in the cached chat and list that a member read up to a message.
 * @param queryClient The query client.
 * @param chatId The chat.
 * @param userId The member.
 * @param sequence The number of the last read message.
 * @param me The current user.
 */
export function applyRead(queryClient: QueryClient, chatId: string, userId: string, sequence: number, me: string): void {
    queryClient.setQueryData<ChatDetails>(chatKeys.details(chatId), (details) =>
        details
            ? {
                  ...details,
                  members: details.members.map((m) =>
                      m.user.id === userId && m.lastReadSequence < sequence ? { ...m, lastReadSequence: sequence } : m,
                  ),
              }
            : details,
    );
    if (userId !== me) {
        return;
    }

    const list = queryClient.getQueryData<ChatSummary[]>(chatKeys.list);
    const chat = list?.find((c) => c.id === chatId);
    if (!list || !chat || chat.lastReadSequence >= sequence) {
        return;
    }

    if (sequence >= chat.lastSequence) {
        queryClient.setQueryData<ChatSummary[]>(
            chatKeys.list,
            list.map((c) => (c.id === chatId ? { ...c, lastReadSequence: sequence, unreadCount: 0 } : c)),
        );
    } else {
        void queryClient.invalidateQueries({ queryKey: chatKeys.list });
    }
}

/**
 * The chat requests that change something.
 */
export const chatApi = {
    /**
     * Opens the one-to-one chat with a person.
     * @param userId The person.
     * @returns The chat.
     */
    openDirect: (userId: string) => unwrap(api.POST("/api/v1/chats/direct", { body: { userId } })),

    /**
     * Creates a group chat.
     * @param title The title, or empty.
     * @param memberIds The other members.
     * @returns The chat.
     */
    createGroup: (title: string, memberIds: readonly string[]) =>
        unwrap(api.POST("/api/v1/chats", { body: { title: title.trim() || null, memberIds: [...memberIds] } })),

    /**
     * Renames a group chat.
     * @param chatId The chat.
     * @param title The title, or empty.
     * @returns Resolves when saved.
     */
    rename: (chatId: string, title: string) =>
        unwrap(api.PUT("/api/v1/chats/{id}/title", { params: { path: { id: chatId } }, body: { title: title.trim() || null } })),

    /**
     * Adds people to a group chat.
     * @param chatId The chat.
     * @param userIds The people.
     * @returns Resolves when added.
     */
    addMembers: (chatId: string, userIds: readonly string[]) =>
        unwrap(api.POST("/api/v1/chats/{id}/members", { params: { path: { id: chatId } }, body: { userIds: [...userIds] } })),

    /**
     * Leaves a chat or removes a person from it.
     * @param chatId The chat.
     * @param userId The person.
     * @returns Resolves when done.
     */
    removeMember: (chatId: string, userId: string) =>
        unwrap(api.DELETE("/api/v1/chats/{id}/members/{userId}", { params: { path: { id: chatId, userId } } })),

    /**
     * Clears the history for the current user only; a one-to-one chat leaves their list until a new message.
     * @param chatId The chat.
     * @returns Resolves when cleared.
     */
    clearHistory: (chatId: string) => unwrap(api.POST("/api/v1/chats/{id}/clear", { params: { path: { id: chatId } } })),

    /**
     * Deletes the chat for all its members with its messages and files.
     * @param chatId The chat.
     * @returns Resolves when deleted.
     */
    deleteChat: (chatId: string) => unwrap(api.DELETE("/api/v1/chats/{id}", { params: { path: { id: chatId } } })),

    /**
     * Sends a message.
     * @param chatId The chat.
     * @param clientId The identifier of the message in this page (a resend lands once).
     * @param body The HTML of the editor.
     * @param files The attachments.
     * @returns The saved message.
     */
    send: (chatId: string, clientId: string, body: string, files: readonly File[]) => {
        const form = new FormData();
        form.append("ClientId", clientId);
        form.append("Body", body);
        files.forEach((file) => form.append("Files", file, file.name));
        return unwrap(
            api.POST("/api/v1/chats/{id}/messages", { params: { path: { id: chatId } }, body: {} as never, bodySerializer: () => form }),
        );
    },

    /**
     * Replaces the text of a message.
     * @param chatId The chat.
     * @param messageId The message.
     * @param body The HTML of the editor.
     * @returns The edited message.
     */
    edit: (chatId: string, messageId: string, body: string) =>
        unwrap(api.PUT("/api/v1/chats/{id}/messages/{messageId}", { params: { path: { id: chatId, messageId } }, body: { body } })),

    /**
     * Forwards messages to a chat; they appear there as the user's messages naming their original authors.
     * @param chatId The chat the messages go to.
     * @param sourceChatId The chat of the messages.
     * @param messageIds The messages.
     * @returns The new messages.
     */
    forward: (chatId: string, sourceChatId: string, messageIds: readonly string[]) =>
        unwrap(
            api.POST("/api/v1/chats/{id}/forward", {
                params: { path: { id: chatId } },
                body: { sourceChatId, messageIds: [...messageIds] },
            }),
        ),

    /**
     * Deletes the user's own message for everyone.
     * @param chatId The chat.
     * @param messageId The message.
     * @returns Resolves when deleted.
     */
    remove: (chatId: string, messageId: string) =>
        unwrap(api.DELETE("/api/v1/chats/{id}/messages/{messageId}", { params: { path: { id: chatId, messageId } } })),

    /**
     * Deletes a message for the current user only.
     * @param chatId The chat.
     * @param messageId The message.
     * @returns The empty placeholder the user sees now.
     */
    hide: (chatId: string, messageId: string) =>
        unwrap(api.POST("/api/v1/chats/{id}/messages/{messageId}/hide", { params: { path: { id: chatId, messageId } } })),

    /**
     * Sets or removes a reaction.
     * @param chatId The chat.
     * @param messageId The message.
     * @param kind The emotion.
     * @returns The message with its reactions.
     */
    react: (chatId: string, messageId: string, kind: ReactionKind) =>
        unwrap(
            api.POST("/api/v1/chats/{id}/messages/{messageId}/reactions/{kind}", {
                params: { path: { id: chatId, messageId, kind } },
            }),
        ),

    /**
     * Marks the chat as read up to a message.
     * @param chatId The chat.
     * @param sequence The number of the last read message.
     * @returns Resolves when saved.
     */
    markRead: (chatId: string, sequence: number) =>
        unwrap(api.PUT("/api/v1/chats/{id}/read", { params: { path: { id: chatId } }, body: { sequence } })),
};
