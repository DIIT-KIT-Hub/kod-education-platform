import { describe, expect, it } from "vitest";
import { chapterOf, helpChapters, topicOf } from "./contents";

/** The source of every chapter file, by path. */
const sources = import.meta.glob<string>("./chapters/*/*Chapter.tsx", { query: "?raw", import: "default", eager: true });

/**
 * The anchors a chapter file uses for its chapter and topics.
 * @param language The folder of the language.
 * @returns The anchors in order.
 */
function anchorsOf(language: string): string[] {
    return Object.entries(sources)
        .filter(([file]) => file.includes(`/chapters/${language}/`))
        .flatMap(([, source]) => [...source.matchAll(/<(?:Chapter|Topic) id="([^"]+)"/gu)].map((match) => match[1] ?? ""));
}

describe("manual contents", () => {
    it("has unique anchors and topics in every chapter", () => {
        const anchors = helpChapters.flatMap((chapter) => [chapter.id, ...chapter.topics.map((topic) => topic.id)]);
        expect(new Set(anchors).size).toBe(anchors.length);
        for (const chapter of helpChapters) {
            expect(chapter.topics.length).toBeGreaterThan(0);
        }
    });

    it("numbers the chapters in order", () => {
        expect(chapterOf("intro").number).toBe(1);
        expect(chapterOf("faq").number).toBe(helpChapters.length);
        expect(topicOf("account-first-login").title.message).toBe("Перший вхід у систему");
    });

    it.each(["uk", "en"])("has every chapter and topic in the %s chapters, and nothing else", (language) => {
        const expected = helpChapters.flatMap((chapter) => [chapter.id, ...chapter.topics.map((topic) => topic.id)]).sort();
        expect(anchorsOf(language).sort()).toEqual(expected);
    });

    it("links only to existing topics", () => {
        const anchors = new Set(helpChapters.flatMap((chapter) => [chapter.id, ...chapter.topics.map((topic) => topic.id)]));
        const links = Object.values(sources).flatMap((source) =>
            [...source.matchAll(/<Ref to="([^"]+)"/gu)].map((match) => match[1] ?? ""),
        );
        expect(links.length).toBeGreaterThan(0);
        expect(links.filter((link) => !anchors.has(link))).toEqual([]);
    });
});
