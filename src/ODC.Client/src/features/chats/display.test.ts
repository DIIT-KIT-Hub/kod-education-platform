import { describe, expect, it } from "vitest";
import type { ChatPreview } from "@/shared/api/schema";
import { chatTitle, eventText, isMine, previewText, shortName } from "./display";
import { makeMessage, maria, me, meRef, olena, petro } from "./testing";

/**
 * A preview of the newest message.
 * @param overrides Changed fields.
 * @returns The preview.
 */
function preview(overrides: Partial<ChatPreview> = {}): ChatPreview {
    return {
        author: maria,
        kind: "Text",
        event: null,
        text: "Добрий день",
        isDeleted: false,
        attachmentCount: 0,
        createdAt: "2026-11-08T08:00:00Z",
        ...overrides,
    };
}

describe("shortName", () => {
    it("keeps the last and the first name", () => {
        expect(shortName("Коваленко Марія Андріївна")).toBe("Коваленко Марія");
        expect(shortName("Коваленко")).toBe("Коваленко");
    });
});

describe("chatTitle", () => {
    it("names a one-to-one chat after the other person", () => {
        expect(chatTitle({ kind: "Direct", title: null, members: [meRef, maria], memberCount: 2 }, me.id)).toBe(maria.fullName);
    });

    it("falls back to «Чат» for a one-to-one chat without the other person", () => {
        expect(chatTitle({ kind: "Direct", title: null, members: [meRef], memberCount: 1 }, me.id)).toBe("Чат");
    });

    it("uses the title of a group", () => {
        expect(chatTitle({ kind: "Group", title: "Бухгалтерія", members: [maria, petro], memberCount: 3 }, me.id)).toBe("Бухгалтерія");
    });

    it("names an untitled group after up to three members", () => {
        expect(chatTitle({ kind: "Group", title: null, members: [maria, petro], memberCount: 3 }, me.id)).toBe(
            "Коваленко Марія, Петренко Петро",
        );
    });

    it("counts the members beyond three", () => {
        expect(chatTitle({ kind: "Group", title: null, members: [meRef, maria, petro, olena], memberCount: 6 }, me.id)).toBe(
            "Коваленко Марія, Петренко Петро, Іваненко Олена та ще 2",
        );
    });

    it("calls a group without other members «Груповий чат»", () => {
        expect(chatTitle({ kind: "Group", title: null, members: [meRef], memberCount: 1 }, me.id)).toBe("Груповий чат");
    });
});

describe("eventText", () => {
    it.each([
        ["Created", "Відділ", "Коваленко Марія створює чат «Відділ»"],
        ["Created", "", "Коваленко Марія створює чат"],
        ["Renamed", "Нова", "Коваленко Марія змінює назву чату на «Нова»"],
        ["Renamed", "", "Коваленко Марія прибирає назву чату"],
        ["MembersAdded", "Петренко Петро", "Коваленко Марія додає до чату: Петренко Петро"],
        ["MemberRemoved", "Петренко Петро", "Коваленко Марія вилучає з чату: Петренко Петро"],
        ["MemberLeft", "", "Коваленко Марія залишає чат"],
    ] as const)("tells %s with «%s»", (event, details, text) => {
        expect(eventText(event, maria, details)).toBe(text);
    });

    it("shows the details of an unknown note", () => {
        expect(eventText(null, maria, "Щось сталося")).toBe("Щось сталося");
    });
});

describe("previewText", () => {
    it("says there are no messages yet", () => {
        expect(previewText(null, "Direct", me.id)).toBe("Повідомлень ще немає");
    });

    it("tells a system note", () => {
        expect(previewText(preview({ kind: "System", event: "MemberLeft", text: "" }), "Group", me.id)).toBe("Коваленко Марія залишає чат");
    });

    it("shows the text of the other person in a one-to-one chat", () => {
        expect(previewText(preview(), "Direct", me.id)).toBe("Добрий день");
    });

    it("prefixes the first name of the author in a group", () => {
        expect(previewText(preview(), "Group", me.id)).toBe("Марія: Добрий день");
    });

    it("uses the full name of an author with one name in a group", () => {
        expect(previewText(preview({ author: { ...maria, fullName: "Адміністратор" } }), "Group", me.id)).toBe("Адміністратор: Добрий день");
    });

    it("prefixes the own messages with «Ви»", () => {
        expect(previewText(preview({ author: meRef }), "Group", me.id)).toBe("Ви: Добрий день");
    });

    it("says a message was deleted", () => {
        expect(previewText(preview({ isDeleted: true }), "Direct", me.id)).toBe("Повідомлення видалено");
    });

    it("counts the attachments of a message without text", () => {
        expect(previewText(preview({ text: "", attachmentCount: 1 }), "Direct", me.id)).toBe("Вкладення");
        expect(previewText(preview({ text: "", attachmentCount: 3 }), "Direct", me.id)).toBe("Вкладень: 3");
    });
});

describe("isMine", () => {
    it("compares the author with the current user", () => {
        expect(isMine(makeMessage(1, { author: meRef }), me.id)).toBe(true);
        expect(isMine(makeMessage(1), me.id)).toBe(false);
    });
});
