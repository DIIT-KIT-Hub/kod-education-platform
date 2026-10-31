import { createContext, useContext } from "react";

/**
 * What the chapters need from the manual page.
 */
export interface ManualState {
    /** The topics that match the search, or null when nothing is searched. */
    readonly matched: ReadonlySet<string> | null;
    /** The titles of the permissions in the interface language (empty until loaded). */
    readonly permissionTitles: ReadonlyMap<string, string>;
    /** Whether the reader has a permission. */
    readonly has: (permission: string) => boolean;
}

/**
 * The state of the manual page.
 */
export const ManualContext = createContext<ManualState>({ matched: null, permissionTitles: new Map(), has: () => false });

/**
 * Reads the state of the manual page.
 * @returns The state.
 */
export function useManual(): ManualState {
    return useContext(ManualContext);
}
