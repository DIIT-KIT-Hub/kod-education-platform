import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, MessageSquareOff, Users } from "lucide-react";
import { useRef, useState } from "react";
import { ApiError } from "@/shared/api/problem";
import { useCurrentUser } from "@/shared/session/session";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Skeleton, SkeletonMessages } from "@/shared/ui/Skeleton";
import { applyRead, chatApi, chatQuery, threadQuery } from "@/features/chats/api";
import { chatTitle, shortName } from "@/features/chats/display";
import { useTyping } from "@/features/chats/typing";
import { ChatAvatar } from "./ChatAvatar";
import styles from "./chats.module.css";
import { Composer } from "./Composer";
import { MembersDialog } from "./MembersDialog";
import { MessageList } from "./MessageList";

/**
 * Props of {@link Conversation}.
 */
export interface ConversationProps {
    /** The chat. */
    readonly chatId: string;
}

/**
 * An open chat: the header with the people, the messages, who is typing and the editor. Reading the end of the
 * conversation marks it as read (the other pages of the user and the other person's "seen" follow live).
 * @param props The props.
 * @returns The conversation.
 */
export function Conversation({ chatId }: ConversationProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const me = useCurrentUser().id;
    const details = useQuery(chatQuery(chatId));
    const thread = useQuery(threadQuery(chatId));
    const typing = useTyping(chatId).filter((user) => user.id !== me);
    const [membersOpen, setMembersOpen] = useState(false);
    const reported = useRef(0);

    if (details.error instanceof ApiError && details.error.status === 404) {
        return (
            <EmptyState icon={MessageSquareOff} title={t`Чат не знайдено`} text={t`Можливо, вас вилучили з чату або його видалено.`}>
                <ButtonLink to="/chats" variant="secondary">
                    {t`До чатів`}
                </ButtonLink>
            </EmptyState>
        );
    }

    if (!details.data || !thread.data) {
        return (
            <div className={styles.conversation}>
                <header className={styles.conversationHeader} aria-hidden="true">
                    <Skeleton shape="circle" height="2.5rem" />
                    <div className={styles.conversationTitle}>
                        <Skeleton width="12rem" height="1rem" />
                        <Skeleton width="8rem" height="0.75rem" />
                    </div>
                </header>
                <SkeletonMessages count={8} />
            </div>
        );
    }

    const chat = details.data;
    const others = chat.members.filter((m) => m.user.id !== me);
    const title = chatTitle({ ...chat, members: chat.members.map((m) => m.user), memberCount: chat.members.length }, me);
    const subtitle = chat.kind === "Direct" ? (others[0]?.position ?? "") : t`Учасників: ${chat.members.length}`;
    const myRead = chat.members.find((m) => m.user.id === me)?.lastReadSequence ?? 0;
    const peerRead = chat.kind === "Direct" ? (others[0]?.lastReadSequence ?? 0) : 0;

    /**
     * Marks the conversation as read up to the newest message the user saw.
     * @param sequence The number of the message.
     */
    function seen(sequence: number): void {
        if (sequence <= Math.max(myRead, reported.current)) {
            return;
        }

        reported.current = sequence;
        applyRead(queryClient, chatId, me, sequence, me);
        void chatApi.markRead(chatId, sequence).catch(() => {
            reported.current = 0;
        });
    }

    const first = typing[0];
    const typingText = !first
        ? ""
        : typing.length === 1
          ? t`${shortName(first.fullName)} пише…`
          : t`${typing.map((user) => shortName(user.fullName).split(" ")[1] ?? user.fullName).join(", ")} пишуть…`;

    return (
        <div className={styles.conversation}>
            <DocumentTitle>{title}</DocumentTitle>
            <header className={styles.conversationHeader}>
                <ButtonLink to="/chats" variant="ghost" size="sm" iconOnly icon={ArrowLeft} className={styles.back}>
                    {t`До чатів`}
                </ButtonLink>
                <ChatAvatar kind={chat.kind} others={others.map((m) => m.user)} />
                <div className={styles.conversationTitle}>
                    <h1>{title}</h1>
                    {subtitle && <p>{subtitle}</p>}
                </div>
                <Button variant="ghost" size="sm" icon={Users} onClick={() => setMembersOpen(true)}>
                    {chat.kind === "Group" ? t`Учасники` : t`Про чат`}
                </Button>
            </header>

            <MessageList
                chatId={chatId}
                kind={chat.kind}
                thread={thread.data}
                me={me}
                peerRead={peerRead}
                onSeen={seen}
                peerName={others[0]?.user.fullName}
            />

            <p className={styles.typing} aria-live="polite">
                {typingText}
            </p>
            <Composer chatId={chatId} />

            {membersOpen && <MembersDialog open onClose={() => setMembersOpen(false)} chat={chat} me={me} />}
        </div>
    );
}
