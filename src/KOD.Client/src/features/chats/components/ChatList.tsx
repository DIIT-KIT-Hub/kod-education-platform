import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { MessagesSquare, SquarePen } from "lucide-react";
import { useState } from "react";
import { NavLink } from "react-router";
import { formatShortMoment } from "@/shared/i18n/format";
import { useCurrentUser } from "@/shared/session/session";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { EmptyState } from "@/shared/ui/EmptyState";
import { SearchInput } from "@/shared/ui/SearchInput";
import { chatsQuery } from "@/features/chats/api";
import { chatTitle, previewText } from "@/features/chats/display";
import { SkeletonList } from "@/shared/ui/Skeleton";
import { ChatAvatar } from "./ChatAvatar";
import styles from "./chats.module.css";

/**
 * Props of {@link ChatList}.
 */
export interface ChatListProps {
    /** Opens the dialog of a new chat. */
    readonly onNew: () => void;
}

/**
 * The chats of the user, the most recently active first, with the newest message and the number of unread ones; the
 * search filters them by name.
 * @param props The props.
 * @returns The list.
 */
export function ChatList({ onNew }: ChatListProps) {
    const { t } = useLingui();
    const me = useCurrentUser().id;
    const { data, error, isPending } = useQuery(chatsQuery);
    const [filter, setFilter] = useState("");

    const chats = (data ?? []).map((chat) => ({ chat, title: chatTitle(chat, me) }));
    const needle = filter.toLocaleLowerCase();
    const shown = needle ? chats.filter(({ title }) => title.toLocaleLowerCase().includes(needle)) : chats;

    return (
        <aside className={styles.sidebar} aria-label={t`Чати`}>
            <div className={styles.sidebarHeader}>
                <h1 className={styles.sidebarTitle}>{t`Чати`}</h1>
                <Button variant="primary" size="sm" iconOnly icon={SquarePen} onClick={onNew}>
                    {t`Новий чат`}
                </Button>
            </div>
            <SearchInput value={filter} onSearch={setFilter} placeholder={t`Пошук чатів`} />

            {isPending ? (
                <SkeletonList rows={7} />
            ) : error ? (
                <Alert tone="danger">{t`Не вдалося завантажити чати.`}</Alert>
            ) : shown.length === 0 ? (
                filter ? (
                    <p className={styles.pickerHint}>{t`Нічого не знайдено.`}</p>
                ) : (
                    <EmptyState
                        icon={MessagesSquare}
                        title={t`Чатів ще немає`}
                        text={t`Почніть розмову з колегою або створіть груповий чат.`}
                    >
                        <Button size="sm" icon={SquarePen} onClick={onNew}>
                            {t`Новий чат`}
                        </Button>
                    </EmptyState>
                )
            ) : (
                <nav aria-label={t`Список чатів`}>
                    <ul className={styles.chatList}>
                        {shown.map(({ chat, title }) => (
                            <li key={chat.id}>
                                <NavLink
                                    to={`/chats/${chat.id}`}
                                    className={({ isActive }) => [styles.chatLink, isActive && styles.chatActive].filter(Boolean).join(" ")}
                                >
                                    <ChatAvatar kind={chat.kind} others={chat.members} />
                                    <span className={styles.chatText}>
                                        <span className={styles.chatTop}>
                                            <span className={styles.chatName}>{title}</span>
                                            <time className={styles.chatTime} dateTime={chat.lastActivityAt}>
                                                {formatShortMoment(chat.lastActivityAt)}
                                            </time>
                                        </span>
                                        <span className={styles.chatBottom}>
                                            <span className={styles.chatPreview}>{previewText(chat.lastMessage, chat.kind, me)}</span>
                                            {chat.unreadCount > 0 && (
                                                <span className={styles.unread} aria-label={t`Непрочитаних: ${chat.unreadCount}`}>
                                                    {chat.unreadCount > 99 ? "99+" : chat.unreadCount}
                                                </span>
                                            )}
                                        </span>
                                    </span>
                                </NavLink>
                            </li>
                        ))}
                    </ul>
                </nav>
            )}
        </aside>
    );
}
