/*
 * The files a chat or a request accepts (the server checks them again: FileRules.ChatFileTypes): an allow-list without
 * executables, scripts, HTML or SVG, 10 files of up to 25 MB, 50 MB in all.
 */

/**
 * The allowed file types; every other file is refused before uploading.
 */
const allowedExtensions = new Set([
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
]);

/**
 * The most files of one message.
 */
export const maxFiles = 10;

/**
 * The largest file (25 MB).
 */
export const maxFileSize = 25 * 1024 * 1024;

/**
 * The largest total size of the files of one message (50 MB).
 */
export const maxTotalSize = 50 * 1024 * 1024;

/**
 * The value of the file input's accept attribute.
 */
export const fileAccept = [...allowedExtensions].map((extension) => `.${extension}`).join(",");

/**
 * Checks whether a file type is allowed.
 * @param file The file.
 * @returns The answer.
 */
export function isAllowedFile(file: File): boolean {
    const extension = file.name.split(".").pop()?.toLowerCase() ?? "";
    return file.name.includes(".") && allowedExtensions.has(extension);
}
