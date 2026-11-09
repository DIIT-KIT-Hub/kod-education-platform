import { Code, Ref, Ui } from "../../components/Manual";

/**
 * The manual page "documents-files" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    <Ui>Download</Ui> in the “Document file” card — the original file without signatures.
                </li>
                <li>
                    <Ui>Document with all signatures</Ui> in the same card — one file that holds the document and every signature applied to
                    it (CAdES, PAdES, ASiC). Send this file to counterparties and verify it on the “Signature verification” page or at
                    czo.gov.ua.
                </li>
                <li>
                    <Ui>Version after this signature</Ui> in the “Signatures” card — the file as it was right after each signature (for
                    XAdES, each signer's <Ui>Signature file</Ui>). Files are named after the document: a signed PDF keeps the original name,
                    containers add their extension (<Code>contract.pdf.p7s</Code>, <Code>.asics</Code>, <Code>.asice</Code>, XAdES —{" "}
                    <Code>.xml</Code>).
                </li>
                <li>
                    To verify a signature, open <Ui>Signing</Ui> → <Ui>Verify</Ui> and add the signature file (for a detached signature,
                    together with the original). See “<Ref to="signing-verify">Verifying a signature</Ref>”.
                </li>
            </ul>
            <p>
                Documents and signatures are stored on the server without public access and are served only to users who are allowed to see
                the document.
            </p>
        </>
    );
}
