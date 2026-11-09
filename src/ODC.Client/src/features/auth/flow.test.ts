import { renderHook, waitFor } from "@testing-library/react";
import { createElement, type ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import { createTestQueryClient } from "@/test/render";
import { makeAuthContext, makeUser } from "@/test/factories";
import { mockApi } from "@/test/server";
import { authContextQuery } from "@/shared/session/session";
import { completeSignIn, flowUrl, useRememberMe, useReturnUrl } from "./flow";

/**
 * A router wrapper opened at an address, for the hooks that read the query string.
 * @param url The address.
 * @returns The wrapper component.
 */
function at(url: string) {
    return function Wrapper({ children }: { readonly children: ReactNode }) {
        return createElement(MemoryRouter, { initialEntries: [url] }, children);
    };
}

describe("flowUrl", () => {
    it("adds nothing for the home page without remember me", () => {
        expect(flowUrl("/account/login-2fa", "/", false)).toBe("/account/login-2fa");
    });

    it("keeps the return address and the remember me choice", () => {
        expect(flowUrl("/account/login-2fa", "/news?page=2", true)).toBe("/account/login-2fa?returnUrl=%2Fnews%3Fpage%3D2&rememberMe=1");
    });

    it("keeps only the remember me choice for the home page", () => {
        expect(flowUrl("/account/login-recovery", "/", true)).toBe("/account/login-recovery?rememberMe=1");
    });

    it("keeps only the return address without remember me", () => {
        expect(flowUrl("/account/setup-authenticator", "/documents", false)).toBe("/account/setup-authenticator?returnUrl=%2Fdocuments");
    });
});

describe("useReturnUrl", () => {
    it.each([
        ["/account/login", "/"],
        ["/account/login?returnUrl=%2Fdocuments%3Fscope%3DMine", "/documents?scope=Mine"],
        ["/account/login?returnUrl=https%3A%2F%2Fevil.example", "/"],
        ["/account/login?returnUrl=%2F%2Fevil.example", "/"],
        ["/account/login?returnUrl=%2F%5Cevil.example", "/"],
        ["/account/login?returnUrl=documents", "/"],
        ["/account/login?returnUrl=", "/"],
    ])("of %s is %s", (url, expected) => {
        const { result } = renderHook(() => useReturnUrl(), { wrapper: at(url) });
        expect(result.current).toBe(expected);
    });
});

describe("useRememberMe", () => {
    it.each([
        ["/account/login-2fa", false],
        ["/account/login-2fa?rememberMe=1", true],
        ["/account/login-2fa?rememberMe=true", false],
        ["/account/login-2fa?rememberMe=0", false],
    ])("of %s is %s", (url, expected) => {
        const { result } = renderHook(() => useRememberMe(), { wrapper: at(url) });
        expect(result.current).toBe(expected);
    });
});

describe("completeSignIn", () => {
    it("loads the auth context of the signed-in identity again", async () => {
        const signedIn = makeAuthContext(makeUser({ fullName: "Нова Людина" }));
        const calls = mockApi("get", "/api/v1/auth/context", { ...signedIn, antiforgeryToken: "new-token" });
        const queryClient = createTestQueryClient();
        queryClient.setQueryData(authContextQuery.queryKey, makeAuthContext(null));

        await completeSignIn(queryClient);

        expect(calls).toHaveLength(1);
        await waitFor(() => expect(queryClient.getQueryData(authContextQuery.queryKey)?.user?.fullName).toBe("Нова Людина"));
        expect(queryClient.getQueryData(authContextQuery.queryKey)?.antiforgeryToken).toBe("new-token");
    });
});
