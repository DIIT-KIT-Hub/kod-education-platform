import { describe, expect, it } from "vitest";
import { safeReturnUrl } from "./navigation";

describe("safeReturnUrl", () => {
    it("keeps a path of the application", () => {
        expect(safeReturnUrl("/documents?page=2")).toBe("/documents?page=2");
    });

    it.each([null, "", "https://evil.example/", "//evil.example/", "/\\evil.example", "javascript:alert(1)"])("rejects %s", (value) => {
        expect(safeReturnUrl(value)).toBe("/");
    });
});
