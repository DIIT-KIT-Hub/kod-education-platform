import { describe, expect, it } from "vitest";
import { mediaUrl } from "./media";

describe("mediaUrl", () => {
    it("points to the full image by default", () => {
        expect(mediaUrl("abc")).toBe("/media/abc");
        expect(mediaUrl("abc", false)).toBe("/media/abc");
    });

    it("points to the thumbnail when asked", () => {
        expect(mediaUrl("abc", true)).toBe("/media/abc/thumb");
    });
});
