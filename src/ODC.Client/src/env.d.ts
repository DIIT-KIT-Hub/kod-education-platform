/// <reference types="vite/client" />

/**
 * Lingui catalogs: the Vite plugin compiles a .po file into its messages when it is imported.
 */
declare module "*.po" {
    import type { Messages } from "@lingui/core";

    export const messages: Messages;
}
