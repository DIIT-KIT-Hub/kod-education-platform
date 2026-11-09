import { HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { ApiError } from "@/shared/api/problem";
import { captureDownloads } from "@/test/downloads";
import { file, mockApi, problem } from "@/test/server";
import { downloadFile, fileNameOf } from "./download";

describe("fileNameOf", () => {
    it("prefers the UTF-8 name", () => {
        expect(
            fileNameOf(
                "attachment; filename=users.xlsx; filename*=UTF-8''%D0%BA%D0%BE%D1%80%D0%B8%D1%81%D1%82%D1%83%D0%B2%D0%B0%D1%87%D1%96.xlsx",
            ),
        ).toBe("користувачі.xlsx");
    });

    it("reads a quoted and a plain name", () => {
        expect(fileNameOf('attachment; filename="users list.csv"')).toBe("users list.csv");
        expect(fileNameOf("attachment; filename=users.csv")).toBe("users.csv");
    });

    it("falls back to the plain name when the UTF-8 one is broken", () => {
        expect(fileNameOf("attachment; filename=users.csv; filename*=UTF-8''%E0%A4%A")).toBe("users.csv");
    });

    it("gives null without a name", () => {
        expect(fileNameOf(null)).toBeNull();
        expect(fileNameOf("attachment")).toBeNull();
        expect(fileNameOf('attachment; filename=""')).toBeNull();
    });
});

describe("downloadFile", () => {
    it("saves the file under the server's name and removes the temporary link", async () => {
        const saved = captureDownloads();
        const calls = mockApi("get", "/api/v1/user-exports", () => file("users-20261109.xlsx", "content"));

        const name = await downloadFile("/api/v1/user-exports?format=Xlsx", "users.xlsx");

        expect(name).toBe("users-20261109.xlsx");
        expect(calls[0]?.query.get("format")).toBe("Xlsx");
        expect(saved).toEqual([{ name: "users-20261109.xlsx", href: expect.stringMatching(/^blob:/) as unknown }]);
        expect(document.querySelector("a[download]")).toBeNull();
    });

    it("uses the given name when the server sends none", async () => {
        const saved = captureDownloads();
        mockApi("get", "/api/v1/user-exports", () => new HttpResponse("content"));

        expect(await downloadFile("/api/v1/user-exports?format=Csv", "users.csv")).toBe("users.csv");
        expect(saved.map(({ name }) => name)).toEqual(["users.csv"]);
    });

    it("fails with the server's problem and saves nothing", async () => {
        const saved = captureDownloads();
        mockApi("get", "/api/v1/user-exports", () => problem(403, "forbidden", "Недостатньо прав."));

        const failure = await downloadFile("/api/v1/user-exports?format=Csv", "users.csv").catch((error: unknown) => error);

        expect(failure).toBeInstanceOf(ApiError);
        expect(failure).toMatchObject({ status: 403, code: "forbidden", detail: "Недостатньо прав.", path: "/api/v1/user-exports" });
        expect(saved).toEqual([]);
    });

    it("fails without a message when the answer has no problem body", async () => {
        captureDownloads();
        mockApi("get", "/api/v1/user-exports", 502);

        await expect(downloadFile("/api/v1/user-exports", "users.csv")).rejects.toMatchObject({ status: 502, detail: "" });
    });
});
