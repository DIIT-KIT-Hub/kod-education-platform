import { describe, expect, it } from "vitest";
import { chapterOf, findEntry, helpChapters, helpEntries, neighboursOf, topicOf, topicPath } from "./contents";

/**
 * The sources of the page modules (`./content/uk/documents-list.tsx`).
 */
const sources = import.meta.glob<string>("./content/*/*.tsx", { query: "?raw", import: "default", eager: true });

/**
 * The topics that have a page in a language.
 * @param language The language folder.
 * @returns The topic ids.
 */
function pagesOf(language: string): string[] {
    return Object.keys(sources)
        .filter((file) => file.includes(`/content/${language}/`))
        .map((file) => file.slice(file.lastIndexOf("/") + 1, -".tsx".length));
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
        expect(topicOf("account-activation").title.message).toBe("Активація облікового запису");
        expect(() => chapterOf("missing")).toThrow();
        expect(() => topicOf("missing")).toThrow();
    });

    it.each(["uk", "en"])("has a page for every topic in %s, and no other pages", (language) => {
        const expected = helpEntries.map((entry) => entry.topic.id).sort();
        expect(pagesOf(language).sort()).toEqual(expected);
    });

    it("links only to existing topics and chapters", () => {
        const topics = new Set([...helpEntries.map((entry) => entry.topic.id), ...helpChapters.map((chapter) => chapter.id)]);
        const links = Object.values(sources).flatMap((source) =>
            [...source.matchAll(/<Ref to="([^"]+)"/gu)].map((match) => match[1] ?? ""),
        );
        expect(links.length).toBeGreaterThan(0);
        expect(links.filter((link) => !topics.has(link))).toEqual([]);
    });

    it("gives every page a subheading structure of h2 only (the page title is the h1)", () => {
        const offenders = Object.entries(sources)
            .filter(([, source]) => /<h[1345][\s>]/u.test(source))
            .map(([file]) => file);
        expect(offenders).toEqual([]);
    });

    it("lists the topics in reading order and finds their neighbours", () => {
        expect(helpEntries[0]?.topic.id).toBe("intro-about");
        expect(findEntry("documents-sign")?.chapter.id).toBe("documents");
        expect(findEntry("missing")).toBeUndefined();

        expect(neighboursOf("intro-about").previous).toBeUndefined();
        expect(neighboursOf("intro-about").next?.topic.id).toBe("intro-interface");
        const last = helpEntries[helpEntries.length - 1]?.topic.id ?? "";
        expect(neighboursOf(last).next).toBeUndefined();
        expect(neighboursOf("missing")).toEqual({ previous: undefined, next: undefined });

        // The last topic of a chapter leads to the first one of the next chapter.
        expect(neighboursOf("intro-glossary").next?.chapter.id).toBe("account");
        expect(topicPath("documents-sign")).toBe("/help/documents-sign");
        expect(topicPath("signing")).toBe("/help/signing-how");
    });
});
