import type { I18n } from "@lingui/core";
import { msg } from "@lingui/core/macro";
import { FileCheck2, FileX2, LifeBuoy, MessageSquareReply, MessagesSquare, Signature, UserCheck, type LucideIcon } from "lucide-react";
import type { NotificationDto, NotificationKind, TicketStatus } from "@/shared/api/schema";
import { ticketStatusNames } from "@/shared/i18n/display";

/**
 * The icons of the notification kinds.
 */
export const notificationIcons: Record<NotificationKind, LucideIcon> = {
    NewsReply: MessageSquareReply,
    DocumentToSign: Signature,
    DocumentSigned: FileCheck2,
    DocumentRejected: FileX2,
    ChatMessage: MessagesSquare,
    TicketCreated: LifeBuoy,
    TicketReply: LifeBuoy,
    TicketStatusChanged: LifeBuoy,
    TicketAssigned: UserCheck,
};

/**
 * The titles of the notification kinds (the chat and the state change build their own).
 */
const titles = {
    NewsReply: msg`Відповідь на Ваш коментар`,
    DocumentToSign: msg`Документ очікує Вашого підпису`,
    DocumentSigned: msg`Документ підписали всі підписанти`,
    DocumentRejected: msg`Документ відхилено`,
    TicketCreated: msg`Нове звернення`,
    TicketReply: msg`Нова відповідь у зверненні`,
    TicketAssigned: msg`Вам призначено звернення`,
} as const;

/**
 * Builds the title of a notification in the interface language.
 * @param notification The notification.
 * @param i18n The translator (`useLingui().i18n`).
 * @returns The title.
 */
export function notificationTitle(notification: NotificationDto, i18n: I18n): string {
    switch (notification.kind) {
        case "ChatMessage":
            return notification.count > 1 ? i18n._(msg`Нові повідомлення: ${notification.count}`) : i18n._(msg`Нове повідомлення`);
        case "TicketStatusChanged": {
            // Own keys only: a code such as "constructor" must not pick up a member of Object.prototype.
            const code = notification.code;
            const status = code && Object.hasOwn(ticketStatusNames, code) ? ticketStatusNames[code as TicketStatus] : undefined;
            return status ? i18n._(msg`Стан звернення: ${i18n._(status)}`) : i18n._(msg`Стан звернення змінено`);
        }
        default:
            return i18n._(titles[notification.kind]);
    }
}
