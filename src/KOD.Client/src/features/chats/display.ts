import { t } from "@lingui/core/macro";
import type { ChatEvent, ChatKind, ChatMessageDto, ChatPreview, UserRef } from "@/shared/api/schema";

/**
 * What a chat needs to be named.
 */
export interface NamedChat {
    readonly kind: ChatKind;
    readonly title: string | null;
    /** The other members (or all members: the current user is skipped). */
    readonly members: readonly UserRef[];
    readonly memberCount: number;
}

/**
 * Shortens a full name to the last and first name («Коваленко Марія»).
 * @param fullName The full name.
 * @returns The short name.
 */
export function shortName(fullName: string): string {
    return fullName.split(" ").slice(0, 2).join(" ");
}

/**
 * Names a chat: the title of a group, otherwise the other people.
 * @param chat The chat.
 * @param me The current user.
 * @returns The name.
 */
export function chatTitle(chat: NamedChat, me: string): string {
    const others = chat.members.filter((m) => m.id !== me);
    if (chat.kind === "Direct") {
        return others[0]?.fullName ?? t`Чат`;
    }

    if (chat.title) {
        return chat.title;
    }

    const names = others.slice(0, 3).map((m) => shortName(m.fullName));
    const rest = chat.memberCount - 1 - names.length;
    return names.length === 0 ? t`Груповий чат` : rest > 0 ? t`${names.join(", ")} та ще ${rest}` : names.join(", ");
}

/**
 * Tells a system note of a chat in the reader's language (present tense: Ukrainian past tense would need the gender).
 * @param event What happened.
 * @param author The person who acted.
 * @param details The title or the names from the note.
 * @returns The text.
 */
export function eventText(event: ChatEvent | null, author: UserRef, details: string): string {
    const name = shortName(author.fullName);
    switch (event) {
        case "Created":
            return details ? t`${name} створює чат «${details}»` : t`${name} створює чат`;
        case "Renamed":
            return details ? t`${name} змінює назву чату на «${details}»` : t`${name} прибирає назву чату`;
        case "MembersAdded":
            return t`${name} додає до чату: ${details}`;
        case "MemberRemoved":
            return t`${name} вилучає з чату: ${details}`;
        case "MemberLeft":
            return t`${name} залишає чат`;
        default:
            return details;
    }
}

/**
 * The line of a chat in the list: the newest message with its author.
 * @param preview The newest message.
 * @param kind The kind of the chat.
 * @param me The current user.
 * @returns The text.
 */
export function previewText(preview: ChatPreview | null, kind: ChatKind, me: string): string {
    if (!preview) {
        return t`Повідомлень ще немає`;
    }

    if (preview.kind === "System") {
        return eventText(preview.event, preview.author, preview.text);
    }

    const content = preview.isDeleted
        ? t`Повідомлення видалено`
        : preview.text || (preview.attachmentCount === 1 ? t`Вкладення` : t`Вкладень: ${preview.attachmentCount}`);
    if (preview.author.id === me) {
        return t`Ви: ${content}`;
    }

    return kind === "Group" ? `${shortName(preview.author.fullName).split(" ")[1] ?? preview.author.fullName}: ${content}` : content;
}

/**
 * Whether a message is the author's own.
 * @param message The message.
 * @param me The current user.
 * @returns The answer.
 */
export function isMine(message: ChatMessageDto, me: string): boolean {
    return message.author.id === me;
}
