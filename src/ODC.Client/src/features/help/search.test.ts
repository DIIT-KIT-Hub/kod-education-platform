import { describe, expect, it } from "vitest";
import { headingId, normalize, searchManual, termsOf, type SearchDocument } from "./search";

/**
 * A few pages of the manual for the search.
 */
const documents: SearchDocument[] = [
    {
        id: "documents-sign",
        chapter: "Документи",
        title: "Підписання документа",
        headings: [{ id: "chergovist", text: "Черговість підписантів" }],
        text: "Щоб підписати документ, відкрийте його картку й натисніть «Підписати». Ключ зчитується лише в браузері.",
    },
    {
        id: "documents-create",
        chapter: "Документи",
        title: "Створення документа",
        headings: [{ id: "marshrut", text: "Маршрут підписання" }],
        text: "Натисніть «Новий документ», заповніть назву й додайте файл.",
    },
    {
        id: "chats-start",
        chapter: "Чати",
        title: "Початок розмови",
        headings: [],
        text: "Знайдіть колегу за прізвищем і напишіть йому. Документ можна переслати як файл.",
    },
];

describe("termsOf", () => {
    it("lower-cases the query and drops words shorter than two letters", () => {
        expect(termsOf("  Підписати   Документ  я ", "uk")).toEqual(["підписати", "документ"]);
        expect(termsOf("a", "uk")).toEqual([]);
    });
});

describe("normalize", () => {
    it("treats every apostrophe alike and keeps the length", () => {
        expect(normalize("Ім’я Ім`я Імʼя", "uk")).toBe("ім'я ім'я ім'я");
        expect(normalize("Ім’я", "uk")).toHaveLength(4);
    });
});

describe("searchManual", () => {
    it("finds nothing for an empty or too short query", () => {
        expect(searchManual(documents, "", "uk")).toEqual([]);
        expect(searchManual(documents, "п", "uk")).toEqual([]);
    });

    it("needs every word of the query on the page", () => {
        expect(searchManual(documents, "підписати колегу", "uk")).toEqual([]);
        expect(searchManual(documents, "колегу прізвищем", "uk").map((hit) => hit.id)).toEqual(["chats-start"]);
    });

    it("puts title matches first, then subheadings, then the text", () => {
        const ids = searchManual(documents, "документ", "uk").map((hit) => hit.id);
        expect(ids[0]).toBe("documents-sign");
        expect(ids).toContain("chats-start");
        expect(ids.indexOf("chats-start")).toBe(ids.length - 1);
    });

    it("leads to the matching subheading and shows the text around the match", () => {
        const [hit] = searchManual(documents, "маршрут", "uk");
        expect(hit?.id).toBe("documents-create");
        expect(hit?.heading).toEqual({ id: "marshrut", text: "Маршрут підписання" });

        const [sign] = searchManual(documents, "ключ", "uk");
        expect(sign?.snippet?.match).toBe("Ключ");
        expect(sign?.snippet?.before.startsWith("…")).toBe(true);
    });

    it("ignores case and apostrophes", () => {
        expect(searchManual(documents, "ПІДПИСАННЯ", "uk").length).toBeGreaterThan(0);
    });

    it("returns at most the given number of results", () => {
        expect(searchManual(documents, "документ", "uk", 1)).toHaveLength(1);
    });
});

describe("headingId", () => {
    it("keeps letters of any alphabet and joins words with dashes", () => {
        expect(headingId("Черговість підписантів")).toBe("черговість-підписантів");
        expect(headingId("Step 2: sign!")).toBe("step-2-sign");
        expect(headingId("!!!")).toBe("section");
    });
});
