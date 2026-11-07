import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

/**
 * Unidirectional architecture: shared → features → app. Shared code knows no feature, a feature knows no other
 * feature and nothing of the app.
 */
const features = ["admin", "auth", "documents", "help", "home", "news", "profile", "signing", "structure"];

export default tseslint.config(
    { ignores: ["dist", "node_modules", "src/locales/**/*.ts", "src/shared/api/schema.d.ts"] },
    js.configs.recommended,
    tseslint.configs.strict,
    reactHooks.configs.flat.recommended,
    {
        languageOptions: { globals: globals.browser },
        rules: {
            "no-restricted-imports": ["error", { patterns: [{ group: ["../*"], message: "Use the @/ alias across folders." }] }],
        },
    },
    {
        files: ["src/shared/**"],
        rules: {
            "no-restricted-imports": [
                "error",
                { patterns: [{ group: ["@/features/*", "@/app/*"], message: "Shared code must not depend on features or the app." }] },
            ],
        },
    },
    ...features.map((feature) => ({
        files: [`src/features/${feature}/**`],
        rules: {
            "no-restricted-imports": [
                "error",
                {
                    patterns: [
                        { group: ["@/app/*"], message: "Features must not depend on the app." },
                        {
                            group: features.filter((other) => other !== feature).map((other) => `@/features/${other}/*`),
                            message: "Features must not depend on each other.",
                        },
                    ],
                },
            ],
        },
    })),
);
