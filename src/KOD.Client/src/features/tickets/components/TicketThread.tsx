import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Send } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import type { TicketMessageDto } from "@/shared/api/schema";
import { ticketStatusNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { Avatar } from "@/shared/ui/Avatar";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { Textarea } from "@/shared/ui/Textarea";
import { toast } from "@/shared/ui/Toast";
import { ticketApi, ticketKeys } from "@/features/tickets/api";
import { FilePicker } from "./FilePicker";
import { TicketAttachments } from "./TicketAttachments";
import styles from "./tickets.module.css";

/**
 * Props of {@link TicketThread}.
 */
export interface TicketThreadProps {
    /** The history, oldest first (the first message is the description). */
    readonly messages: readonly TicketMessageDto[];
}

/**
 * The history of a request: the description, the messages of the author and the handlers (the handlers' ones marked)
 * and short notes about the changes of the state and the handler.
 * @param props The props.
 * @returns The history.
 */
export function TicketThread({ messages }: TicketThreadProps) {
    const { t } = useLingui();

    /**
     * The text of a note about a change.
     * @param message The note.
     * @returns The text.
     */
    function note(message: TicketMessageDto): string {
        if (message.kind === "StatusChanged" && message.status) {
            return t`Стан змінено на «${t(ticketStatusNames[message.status])}»`;
        }

        return message.body ? t`Виконавець: ${message.body}` : t`Виконавця знято`;
    }

    return (
        <ol className={styles.thread}>
            {messages.map((message, index) =>
                message.kind === "Comment" ? (
                    <li
                        key={message.id}
                        className={styles.message}
                        data-staff={!message.fromAuthor || undefined}
                        data-first={index === 0 || undefined}
                    >
                        <Avatar name={message.author.fullName} photoId={message.author.photoId} size="sm" decorative />
                        <div className={styles.messageCard}>
                            <div className={styles.messageHeader}>
                                <span className={styles.messageAuthor}>{message.author.fullName}</span>
                                {!message.fromAuthor && <Badge tone="inverse">{t`Підтримка`}</Badge>}
                                {index === 0 && <Badge tone="outline">{t`Опис звернення`}</Badge>}
                                <time className={styles.messageTime} dateTime={message.createdAt}>
                                    {formatDateTime(message.createdAt)}
                                </time>
                            </div>
                            {message.body && <p className={styles.messageBody}>{message.body}</p>}
                            {message.attachments.length > 0 && <TicketAttachments items={message.attachments} />}
                        </div>
                    </li>
                ) : (
                    <li key={message.id} className={styles.note}>
                        <span>{note(message)}</span>
                        <span className={styles.noteMeta}>
                            {message.author.fullName} · {formatDateTime(message.createdAt)}
                        </span>
                    </li>
                ),
            )}
        </ol>
    );
}

/**
 * Props of {@link ReplyBox}.
 */
export interface ReplyBoxProps {
    /** The request. */
    readonly ticketId: string;
    /** The hint inside the empty text box. */
    readonly placeholder: string;
}

/**
 * Writes a message with files in a request.
 * @param props The props.
 * @returns The form.
 */
export function ReplyBox({ ticketId, placeholder }: ReplyBoxProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const [body, setBody] = useState("");
    const [files, setFiles] = useState<readonly File[]>([]);
    const [sending, setSending] = useState(false);
    const empty = body.trim().length === 0 && files.length === 0;

    /**
     * Sends the message.
     */
    async function send(): Promise<void> {
        if (empty) {
            return;
        }

        setSending(true);
        try {
            await ticketApi.comment(ticketId, body.trim(), files);
            setBody("");
            setFiles([]);
            await queryClient.invalidateQueries({ queryKey: ticketKeys.all });
        } catch (error) {
            toast(describeError(error, t`Не вдалося надіслати повідомлення.`), { danger: true });
        } finally {
            setSending(false);
        }
    }

    return (
        <form
            className={styles.reply}
            onSubmit={(event) => {
                event.preventDefault();
                void send();
            }}
        >
            <Textarea
                aria-label={t`Повідомлення`}
                value={body}
                rows={4}
                maxLength={10_000}
                placeholder={placeholder}
                disabled={sending}
                onChange={(event) => setBody(event.target.value)}
                onKeyDown={(event) => {
                    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                        event.preventDefault();
                        void send();
                    }
                }}
            />
            <div className={styles.replyBar}>
                <FilePicker files={files} onChange={setFiles} disabled={sending} />
                <Button type="submit" icon={Send} busy={sending} disabled={empty}>
                    {t`Надіслати`}
                </Button>
            </div>
        </form>
    );
}
