import type { QueryClient } from "@tanstack/react-query";
import type { UserRef } from "@/shared/api/schema";
import { chatApi, chatKeys, mergeMessages, receiveMessage, touchList, type ChatMessage, type ChatThread } from "./api";

export { fileAccept, isAllowedFile, maxFiles, maxFileSize, maxTotalSize } from "@/shared/lib/attachments";

/**
 * A message to send.
 */
export interface Draft {
    /** The identifier of the message in this page; a resend with it lands once. */
    readonly clientId: string;
    /** The HTML of the editor ("" when only files are sent). */
    readonly body: string;
    /** The visible text (shown while sending). */
    readonly text: string;
    /** The attachments. */
    readonly files: readonly File[];
}

/**
 * Shows a message in the conversation at once and sends it; when sending fails, the message stays marked with its
 * files, so it can be sent again with the same client identifier (the server never saves it twice).
 * @param queryClient The query client.
 * @param chatId The chat.
 * @param author The current user.
 * @param draft The message.
 * @returns Resolves with whether the message was sent.
 */
export async function sendDraft(queryClient: QueryClient, chatId: string, author: UserRef, draft: Draft): Promise<boolean> {
    const pending = (failed: boolean): ChatMessage => ({
        id: `local:${draft.clientId}`,
        chatId,
        sequence: 0,
        author,
        kind: "Text",
        event: null,
        body: draft.body,
        text: draft.text,
        createdAt: new Date().toISOString(),
        editedAt: null,
        isDeleted: false,
        clientId: draft.clientId,
        attachments: [],
        reactions: [],
        pending: { failed, files: draft.files },
    });
    const put = (message: ChatMessage) =>
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), (thread) => (thread ? mergeMessages(thread, [message]) : thread));

    put(pending(false));
    try {
        const sent = await chatApi.send(chatId, draft.clientId, draft.body, draft.files);
        receiveMessage(queryClient, sent);
        if (!touchList(queryClient, sent, author.id)) {
            void queryClient.invalidateQueries({ queryKey: chatKeys.list });
        }

        return true;
    } catch (error) {
        put(pending(true));
        throw error;
    }
}

/**
 * Removes a message that could not be sent.
 * @param queryClient The query client.
 * @param chatId The chat.
 * @param clientId The identifier of the message in this page.
 */
export function discardDraft(queryClient: QueryClient, chatId: string, clientId: string): void {
    queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), (thread) =>
        thread ? { ...thread, messages: thread.messages.filter((m) => !(m.pending && m.clientId === clientId)) } : thread,
    );
}
