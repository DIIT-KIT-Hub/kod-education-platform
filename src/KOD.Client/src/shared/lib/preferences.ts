import { useSyncExternalStore } from "react";

/**
 * A colour theme.
 */
export type Theme = "light" | "dark";

/**
 * Reads a preference stored in this browser.
 * @param key The storage key.
 * @returns The value, or null when storage is unavailable.
 */
function read(key: string): string | null {
    try {
        return window.localStorage.getItem(key);
    } catch {
        return null;
    }
}

/**
 * Stores a preference in this browser.
 * @param key The storage key.
 * @param value The value.
 */
function write(key: string, value: string): void {
    try {
        window.localStorage.setItem(key, value);
    } catch {
        // Private mode or blocked storage: the choice lasts until the page is reloaded.
    }
}

/**
 * Subscribes to changes of the <html> preference attributes (data-theme, data-sidebar).
 * @param onChange Called after a change.
 * @returns The unsubscribe function.
 */
function subscribe(onChange: () => void): () => void {
    const observer = new MutationObserver(onChange);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme", "data-sidebar"] });
    return () => observer.disconnect();
}

/**
 * Gets the current theme (set before the first paint by /theme-init.js).
 * @returns The theme.
 */
function currentTheme(): Theme {
    return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
}

/**
 * Gets whether the desktop sidebar is collapsed to an icon rail.
 * @returns True when collapsed.
 */
function sidebarCollapsed(): boolean {
    return document.documentElement.getAttribute("data-sidebar") === "collapsed";
}

/**
 * Switches between the light and dark themes and remembers the choice.
 */
export function toggleTheme(): void {
    const theme: Theme = currentTheme() === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", theme);
    write("kod-theme", theme);
}

/**
 * Collapses or expands the desktop sidebar and remembers the choice.
 */
export function toggleSidebar(): void {
    const collapsed = !sidebarCollapsed();
    document.documentElement.setAttribute("data-sidebar", collapsed ? "collapsed" : "expanded");
    write("kod-sidebar", collapsed ? "collapsed" : "expanded");
}

/**
 * Reads the current theme and re-renders when it changes.
 * @returns The theme.
 */
export function useTheme(): Theme {
    return useSyncExternalStore(subscribe, currentTheme);
}

/**
 * Reads whether the desktop sidebar is collapsed and re-renders when it changes.
 * @returns True when collapsed.
 */
export function useSidebarCollapsed(): boolean {
    return useSyncExternalStore(subscribe, sidebarCollapsed);
}

export { read as readPreference };
