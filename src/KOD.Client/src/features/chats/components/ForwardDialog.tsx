import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router";
import { describeError } from "@/shared/api/problem";
import { useCurrentUser } from "@/shared/session/session";
import { Avatar } from "@/shared/ui/Avatar";
import { Dialog } from "@/shared/ui/Dialog";
import { SearchInput } from "@/shared/ui/SearchInput";
import { SkeletonList } from "@/shared/ui/Skeleton";
import { toast } from "@/shared/ui/Toast";
import { chatApi, chatKeys, chatsQuery, peopleQuery } from "@/features/chats/api";
import { chatTitle } from "@/features/chats/display";
import { ChatAvatar } from "./ChatAvatar";
import styles from "./chats.module.css";

/**
 * Props of {@link ForwardDialog}.
 */
export interface ForwardDialogProps {
    /** Whether the dialog is open. */
    readonly open: boolean;
    /** Closes the dialog. */
    readonly onClose: () => void;
    /** The chat of the messages. */
    readonly chatId: string;
    /** The messages to forward. */
    readonly messageIds: readonly string[];
}

/**
 * Chooses where to forward messages: one of the user's chats (the search filters them by name) or, for a search of two
 * characters or more, a colleague without a one-to-one chat yet (the chat is opened). The forwarded messages appear in
 * that chat with the names of their original authors, and the user goes there.
 * @param props The props.
 * @returns The dialog.
 */
export function ForwardDialog({ open, onClose, chatId, messageIds }: ForwardDialogProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const me = useCurrentUser().id;
    const [filter, setFilter] = useState("");
    const [busy, setBusy] = useState(false);
    const chats = useQuery(chatsQuery);
    const people = useQuery(peopleQuery(filter));

    const needle = filter.trim().toLocaleLowerCase();
    const listed = (chats.data ?? []).map((chat) => ({ chat, title: chatTitle(chat, me) }));
    const shownChats = needle ? listed.filter(({ title }) => title.toLocaleLowerCase().includes(needle)) : listed;

    // People already in a one-to-one chat are found among the chats.
    const direct = new Set(listed.filter(({ chat }) => chat.kind === "Direct").flatMap(({ chat }) => chat.members.map((m) => m.id)));
    const shownPeople = needle.length >= 2 ? (people.data ?? []).filter((person) => !direct.has(person.id)) : [];

    /**
     * Forwards the messages to a chat and opens it.
     * @param target The chat, or a person whose one-to-one chat is opened first.
     */
    async function forward(target: { readonly chatId: string } | { readonly userId: string }): Promise<void> {
        setBusy(true);
        try {
            const to = "chatId" in target ? target.chatId : (await chatApi.openDirect(target.userId)).id;
            await chatApi.forward(to, chatId, messageIds);
            await queryClient.invalidateQueries({ queryKey: chatKeys.list });
            onClose();
            toast(messageIds.length === 1 ? t`Повідомлення переслано.` : t`Повідомлення переслано (${messageIds.length}).`);
            if (to !== chatId) {
                void navigate(`/chats/${to}`);
            }
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося переслати повідомлення.`), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Dialog open={open} onClose={onClose} title={t`Переслати в чат`}>
            <div className={styles.dialogBody}>
                <SearchInput value={filter} onSearch={setFilter} placeholder={t`Чат або людина`} autoFocus />

                {chats.isPending ? (
                    <SkeletonList rows={5} />
                ) : (
                    <ul className={styles.people} aria-label={t`Куди переслати`} aria-busy={busy}>
                        {shownChats.map(({ chat, title }) => (
                            <li key={chat.id}>
                                <button
                                    type="button"
                                    className={styles.person}
                                    disabled={busy}
                                    onClick={() => void forward({ chatId: chat.id })}
                                >
                                    <ChatAvatar kind={chat.kind} others={chat.members} />
                                    <span className={styles.personText}>
                                        <span className={styles.personName}>{title}</span>
                                        <span className={styles.personMeta}>
                                            {chat.kind === "Group" ? t`Учасників: ${chat.memberCount}` : t`Особистий чат`}
                                        </span>
                                    </span>
                                </button>
                            </li>
                        ))}
                        {shownPeople.map((person) => (
                            <li key={person.id}>
                                <button
                                    type="button"
                                    className={styles.person}
                                    disabled={busy}
                                    onClick={() => void forward({ userId: person.id })}
                                >
                                    <Avatar name={person.fullName} photoId={person.photoId} size="sm" decorative />
                                    <span className={styles.personText}>
                                        <span className={styles.personName}>{person.fullName}</span>
                                        <span className={styles.personMeta}>{person.position ?? person.email}</span>
                                    </span>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}

                {!chats.isPending && shownChats.length === 0 && shownPeople.length === 0 && (
                    <p className={styles.pickerHint}>{needle.length >= 2 && people.isFetching ? t`Пошук…` : t`Нічого не знайдено.`}</p>
                )}
            </div>
        </Dialog>
    );
}
