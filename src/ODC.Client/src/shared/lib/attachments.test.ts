import { describe, expect, it } from "vitest";
import { fileAccept, isAllowedFile, maxFiles, maxFileSize, maxTotalSize } from "./attachments";

/**
 * A file with a name (the content does not matter for the allow-list).
 * @param name The file name.
 * @returns The file.
 */
function named(name: string): File {
    return new File(["x"], name);
}

/** Every extension the server allows (FileRules.ChatFileTypes). */
const allowed = [
    "pdf",
    "doc",
    "docx",
    "xls",
    "xlsx",
    "ppt",
    "pptx",
    "odt",
    "ods",
    "odp",
    "rtf",
    "txt",
    "csv",
    "md",
    "xml",
    "json",
    "p7s",
    "asics",
    "asice",
    "zip",
    "7z",
    "rar",
    "jpg",
    "jpeg",
    "png",
    "webp",
    "avif",
    "gif",
    "heic",
    "mp3",
    "m4a",
    "wav",
    "mp4",
    "mov",
    "webm",
];

describe("attachment limits", () => {
    it("allows 10 files of up to 25 MB and 50 MB in all", () => {
        expect(maxFiles).toBe(10);
        expect(maxFileSize).toBe(25 * 1024 * 1024);
        expect(maxTotalSize).toBe(50 * 1024 * 1024);
    });
});

describe("fileAccept", () => {
    it("lists every allowed extension with a dot, in order", () => {
        expect(fileAccept).toBe(allowed.map((extension) => `.${extension}`).join(","));
    });

    it.each(["exe", "svg", "html", "js"])("does not offer .%s", (extension) => {
        expect(fileAccept.split(",")).not.toContain(`.${extension}`);
    });
});

describe("isAllowedFile", () => {
    it.each(allowed)("accepts .%s", (extension) => {
        expect(isAllowedFile(named(`file.${extension}`))).toBe(true);
    });

    it("ignores the case of the extension", () => {
        expect(isAllowedFile(named("SCAN.PDF"))).toBe(true);
        expect(isAllowedFile(named("Photo.JpEg"))).toBe(true);
    });

    it("looks at the last extension only", () => {
        expect(isAllowedFile(named("report.exe.pdf"))).toBe(true);
        expect(isAllowedFile(named("report.pdf.exe"))).toBe(false);
    });

    it.each(["setup.exe", "image.svg", "page.html", "page.htm", "script.js", "macro.bat", "run.sh", "app.msi", "lib.dll"])(
        "refuses %s",
        (name) => {
            expect(isAllowedFile(named(name))).toBe(false);
        },
    );

    it("refuses a name without an extension, even one equal to an allowed extension", () => {
        expect(isAllowedFile(named("pdf"))).toBe(false);
        expect(isAllowedFile(named("README"))).toBe(false);
    });

    it("refuses a name ending with a dot", () => {
        expect(isAllowedFile(named("file."))).toBe(false);
    });
});
