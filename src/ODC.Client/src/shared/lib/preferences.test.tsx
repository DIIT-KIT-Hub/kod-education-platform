import { act, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { readPreference, toggleSidebar, toggleTheme, useSidebarCollapsed, useTheme } from "./preferences";

/**
 * Shows the current preferences.
 * @returns The values.
 */
function Probe() {
    const theme = useTheme();
    const collapsed = useSidebarCollapsed();
    return (
        <p>
            {theme} {collapsed ? "collapsed" : "expanded"}
        </p>
    );
}

beforeEach(() => {
    window.localStorage.clear();
});

afterEach(() => {
    document.documentElement.removeAttribute("data-theme");
    document.documentElement.removeAttribute("data-sidebar");
    vi.restoreAllMocks();
});

describe("toggleTheme", () => {
    it("switches from light to dark and back, remembering the choice", () => {
        toggleTheme();
        expect(document.documentElement).toHaveAttribute("data-theme", "dark");
        expect(window.localStorage.getItem("odc-theme")).toBe("dark");

        toggleTheme();
        expect(document.documentElement).toHaveAttribute("data-theme", "light");
        expect(window.localStorage.getItem("odc-theme")).toBe("light");
    });

    it("treats an unknown theme as light", () => {
        document.documentElement.setAttribute("data-theme", "sepia");
        toggleTheme();
        expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    });

    it("still switches when the storage is blocked", () => {
        vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
            throw new Error("blocked");
        });
        expect(() => toggleTheme()).not.toThrow();
        expect(document.documentElement).toHaveAttribute("data-theme", "dark");
    });
});

describe("toggleSidebar", () => {
    it("collapses and expands the sidebar, remembering the choice", () => {
        toggleSidebar();
        expect(document.documentElement).toHaveAttribute("data-sidebar", "collapsed");
        expect(window.localStorage.getItem("odc-sidebar")).toBe("collapsed");

        toggleSidebar();
        expect(document.documentElement).toHaveAttribute("data-sidebar", "expanded");
        expect(window.localStorage.getItem("odc-sidebar")).toBe("expanded");
    });
});

describe("readPreference", () => {
    it("reads a stored value", () => {
        window.localStorage.setItem("odc-theme", "dark");
        expect(readPreference("odc-theme")).toBe("dark");
        expect(readPreference("missing")).toBeNull();
    });

    it("returns null when the storage is blocked", () => {
        vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
            throw new Error("blocked");
        });
        expect(readPreference("odc-theme")).toBeNull();
    });
});

describe("useTheme and useSidebarCollapsed", () => {
    it("read the attributes of the document and follow their changes", async () => {
        render(<Probe />);
        expect(screen.getByText("light expanded")).toBeInTheDocument();

        act(() => toggleTheme());
        await waitFor(() => expect(screen.getByText("dark expanded")).toBeInTheDocument());

        act(() => toggleSidebar());
        await waitFor(() => expect(screen.getByText("dark collapsed")).toBeInTheDocument());
    });
});
