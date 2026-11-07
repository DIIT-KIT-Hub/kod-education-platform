import { describe, expect, it } from "vitest";
import { viewTarget } from "./viewParams";

describe("viewTarget", () => {
    it("keeps an address without a query string", () => {
        expect(viewTarget("/documents")).toEqual({ to: "/documents", replace: false });
    });

    it("moves the query string into the history state", () => {
        expect(viewTarget("/documents?scope=Authored&status=Draft")).toEqual({
            to: { pathname: "/documents", hash: "" },
            state: { view: "scope=Authored&status=Draft" },
            replace: false,
        });
    });

    it("keeps the hash in the address", () => {
        expect(viewTarget("/news/1?comments=2#comment-5")).toEqual({
            to: { pathname: "/news/1", hash: "#comment-5" },
            state: { view: "comments=2" },
            replace: false,
        });
    });

    it("stays on the current page for a bare query string", () => {
        expect(viewTarget("?scope=AwaitingMe")).toEqual({ to: { hash: "" }, state: { view: "scope=AwaitingMe" }, replace: false });
    });
});
