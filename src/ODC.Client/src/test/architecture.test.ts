import { readdirSync, readFileSync } from "node:fs";
import { join, relative } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * The source folder.
 */
const root = join(process.cwd(), "src");

/**
 * Lists the TypeScript sources (tests and generated files left out).
 * @param folder The folder.
 * @returns The file paths.
 */
function sources(folder: string): string[] {
    return readdirSync(folder, { withFileTypes: true }).flatMap((entry) => {
        const path = join(folder, entry.name);
        if (entry.isDirectory()) {
            return entry.name === "locales" || entry.name === "test" ? [] : sources(path);
        }

        return /\.tsx?$/.test(entry.name) && !/\.test\.|\.d\.ts$/.test(entry.name) ? [path] : [];
    });
}

describe("client architecture", () => {
    it("passes the abort signal of every query to its request, so a page left early cancels it", () => {
        const offenders = sources(root).flatMap((file) => {
            const text = readFileSync(file, "utf8");
            return [...text.matchAll(/queryFn:\s*(async\s*)?\(\s*\)/g)].map(() => relative(root, file));
        });

        expect(offenders).toEqual([]);
    });

    it("uses the API client only through the shared module (no hand-written fetch to the API)", () => {
        const offenders = sources(root)
            .filter((file) => !file.includes(join("shared", "api")))
            .filter((file) => /\bfetch\(\s*["'`]\/api\//.test(readFileSync(file, "utf8")))
            .map((file) => relative(root, file));

        expect(offenders).toEqual([]);
    });
});
