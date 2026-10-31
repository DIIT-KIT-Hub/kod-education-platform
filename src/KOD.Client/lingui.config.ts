import { defineConfig } from "@lingui/cli";
import { formatter } from "@lingui/format-po";

/**
 * Ukrainian is the source language: the Ukrainian text is the message id, as on the server (Texts.T). Messages that
 * need a different English translation for the same Ukrainian words carry a context.
 */
export default defineConfig({
    sourceLocale: "uk",
    locales: ["uk", "en"],
    fallbackLocales: { default: "uk" },
    catalogs: [
        {
            path: "<rootDir>/src/locales/{locale}/messages",
            include: ["<rootDir>/src"],
            exclude: ["**/node_modules/**", "**/*.test.*"],
        },
    ],
    format: formatter({ lineNumbers: false }),
});
