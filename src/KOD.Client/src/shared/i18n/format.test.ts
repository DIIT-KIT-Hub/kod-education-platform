import { i18n } from "@lingui/core";
import { describe, expect, it } from "vitest";
import { formatDate, formatDateTime } from "./format";

describe("formatDate", () => {
    it("writes a Ukrainian date in Kyiv time without the year abbreviation", () => {
        i18n.activate("uk");
        expect(formatDate("2026-10-28T23:30:00Z")).toBe("29 жовтня 2026");
        expect(formatDateTime("2026-10-28T23:30:00Z")).toBe("29.10.2026, 01:30");
    });

    it("writes an English date", () => {
        i18n.activate("en");
        expect(formatDate("2026-10-28T23:30:00Z")).toBe("29 October 2026");
    });

    it("shows a dash for no date", () => {
        expect(formatDate(null)).toBe("—");
    });
});
