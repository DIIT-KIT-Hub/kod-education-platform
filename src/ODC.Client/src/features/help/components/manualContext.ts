import { createContext, useContext } from "react";

/**
 * What the manual pages know about the reader.
 */
export interface ManualState {
    /** The translated titles of the permissions (for the access badges). */
    readonly permissionTitles: ReadonlyMap<string, string>;
    /** Whether the reader holds a permission. */
    readonly has: (permission: string) => boolean;
}

/**
 * The reader of the manual, provided by the help layout.
 */
export const ManualContext = createContext<ManualState>({ permissionTitles: new Map(), has: () => false });

/**
 * Reads the reader of the manual.
 * @returns The state.
 */
export function useManual(): ManualState {
    return useContext(ManualContext);
}
