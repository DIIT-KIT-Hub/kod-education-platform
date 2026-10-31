import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useLingui } from "@lingui/react/macro";
import { useMatch, useNavigate } from "react-router";
import type { ChatMessageDto, UserRef } from "@/shared/api/schema";
import { useLiveEvent } from "@/shared/realtime/live";
import { useCurrentUser } from "@/shared/session/session";
import { toast } from "@/shared/ui/Toast";
import { applyRead, catchUp, chatKeys, chatsQuery, receiveMessage, touchList, type ChatThread } from "@/features/chats/api";
import { clearTyping, noteTyping } from "@/features/chats/typing";

/**
 * Keeps the chats current while the user is signed in, on every page: new messages, edits and reactions are put into the
 * loaded conversations and the chat list (with the unread counts of the menu), typing signals are noted, and after a
 * reconnect everything catches up. Renders nothing.
 * @returns Nothing.
 */
export function ChatSync() {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const me = useCurrentUser().id;
    const open = useMatch("/chats/:chatId")?.params.chatId;

    /**
     * Applies a new or changed message.
     * @param message The message.
     */
    function onMessage(message: ChatMessageDto): void {
        receiveMessage(queryClient, message);
        clearTyping(message.chatId, message.author.id);
        if (!touchList(queryClient, message, me)) {
            void queryClient.invalidateQueries({ queryKey: chatKeys.list });
        }

        // Someone joined or left: the member list of the chat changed.
        if (message.kind === "System") {
            void queryClient.invalidateQueries({ queryKey: chatKeys.details(message.chatId) });
        }
    }

    useLiveEvent("ChatMessageAdded", onMessage, () => {
        void queryClient.invalidateQueries({ queryKey: chatKeys.list });
        queryClient
            .getQueriesData<ChatThread>({ queryKey: ["chats", "thread"] })
            .forEach(([key]) => void catchUp(queryClient, String(key[2])).catch(() => undefined));
    });
    useLiveEvent("ChatMessageChanged", (message: ChatMessageDto) => {
        receiveMessage(queryClient, message);
        void queryClient.invalidateQueries({ queryKey: chatKeys.list });
    });
    useLiveEvent("ChatChanged", (chatId: string) => {
        void queryClient.invalidateQueries({ queryKey: chatKeys.list });
        void queryClient.invalidateQueries({ queryKey: chatKeys.details(chatId) });
    });
    useLiveEvent("ChatRemoved", (chatId: string) => {
        queryClient.removeQueries({ queryKey: chatKeys.thread(chatId) });
        queryClient.removeQueries({ queryKey: chatKeys.details(chatId) });
        void queryClient.invalidateQueries({ queryKey: chatKeys.list });
        if (open === chatId) {
            toast(t`Ви більше не в цьому чаті.`);
            void navigate("/chats", { replace: true });
        }
    });
    useLiveEvent("ChatRead", (chatId: string, userId: string, sequence: number) => applyRead(queryClient, chatId, userId, sequence, me));
    useLiveEvent("ChatTyping", (chatId: string, user: UserRef) => noteTyping(chatId, user));

    return null;
}

/**
 * The number of unread messages in all chats (the badge of the menu).
 * @param enabled Whether the user may use chats.
 * @returns The number.
 */
export function useUnreadChats(enabled: boolean): number {
    const { data } = useQuery({ ...chatsQuery, enabled });
    return data?.reduce((sum, chat) => sum + chat.unreadCount, 0) ?? 0;
}
