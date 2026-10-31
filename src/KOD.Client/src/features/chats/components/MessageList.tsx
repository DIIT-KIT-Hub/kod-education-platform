import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowDown } from "lucide-react";
import { Fragment, useEffect, useLayoutEffect, useRef, useState } from "react";
import type { ChatKind } from "@/shared/api/schema";
import { formatDate, kyivDay } from "@/shared/i18n/format";
import { Icon } from "@/shared/ui/Icon";
import { Spinner } from "@/shared/ui/Spinner";
import { loadOlder, type ChatThread } from "@/features/chats/api";
import styles from "./chats.module.css";
import { MessageItem } from "./MessageItem";

/**
 * How close (in pixels) to the end the list counts as "at the newest message".
 */
const bottomZone = 96;

/**
 * Messages of one author within this time are grouped (no repeated photo and name).
 */
const groupWindow = 5 * 60_000;

/**
 * Props of {@link MessageList}.
 */
export interface MessageListProps {
    /** The chat. */
    readonly chatId: string;
    /** The kind of the chat. */
    readonly kind: ChatKind;
    /** The loaded messages. */
    readonly thread: ChatThread;
    /** The current user. */
    readonly me: string;
    /** How far the other person of a one-to-one chat has read (for "seen"). */
    readonly peerRead: number;
    /** Called with the newest message number while the user sees the end of the conversation. */
    readonly onSeen: (sequence: number) => void;
}

/**
 * Names the day of a separator: today, yesterday or the date.
 * @param moment The moment of the first message of the day.
 * @param today The label of today.
 * @param yesterday The label of yesterday.
 * @returns The label.
 */
function dayLabel(moment: string, today: string, yesterday: string): string {
    const now = new Date();
    const day = kyivDay(new Date(moment));
    if (day === kyivDay(now)) {
        return today;
    }

    return day === kyivDay(new Date(now.getTime() - 24 * 3600_000)) ? yesterday : formatDate(moment);
}

/**
 * The scrolling list of a conversation, newest at the bottom: opens at the end, keeps the place when older messages
 * load above (scrolling up loads them), follows new messages while the user is at the end and otherwise offers a jump
 * to them. Days are separated, consecutive messages of one author grouped.
 * @param props The props.
 * @returns The list.
 */
export function MessageList({ chatId, kind, thread, me, peerRead, onSeen }: MessageListProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const scroller = useRef<HTMLDivElement>(null);
    const top = useRef<HTMLDivElement>(null);
    const atBottom = useRef(true);
    const previous = useRef<{ first?: string; last?: string; height: number }>({ height: 0 });
    const [loadingOlder, setLoadingOlder] = useState(false);
    const [unseen, setUnseen] = useState(false);
    const [editing, setEditing] = useState<string | null>(null);
    const messages = thread.messages;
    const newest = messages.filter((m) => !m.pending).at(-1)?.sequence ?? 0;

    /**
     * Scrolls to the newest message.
     * @param smooth Whether to animate.
     */
    function toBottom(smooth: boolean): void {
        const element = scroller.current;
        element?.scrollTo({ top: element.scrollHeight, behavior: smooth ? "smooth" : "auto" });
        atBottom.current = true;
        setUnseen(false);
    }

    /**
     * Reports the newest message as seen when the user looks at the end of the conversation.
     */
    function reportSeen(): void {
        if (atBottom.current && newest > 0 && document.visibilityState === "visible" && document.hasFocus()) {
            onSeen(newest);
        }
    }

    // Keeps the place: at the end on opening, the same message on top after older ones load, the end for new ones and
    // changed ones while the user is there.
    useLayoutEffect(() => {
        const element = scroller.current;
        if (!element) {
            return;
        }

        const first = messages[0]?.id;
        const last = messages.at(-1);
        const before = previous.current;
        if (before.first === undefined) {
            element.scrollTop = element.scrollHeight;
        } else if (first !== before.first && last?.id === before.last) {
            element.scrollTop += element.scrollHeight - before.height;
        } else if (last && last.id !== before.last) {
            if (atBottom.current || last.author.id === me) {
                element.scrollTop = element.scrollHeight;
                atBottom.current = true;
            } else {
                setUnseen(true);
            }
        } else if (atBottom.current) {
            // An edit or a reaction changed the height: stay at the end.
            element.scrollTop = element.scrollHeight;
        }

        previous.current = { first, last: last?.id, height: element.scrollHeight };
    }, [messages, me]);

    useEffect(reportSeen);

    useEffect(() => {
        const again = () => reportSeen();
        document.addEventListener("visibilitychange", again);
        window.addEventListener("focus", again);
        return () => {
            document.removeEventListener("visibilitychange", again);
            window.removeEventListener("focus", again);
        };
    });

    // Loads older messages when the top of the list comes into view.
    useEffect(() => {
        const sentinel = top.current;
        if (!sentinel || !thread.hasOlder) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries.some((entry) => entry.isIntersecting) && !loadingOlder) {
                    setLoadingOlder(true);
                    void loadOlder(queryClient, chatId)
                        .catch(() => undefined)
                        .finally(() => setLoadingOlder(false));
                }
            },
            { root: scroller.current, rootMargin: "400px 0px 0px 0px" },
        );
        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [chatId, queryClient, thread.hasOlder, loadingOlder]);

    // The last own message the other person of a one-to-one chat has read.
    const seenId =
        kind === "Direct" ? messages.filter((m) => !m.pending && m.author.id === me && m.sequence <= peerRead).at(-1)?.id : undefined;
    const today = t`Сьогодні`;
    const yesterday = t`Учора`;

    return (
        <div className={styles.listWrap}>
            <div
                ref={scroller}
                className={styles.messages}
                role="log"
                aria-live="polite"
                aria-label={t`Повідомлення`}
                tabIndex={0}
                onScroll={(event) => {
                    const element = event.currentTarget;
                    atBottom.current = element.scrollHeight - element.scrollTop - element.clientHeight < bottomZone;
                    previous.current = { ...previous.current, height: element.scrollHeight };
                    if (atBottom.current) {
                        setUnseen(false);
                        reportSeen();
                    }
                }}
            >
                <div ref={top} className={styles.topSentinel}>
                    {loadingOlder && <Spinner />}
                    {!thread.hasOlder && messages.length > 0 && <span>{t`Початок розмови`}</span>}
                </div>
                <ol className={styles.messageList}>
                    {messages.map((message, index) => {
                        const before = messages[index - 1];
                        const newDay = !before || kyivDay(new Date(before.createdAt)) !== kyivDay(new Date(message.createdAt));
                        const compact =
                            !newDay &&
                            before?.kind === "Text" &&
                            message.kind === "Text" &&
                            before.author.id === message.author.id &&
                            new Date(message.createdAt).getTime() - new Date(before.createdAt).getTime() < groupWindow;
                        return (
                            <Fragment key={message.id}>
                                {newDay && (
                                    <li className={styles.day}>
                                        <span>{dayLabel(message.createdAt, today, yesterday)}</span>
                                    </li>
                                )}
                                <MessageItem
                                    message={message}
                                    me={me}
                                    compact={compact}
                                    showAuthor={kind === "Group"}
                                    seen={message.id === seenId}
                                    editing={editing === message.id}
                                    onEditing={(on) => setEditing(on ? message.id : null)}
                                />
                            </Fragment>
                        );
                    })}
                </ol>
            </div>
            {unseen && (
                <button type="button" className={styles.jump} onClick={() => toBottom(true)}>
                    <Icon icon={ArrowDown} size="sm" />
                    {t`Нові повідомлення`}
                </button>
            )}
        </div>
    );
}
