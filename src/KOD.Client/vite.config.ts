import { lingui } from "@lingui/vite-plugin";
import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

/**
 * The application is served by KOD.Web from the same origin as the API, in development too (`pnpm dev` rebuilds into
 * `dist`, which KOD.Web serves). While the Blazor pages are being replaced it lives under /app/.
 */
export default defineConfig(({ mode }) => ({
    base: "/app/",
    resolve: {
        alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
    },
    plugins: [
        // React Compiler first: it must see the original components before other transforms.
        babel({ presets: [reactCompilerPreset()] }),
        react(),
        // Compiles the .po catalogs on import and transforms the Lingui macros natively; a production build fails on a
        // message without an English translation.
        lingui({ macroTransform: true, failOnMissing: mode === "production", failOnCompileError: true }),
    ],
    css: {
        modules: {
            localsConvention: "camelCaseOnly",
            generateScopedName: mode === "production" ? "[hash:base64:6]" : "[name]__[local]__[hash:base64:4]",
        },
    },
    build: {
        outDir: "dist",
        emptyOutDir: true,
        sourcemap: mode !== "production",
        // One rebuild in development must not leave stale hashed files behind.
        assetsDir: "assets",
        // The Content-Security-Policy allows fonts and scripts only from the site itself: never inline assets as data: URLs.
        assetsInlineLimit: 0,
    },
    test: {
        environment: "happy-dom",
        include: ["src/**/*.test.{ts,tsx}"],
    },
}));
