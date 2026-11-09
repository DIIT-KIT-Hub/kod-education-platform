import { safeReturnUrl } from "./navigation";

/**
 * The session storage key of the last page of the cabinet (outside the manual).
 */
const storageKey = "odc.last-page";

/**
 * A page of the cabinet with its history state (list views keep their filters there).
 */
export interface LastPage {
    /** The address (path, query and hash). */
    readonly path: string;
    /** The history state of the page. */
    readonly state: unknown;
}

/**
 * Remembers the page the user is on, so the manual can lead back to it.
 * @param page The page.
 */
export function rememberPage(page: LastPage): void {
    try {
        sessionStorage.setItem(storageKey, JSON.stringify(page));
    } catch {
        // Storage may be full or disabled: the manual then leads to the home page.
    }
}

/**
 * The last page of the cabinet the user saw in this tab.
 * @returns The page; the home page when none is known.
 */
export function lastPage(): LastPage {
    try {
        const stored: unknown = JSON.parse(sessionStorage.getItem(storageKey) ?? "null");
        if (stored && typeof stored === "object" && "path" in stored && typeof stored.path === "string") {
            return { path: safeReturnUrl(stored.path), state: "state" in stored ? stored.state : null };
        }
    } catch {
        // A broken value: start from the home page.
    }

    return { path: "/", state: null };
}
