import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import type { SignFormat, SignLevel, SignPackaging } from "./client";

/**
 * A signature format of the library.
 */
export interface FormatInfo {
    readonly id: SignFormat;
    readonly title: string;
    readonly hint: MessageDescriptor;
    /** Whether the format has a packaging choice (CAdES, XAdES). */
    readonly packaging: boolean;
    /** Whether it signs several files at once. */
    readonly multiple: boolean;
    /** Whether it signs PDF files only. */
    readonly pdf: boolean;
}

/**
 * The formats in the order they are offered.
 */
export const formats: readonly FormatInfo[] = [
    { id: "CAdES", title: "CAdES", hint: msg`*.p7s · будь-який файл`, packaging: true, multiple: false, pdf: false },
    { id: "XAdES", title: "XAdES", hint: msg`*.xml · один або кілька файлів`, packaging: true, multiple: true, pdf: false },
    { id: "PAdES", title: "PAdES", hint: msg`підписаний PDF`, packaging: false, multiple: false, pdf: true },
    { id: "AsicS", title: "ASiC-S", hint: msg`контейнер з 1 файлом`, packaging: false, multiple: false, pdf: false },
    { id: "AsicE", title: "ASiC-E", hint: msg`контейнер з кількох файлів`, packaging: false, multiple: true, pdf: false },
];

/**
 * The packagings of CAdES and XAdES.
 */
export const packagings: readonly { readonly id: SignPackaging; readonly title: MessageDescriptor; readonly hint: MessageDescriptor }[] = [
    { id: "Detached", title: msg`Окремо від даних`, hint: msg`файл підпису поруч із документом` },
    { id: "Enveloped", title: msg`Разом з даними`, hint: msg`дані всередині підпису` },
];

/**
 * The signature levels (ETSI baseline profiles).
 */
export const levels: readonly { readonly id: SignLevel; readonly title: MessageDescriptor }[] = [
    { id: "B-B", title: msg`B-B — базовий` },
    { id: "B-T", title: msg`B-T — з позначкою часу` },
    { id: "B-LT", title: msg`B-LT — з даними для довготривалої перевірки` },
    { id: "B-LTA", title: msg`B-LTA — архівний` },
];

/**
 * The format and packaging the first signer of a document chose; the next signatures must use them.
 */
export interface FixedParameters {
    readonly format: SignFormat;
    readonly packaging: SignPackaging;
}

/**
 * The formats available for the files: PAdES only when every file is a PDF, single-file formats only for one file;
 * only the fixed format when the document already has signatures.
 * @param names The file names.
 * @param fixed The format fixed by the first signature.
 * @returns The available format identifiers.
 */
export function availableFormats(names: readonly string[], fixed: FixedParameters | null): SignFormat[] {
    if (fixed) {
        return [fixed.format];
    }

    const allPdf = names.length > 0 && names.every((name) => /\.pdf$/i.test(name));
    return formats.filter((format) => (!format.pdf || allPdf) && (format.multiple || names.length <= 1)).map((format) => format.id);
}

/**
 * The packaging that goes with a format: formats without a choice are always enveloped.
 * @param format The format.
 * @param packaging The chosen packaging.
 * @returns The packaging to sign with.
 */
export function effectivePackaging(format: SignFormat, packaging: SignPackaging): SignPackaging {
    return formats.find((info) => info.id === format)?.packaging ? packaging : "Enveloped";
}

/**
 * Suggests the file name of a signature: a signed PDF keeps the document's name, containers add their extension (the
 * same rule as the server's SignedFileName).
 * @param format The format.
 * @param names The signed file names.
 * @returns The file name.
 */
export function signatureFileName(format: SignFormat, names: readonly string[]): string {
    const first = names[0] ?? "document";
    const base = first.replace(/\.[^.]+$/, "");
    switch (format) {
        case "XAdES":
            return names.length === 1 ? `${first}.xml` : "signature.xml";
        case "PAdES":
            return `${base}.pdf`;
        case "AsicS":
            return `${first}.asics`;
        case "AsicE":
            return `${base}.asice`;
        default:
            return `${first}.p7s`;
    }
}

/** The largest file the library signs in the browser (matches EU_MAX_DATA_SIZE_MB of the worker). */
export const maxDataBytes = 25 * 1024 * 1024;

/** Extensions of file keys. */
export const keyAccept = ".dat,.pfx,.p12,.pk8,.jks,.zs2,.key";

/**
 * Computes the SHA-256 digest of bytes as lowercase hex.
 * @param buffer The data.
 * @returns The digest.
 */
export async function sha256(buffer: ArrayBuffer): Promise<string> {
    const digest = await crypto.subtle.digest("SHA-256", buffer);
    return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, "0")).join("");
}
