import { act, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { describe, expect, it } from "vitest";
import { useViewParams, viewTarget, type ViewLink } from "./viewParams";

describe("viewTarget", () => {
    it("keeps an address without a query string", () => {
        expect(viewTarget("/documents")).toEqual({ to: "/documents", replace: false });
    });

    it("keeps an address with only a hash as it is", () => {
        expect(viewTarget("/news/1#comment-5")).toEqual({ to: "/news/1#comment-5", replace: false });
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

    it("does not take a question mark of the hash for a query string", () => {
        expect(viewTarget("/help#what?")).toEqual({ to: "/help#what?", replace: false });
    });

    it("stays on the current page for a bare query string", () => {
        expect(viewTarget("?scope=AwaitingMe")).toEqual({ to: { hash: "" }, state: { view: "scope=AwaitingMe" }, replace: false });
    });

    it("keeps an empty view for a trailing question mark", () => {
        expect(viewTarget("/documents?")).toEqual({ to: { pathname: "/documents", hash: "" }, state: { view: "" }, replace: false });
    });
});

/** The parameters and the setter of the hook. */
type View = ReturnType<typeof useViewParams>;

/**
 * Shows the view parameters of the page and hands the hook to the test.
 * @param props Receives the hook on every render.
 * @returns The parameters as text.
 */
function Probe({ onRender }: { readonly onRender: (view: View) => void }) {
    const view = useViewParams();
    onRender(view);
    return <p data-testid="params">{view[0].toString()}</p>;
}

/**
 * The hook as the probe last rendered it.
 */
let latest: View | undefined;

/**
 * Keeps the hook of the last render.
 * @param view The hook.
 */
function remember(view: View): void {
    latest = view;
}

/**
 * The hook as the probe last rendered it.
 * @returns The parameters and the setter.
 */
function probe(): View {
    if (!latest) {
        throw new Error("The probe has not rendered.");
    }

    return latest;
}

/**
 * Renders the probe on a list page.
 * @param url The opened address.
 * @param state The history state of the first entry.
 * @returns The router.
 */
function renderProbe(url = "/documents", state: unknown = null) {
    const { pathname, search, hash } = new URL(url, "https://localhost");
    const router = createMemoryRouter([{ path: "/documents", element: <Probe onRender={remember} /> }], {
        initialEntries: [{ pathname, search, hash, state }],
    });
    render(<RouterProvider router={router} />);
    return router;
}

/**
 * The view parameters shown by the probe.
 * @returns The text.
 */
function shown(): string {
    return screen.getByTestId("params").textContent ?? "";
}

describe("useViewParams", () => {
    it("reads the view from the history state", () => {
        renderProbe("/documents", { view: "scope=Authored&page=2" });
        expect(shown()).toBe("scope=Authored&page=2");
        expect(probe()[0].get("page")).toBe("2");
    });

    it("has no parameters without a view", () => {
        renderProbe("/documents", "not an object");
        expect(shown()).toBe("");
    });

    it("moves a query string of the address into the history state once, keeping other state", async () => {
        const router = renderProbe("/documents?scope=AwaitingMe#top", { other: 1 });

        expect(await screen.findByText("scope=AwaitingMe")).toBeInTheDocument();
        expect(router.state.location.search).toBe("");
        expect(router.state.location.hash).toBe("#top");
        expect(router.state.location.state).toEqual({ other: 1, view: "scope=AwaitingMe" });
        expect(router.state.historyAction).toBe("REPLACE");
    });

    it("prefers the query string of the address over an older view", () => {
        renderProbe("/documents?page=3", { view: "page=1" });
        expect(probe()[0].get("page")).toBe("3");
    });

    it("changes the view with an object and adds a history entry", async () => {
        const router = renderProbe("/documents", { view: "page=2", keep: true });

        await act(() => probe()[1]({ scope: "Signed" }));

        expect(shown()).toBe("scope=Signed");
        expect(router.state.location.pathname).toBe("/documents");
        expect(router.state.location.state).toEqual({ keep: true, view: "scope=Signed" });
        expect(router.state.historyAction).toBe("PUSH");
    });

    it("changes the view with a function of the current parameters", async () => {
        renderProbe("/documents", { view: "scope=Signed&page=4" });

        await act(() =>
            probe()[1]((current) => {
                current.delete("page");
                current.set("search", "наказ");
                return current;
            }),
        );

        expect(probe()[0].get("scope")).toBe("Signed");
        expect(probe()[0].get("page")).toBeNull();
        expect(probe()[0].get("search")).toBe("наказ");
    });

    it("does not change the parameters it hands to the function", async () => {
        renderProbe("/documents", { view: "page=4" });
        const before = probe()[0];

        await act(() =>
            probe()[1]((current) => {
                current.set("page", "5");
                return current;
            }),
        );

        expect(before.get("page")).toBe("4");
    });

    it("replaces the history entry when asked", async () => {
        const router = renderProbe("/documents");

        await act(() => probe()[1](new URLSearchParams("page=2"), { replace: true, preventScrollReset: true }));

        expect(shown()).toBe("page=2");
        expect(router.state.historyAction).toBe("REPLACE");
    });

    it("builds link targets to other views of the page", () => {
        renderProbe("/documents", { view: "page=2", keep: true });

        const target: ViewLink = probe()[2](new URLSearchParams("page=3"));

        expect(target).toEqual({ to: { pathname: "/documents" }, state: { keep: true, view: "page=3" }, replace: false });
    });

    it("restores the previous view on Back", async () => {
        const router = renderProbe("/documents", { view: "page=1" });

        await act(() => probe()[1]({ page: "2" }));
        expect(shown()).toBe("page=2");

        await act(() => router.navigate(-1));
        expect(shown()).toBe("page=1");
    });
});
