import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { CircleAlert, FileText, Pencil, RotateCcw, SmilePlus, Trash2 } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import type { ReactionKind } from "@/shared/api/schema";
import { reactions } from "@/shared/i18n/display";
import { formatClock, formatDateTime } from "@/shared/i18n/format";
import { Avatar } from "@/shared/ui/Avatar";
import { confirm } from "@/shared/ui/ConfirmHost";
import { Icon } from "@/shared/ui/Icon";
import { Spinner } from "@/shared/ui/Spinner";
import { toast } from "@/shared/ui/Toast";
import { chatApi, receiveMessage, type ChatMessage } from "@/features/chats/api";
import { eventText, shortName } from "@/features/chats/display";
import { discardDraft, sendDraft } from "@/features/chats/send";
import { Attachments } from "./Attachments";
import styles from "./chats.module.css";
import { Composer } from "./Composer";

/**
 * Props of {@link MessageItem}.
 */
export interface MessageItemProps {
    /** The message. */
    readonly message: ChatMessage;
    /** The current user. */
    readonly me: string;
    /** Whether it follows a message of the same author closely (no avatar and name then). */
    readonly compact: boolean;
    /** Whether the name of the author is shown (group chats). */
    readonly showAuthor: boolean;
    /** Shows "seen" under the message (the other person of a one-to-one chat has read it). */
    readonly seen: boolean;
    /** Whether the message is being edited. */
    readonly editing: boolean;
    /** Starts or stops editing the message. */
    readonly onEditing: (editing: boolean) => void;
}

/**
 * One message of a conversation: the author's own on the right, others' on the left with their photo, system notes in
 * the middle. The text is the HTML sanitized by the server. Hovering or focusing shows the actions: a reaction for
 * everyone, editing and deleting for the author. A message that could not be sent offers "send again".
 * @param props The props.
 * @returns The message.
 */
