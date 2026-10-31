import { describe, expect, it } from "vitest";
import { availableFormats, effectivePackaging, signatureFileName } from "./formats";

describe("availableFormats", () => {
    it("offers PAdES only for PDF files", () => {
        expect(availableFormats(["order.pdf"], null)).toEqual(["CAdES", "XAdES", "PAdES", "AsicS", "AsicE"]);
        expect(availableFormats(["order.docx"], null)).toEqual(["CAdES", "XAdES", "AsicS", "AsicE"]);
    });

    it("offers only multi-file formats for several files", () => {
        expect(availableFormats(["a.pdf", "b.pdf"], null)).toEqual(["XAdES", "AsicE"]);
    });

    it("keeps the format of the first signature", () => {
        expect(availableFormats(["order.pdf"], { format: "CAdES", packaging: "Enveloped" })).toEqual(["CAdES"]);
    });
});

describe("signature file names", () => {
    it.each([
        ["CAdES", ["order.pdf"], "order.pdf.p7s"],
        ["PAdES", ["order.pdf"], "order.pdf"],
        ["XAdES", ["a.txt", "b.txt"], "signature.xml"],
        ["AsicS", ["order.docx"], "order.docx.asics"],
        ["AsicE", ["a.txt", "b.txt"], "a.asice"],
    ] as const)("%s of %j is %s", (format, names, expected) => {
        expect(signatureFileName(format, names)).toBe(expected);
    });

    it("signs containers enveloped whatever was chosen", () => {
        expect(effectivePackaging("AsicE", "Detached")).toBe("Enveloped");
        expect(effectivePackaging("CAdES", "Detached")).toBe("Detached");
    });
});
