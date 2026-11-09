import { describe, expect, it } from "vitest";
import { safeReturnUrl } from "./navigation";

/**
 * Where the browser would go for an address on this site.
 * @param value The address.
 * @returns The origin the browser resolves it to.
 */
function resolvedOrigin(value: string): string {
    return new URL(value, "https://localhost/account/login").origin;
}

describe("safeReturnUrl", () => {
    it("keeps a path of the application", () => {
        expect(safeReturnUrl("/documents?page=2")).toBe("/documents?page=2");
    });

    it.each(["/", "/news/1#comment-5", "/documents?scope=AwaitingMe&page=2", "/admin/users/00000000-0000-7000-8000-000000000001"])(
        "keeps %s",
        (value) => {
            expect(safeReturnUrl(value)).toBe(value);
        },
    );

    it.each([
        null,
        undefined,
        "",
        "https://evil.example/",
        "http://evil.example/",
        "//evil.example/",
        "///evil.example/",
        "/\\evil.example",
        "\\\\evil.example",
        "\\/evil.example",
        "javascript:alert(1)",
        "data:text/html,<script>alert(1)</script>",
        "documents",
        " /documents",
    ])("rejects %j", (value) => {
        expect(safeReturnUrl(value)).toBe("/");
    });

    it("uses the given fallback", () => {
        expect(safeReturnUrl("https://evil.example/", "/profile")).toBe("/profile");
        expect(safeReturnUrl(null, "/profile")).toBe("/profile");
    });

    it("never returns an address the browser resolves to another site", () => {
        for (const value of ["/documents", "/\\evil.example", "//evil.example", "https://evil.example"]) {
            expect(resolvedOrigin(safeReturnUrl(value))).toBe("https://localhost");
        }
    });

    it("rejects addresses with tabs or line breaks, which browsers would resolve to another site", () => {
        // The URL parser drops tabs and newlines, so "/\t/evil.example" becomes "//evil.example" (protocol-relative);
        // e.g. the "continue" ButtonLink of SetupAuthenticatorPage renders it as its href.
        for (const value of ["/\t/evil.example", "/\n/evil.example", "/\r/evil.example", "/\t\\evil.example"]) {
            expect(resolvedOrigin(safeReturnUrl(value))).toBe("https://localhost");
        }
    });
});