export function MessageItem({ message, me, compact, showAuthor, seen, editing, onEditing }: MessageItemProps) {
    const { t, i18n } = useLingui();
    const queryClient = useQueryClient();
    const [picking, setPicking] = useState(false);
    const mine = message.author.id === me;

    if (message.kind === "System") {
        return (
            <li className={styles.system}>
                <span>{eventText(message.event, message.author, message.text)}</span>
                <time dateTime={message.createdAt}>{formatClock(message.createdAt)}</time>
            </li>
        );
    }

    /**
     * Sets or removes the user's reaction.
     * @param kind The emotion.
     */
    function react(kind: ReactionKind): void {
        setPicking(false);
        chatApi
            .react(message.chatId, message.id, kind)
            .then((updated) => receiveMessage(queryClient, updated))
            .catch((error: unknown) => toast(describeError(error, t`Не вдалося додати реакцію.`), { danger: true }));
    }

    /**
     * Deletes the message after a confirmation.
     */
    async function remove(): Promise<void> {
        const confirmed = await confirm({
            title: t`Видалити повідомлення?`,
            text: t`Повідомлення та його файли буде видалено для всіх учасників чату.`,
            confirmText: t`Видалити`,
            danger: true,
        });
        if (confirmed) {
            await chatApi
                .remove(message.chatId, message.id)
                .catch((error: unknown) => toast(describeError(error, t`Не вдалося видалити повідомлення.`), { danger: true }));
        }
    }

    /**
     * Sends a failed message again with its client identifier (the server keeps it once).
     */
    function retry(): void {
        const files = message.pending?.files ?? [];
        void sendDraft(queryClient, message.chatId, message.author, {
            clientId: message.clientId ?? crypto.randomUUID(),
            body: message.body,
            text: message.text,
            files,
        }).catch((error: unknown) => toast(describeError(error, t`Повідомлення не надіслано.`), { danger: true }));
    }

    const actionable = !message.pending && !message.isDeleted;
    return (
        <li
            className={styles.message}
            data-mine={mine ? "" : undefined}
            data-compact={compact ? "" : undefined}
            onMouseLeave={() => setPicking(false)}
        >
            {!mine && (
                <span className={styles.messageAvatar}>
                    {!compact && <Avatar name={message.author.fullName} photoId={message.author.photoId} size="sm" decorative />}
                </span>
            )}

            <div className={styles.messageBody}>
                {showAuthor && !compact && !mine && <span className={styles.author}>{shortName(message.author.fullName)}</span>}

                {editing ? (
                    <Composer
                        chatId={message.chatId}
                        editing={{
                            html: message.body,
                            save: async (html) => {
                                receiveMessage(queryClient, await chatApi.edit(message.chatId, message.id, html));
                                onEditing(false);
                            },
                            cancel: () => onEditing(false),
                        }}
                    />
                ) : (
                    <div
                        className={styles.bubble}
                        data-deleted={message.isDeleted ? "" : undefined}
                        data-failed={message.pending?.failed ? "" : undefined}
                    >
                        {message.isDeleted ? (
                            <span className={styles.deleted}>{t`Повідомлення видалено`}</span>
                        ) : (
                            <>
                                {message.body && (
                                    // The server sanitized the HTML (text formatting and safe links only).
                                    <div className={styles.text} dangerouslySetInnerHTML={{ __html: message.body }} />
                                )}
                                <Attachments items={message.attachments} />
                                {message.pending && message.pending.files.length > 0 && (
                                    <ul className={styles.files}>
                                        {message.pending.files.map((file, index) => (
                                            <li key={`${file.name}-${index}`} className={styles.file}>
                                                <span className={styles.fileIcon}>
                                                    <Icon icon={FileText} />
                                                </span>
                                                <span className={styles.fileName}>{file.name}</span>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </>
                        )}
                    </div>
                )}

                {message.reactions.length > 0 && !message.isDeleted && (
                    <div className={styles.reactions}>
                        {message.reactions.map((reaction) => {
                            const meta = reactions.find((r) => r.kind === reaction.kind);
                            const pressed = reaction.userIds.includes(me);
                            return (
                                <button
                                    key={reaction.kind}
                                    type="button"
                                    className={styles.reaction}
                                    aria-pressed={pressed}
                                    title={meta ? i18n._(meta.name) : reaction.kind}
                                    onClick={() => react(reaction.kind)}
                                >
                                    <span aria-hidden="true">{meta?.emoji}</span> {reaction.userIds.length}
                                </button>
                            );
                        })}
                    </div>
                )}

                <div className={styles.meta}>
                    {message.pending ? (
                        message.pending.failed ? (
                            <span className={styles.failed}>
                                <Icon icon={CircleAlert} size="sm" />
                                {t`Не надіслано`}
                                <button type="button" className={styles.metaButton} onClick={retry}>
                                    <Icon icon={RotateCcw} size="sm" />
                                    {t`Надіслати ще раз`}
                                </button>
                                <button
                                    type="button"
                                    className={styles.metaButton}
                                    onClick={() => discardDraft(queryClient, message.chatId, message.clientId ?? "")}
                                >
                                    {t`Прибрати`}
                                </button>
                            </span>
                        ) : (
                            <span className={styles.sending}>
                                <Spinner />
                                {t`Надсилання…`}
                            </span>
                        )
                    ) : (
                        <>
                            <time dateTime={message.createdAt} title={formatDateTime(message.createdAt)}>
                                {formatClock(message.createdAt)}
                            </time>
                            {message.editedAt && !message.isDeleted && <span>{t`змінено`}</span>}
                            {seen && <span className={styles.seen}>{t`Переглянуто`}</span>}
                        </>
                    )}
                </div>
            </div>

            {actionable && !editing && (
                <div className={styles.actions}>
                    <button
                        type="button"
                        className={styles.action}
                        aria-label={t`Реакція`}
                        title={t`Реакція`}
                        aria-expanded={picking}
                        onClick={() => setPicking(!picking)}
                    >
                        <Icon icon={SmilePlus} size="sm" />
                    </button>
                    {mine && (
                        <>
                            <button
                                type="button"
                                className={styles.action}
                                aria-label={t`Змінити`}
                                title={t`Змінити`}
                                onClick={() => onEditing(true)}
                            >
                                <Icon icon={Pencil} size="sm" />
                            </button>
                            <button
                                type="button"
                                className={styles.action}
                                aria-label={t`Видалити`}
                                title={t`Видалити`}
                                onClick={() => void remove()}
                            >
                                <Icon icon={Trash2} size="sm" />
                            </button>
                        </>
                    )}
                    {picking && (
                        <div className={styles.picker} role="group" aria-label={t`Реакції`}>
                            {reactions.map((reaction) => (
                                <button
                                    key={reaction.kind}
                                    type="button"
                                    className={styles.pickerItem}
                                    title={i18n._(reaction.name)}
                                    aria-label={i18n._(reaction.name)}
                                    onClick={() => react(reaction.kind)}
                                >
                                    {reaction.emoji}
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            )}
        </li>
    );
}
