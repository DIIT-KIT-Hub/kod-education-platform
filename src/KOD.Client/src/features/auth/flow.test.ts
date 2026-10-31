import { describe, expect, it } from "vitest";
import { flowUrl } from "./flow";

describe("flowUrl", () => {
    it("adds nothing for the home page without remember me", () => {
        expect(flowUrl("/account/login-2fa", "/", false)).toBe("/account/login-2fa");
    });

    it("keeps the return address and the remember me choice", () => {
        expect(flowUrl("/account/login-2fa", "/news?page=2", true)).toBe("/account/login-2fa?returnUrl=%2Fnews%3Fpage%3D2&rememberMe=1");
    });
});
