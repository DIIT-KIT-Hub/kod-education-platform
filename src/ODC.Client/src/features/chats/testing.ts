/*
 * Fixtures of the chat tests (typed by the API schema, so a contract change breaks the tests at compile time).
 */
import { makeUser, makeUserRef, type Schemas } from "@/test/factories";

/**
 * The signed-in user of the chat tests.
 */
export const me = makeUser();

/**
 * The signed-in user as a reference.
 */
export const meRef: Schemas["UserRef"] = { id: me.id, fullName: me.fullName, photoId: me.photoId };

/**
 * The other person of most tests.
 */
export const maria = makeUserRef();

/**
 * A third person.
 */
export const petro = makeUserRef({ id: "00000000-0000-7000-8000-000000000003", fullName: "Петренко Петро Іванович" });

/**
 * A fourth person.
 */
export const olena = makeUserRef({ id: "00000000-0000-7000-8000-000000000004", fullName: "Іваненко Олена Сергіївна" });

/**
 * The identifier of the chat of most tests.
 */
export const chatId = "10000000-0000-7000-8000-000000000001";

/**
 * A message.
 * @param sequence The number in the chat.
 * @param overrides Changed fields.
 * @returns The message.
 */
export function makeMessage(sequence: number, overrides: Partial<Schemas["ChatMessageDto"]> = {}): Schemas["ChatMessageDto"] {
    return {
        id: `20000000-0000-7000-8000-${String(sequence).padStart(12, "0")}`,
        chatId,
        sequence,
        author: maria,
        kind: "Text",
        event: null,
        body: `<p>Повідомлення ${sequence}</p>`,
        text: `Повідомлення ${sequence}`,
        createdAt: new Date(Date.UTC(2026, 10, 8, 8, 0, sequence)).toISOString(),
        editedAt: null,
        isDeleted: false,
        clientId: null,
        attachments: [],
        reactions: [],
        isHidden: false,
        forwardedFrom: null,
        ...overrides,
    };
}

/**
 * A page of messages.
 * @param messages The messages.
 * @param hasMore Whether more exist in the requested direction.
 * @returns The page.
 */
export function makeMessagePage(messages: readonly Schemas["ChatMessageDto"][], hasMore = false): Schemas["ChatMessagePage"] {
    return { messages: [...messages], hasMore, lastSequence: messages.at(-1)?.sequence ?? 0 };
}

/**
 * A chat of the list.
 * @param overrides Changed fields.
 * @returns The chat.
 */
export function makeSummary(overrides: Partial<Schemas["ChatSummary"]> = {}): Schemas["ChatSummary"] {
    return {
        id: chatId,
        kind: "Direct",
        title: null,
        members: [maria],
        memberCount: 2,
        lastMessage: null,
        lastActivityAt: "2026-11-08T08:00:00Z",
        lastSequence: 0,
        lastReadSequence: 0,
        unreadCount: 0,
        ...overrides,
    };
}

/**
 * A member of a chat.
 * @param user The person.
 * @param overrides Changed fields.
 * @returns The member.
 */
export function makeMember(user: Schemas["UserRef"], overrides: Partial<Schemas["ChatMemberDto"]> = {}): Schemas["ChatMemberDto"] {
    return { user, role: "Member", position: null, lastReadSequence: 0, isActive: true, ...overrides };
}

/**
 * A chat with its members.
 * @param overrides Changed fields.
 * @returns The chat.
 */
export function makeDetails(overrides: Partial<Schemas["ChatDetails"]> = {}): Schemas["ChatDetails"] {
    return {
        id: chatId,
        kind: "Direct",
        title: null,
        members: [makeMember(meRef, { role: "Owner" }), makeMember(maria, { position: "Бухгалтер, Фінансовий відділ" })],
        lastSequence: 0,
        myRole: "Owner",
        ...overrides,
    };
}

/**
 * A person found by the people search.
 * @param user The person.
 * @param overrides Changed fields.
 * @returns The search result.
 */
export function makePerson(user: Schemas["UserRef"], overrides: Partial<Schemas["UserLookupItem"]> = {}): Schemas["UserLookupItem"] {
    return { id: user.id, fullName: user.fullName, email: `${user.id.slice(-1)}@odc.local`, position: null, photoId: null, ...overrides };
}

/**
 * A file of the given size.
 * @param name The file name.
 * @param size The size in bytes.
 * @param type The media type.
 * @returns The file.
 */
export function makeFile(name: string, size = 10, type = "application/pdf"): File {
    const file = new File(["x"], name, { type });
    Object.defineProperty(file, "size", { value: size });
    return file;
}
