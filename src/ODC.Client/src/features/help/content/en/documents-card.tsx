import documentsCardImage from "../../images/en/documents-card.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "documents-card" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open a document from the registry (its title or <Ui>Open</Ui>). At the top are the title, status, type and number, and on
                the right — the action buttons available to you right now.
            </p>
            <Figure
                src={documentsCardImage}
                alt="The card of a document in signing"
                caption="The document card: actions at the top, the file and the details on the left, the signing route on the right."
            />
            <ul>
                <li>
                    <strong>Document file</strong> — the file name, size and type, the <Ui>Download</Ui> button and the{" "}
                    <strong>SHA-256</strong> line — the file's “fingerprint”, used to verify that the file hasn't been changed.
                </li>
                <li>
                    <strong>Details</strong> — registration number, type, unit (a link to its page), author, signing order, the “Created”,
                    “Sent for signing” and “Completed” dates, and the description.
                </li>
                <li>
                    <strong>Signatures</strong> — all applied QES: the full name from the certificate; signature format, level and type;
                    date; provider, certificate number, individual tax number / EDRPOU code; a download button: <Ui>Signature file</Ui>, for
                    PAdES <Ui>Signed PDF</Ui>, and for cumulative signatures <Ui>Version after this signature</Ui>. If there are no
                    signatures yet — “No signatures yet”.
                </li>
                <li>
                    <strong>Signing route</strong> — a timeline: step number, full name, position and unit, status and decision date. The
                    reason for declining is highlighted in red.
                </li>
            </ul>
            <p>
                If it's your turn now, the hint “The document is awaiting your signature” appears at the top. The author of a draft or
                rejected document sees the <Ui>Edit</Ui> button, and for a rejected one also the “Document rejected” hint with advice on
                what to do next.
            </p>
            <p>
                If the document doesn't exist or you don't have access to it, you'll see “Document not found” with a <Ui>To registry</Ui>{" "}
                button.
            </p>
        </>
    );
}
