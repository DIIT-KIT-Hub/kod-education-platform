import { describe, expect, it } from "vitest";
import { fromIsoDate, parseTypedDate, toIsoDate } from "./DatePicker";

describe("parseTypedDate", () => {
    it("reads day, month and year with any separator", () => {
        expect(toIsoDate(parseTypedDate("25.10.2026") as Date)).toBe("2026-10-25");
        expect(toIsoDate(parseTypedDate("25/10/2026") as Date)).toBe("2026-10-25");
        expect(toIsoDate(parseTypedDate(" 5-3-26 ") as Date)).toBe("2026-03-05");
    });

    it("reads eight digits in a row and ISO dates", () => {
        expect(toIsoDate(parseTypedDate("25102026") as Date)).toBe("2026-10-25");
        expect(toIsoDate(parseTypedDate("2026-10-25") as Date)).toBe("2026-10-25");
    });

    it("rejects impossible days and other text", () => {
        expect(parseTypedDate("31.02.2026")).toBeNull();
        expect(parseTypedDate("завтра")).toBeNull();
        expect(parseTypedDate("10.2026")).toBeNull();
    });
});

describe("fromIsoDate", () => {
    it("parses only real calendar days", () => {
        expect(fromIsoDate("2024-02-29")?.getDate()).toBe(29);
        expect(fromIsoDate("2026-02-29")).toBeNull();
        expect(fromIsoDate("")).toBeNull();
    });
});
