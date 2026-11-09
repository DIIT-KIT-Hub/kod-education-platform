import documentsSignImage from "../../images/en/documents-sign.webp";
import { Code, Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "documents-sign" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>You can sign a document when it's your turn in the route and you have the “Sign documents” permission.</p>
            <Figure src={documentsSignImage} alt="The signing window" caption="The signing window opens with the “Sign” button." />
            <Steps>
                <li>
                    Open the document (the <Ui>Awaiting my signature</Ui> tab or the <Ui>Sign</Ui> button in the registry) and review the
                    file carefully — the <Ui>Download</Ui> button in the “Document file” card.
                </li>
                <li>
                    Click <Ui>Sign</Ui> at the top of the page. The “Sign document” window opens with the number and file name.
                </li>
                <li>
                    <strong>The “Document” step.</strong> The browser downloads the file and compares its SHA-256 fingerprint with the
                    registered one. If everything is fine — “Integrity confirmed”. If the fingerprint doesn't match — “The file checksum
                    doesn't match the registered one. Signing is not allowed.” (contact your administrator). Click <Ui>Next</Ui>.
                </li>
                <li>
                    <strong>The “Parameters” step.</strong> Choose the format and level (CAdES, B-LT recommended; PAdES is available for
                    PDF). The first signer chooses the format and type; later signers sign in the same format, so these fields are already
                    fixed for them. Click <Ui>Next</Ui>.
                </li>
                <li>
                    <strong>The “Key and signature” step.</strong> Select the key file, enter the password, click <Ui>Read key</Ui>, check
                    the owner's details and click <Ui>Sign</Ui>. More about keys — “<Ref to="signing-keys">Keys and providers</Ref>”.
                </li>
                <li>
                    “Document signed” appears — “Signature saved. The page will refresh automatically.” The signature appears in the
                    “Signatures” card, and your step in the route becomes “Signed”.
                </li>
            </Steps>
            <p>
                If you are the last signer, the document gets the “Signed” status. With sequential order, the next signer receives the
                document in their queue.
            </p>
            <p>
                <strong>All signatures in one file.</strong> Each next signer adds their signature to the file the previous signers signed:
                CAdES gives one <Code>.p7s</Code> with every signature, PAdES one PDF with every signature, ASiC one container. The
                “Document” step shows how many signatures the file already has. XAdES cannot do this: its signatures are independent, each
                over the same original.
            </p>
            <p>
                <strong>Taking turns in a parallel route.</strong> While one signer is signing the document (up to 10 minutes), the others
                see “… is signing the document right now” and wait — the window checks again every few seconds. This keeps two signatures
                from landing on the same version. If someone signed the document in the meantime, you'll see “Another signer signed the
                document while you were signing…”: the latest version is loaded, so just click <Ui>Sign</Ui> again.
            </p>
            <p>
                Closing the signing window (the close button or a click outside the window) releases the key — the next time you open it,
                you'll need to read the key again.
            </p>
            <p>
                Possible errors: “The document is not awaiting your signature right now.” (it isn't your turn yet or the document has
                changed — refresh the page), “The PAdES format is available only for PDF documents.”, “The data was just changed by another
                user. Refresh the page and try again.”, as well as key errors (see “<Ref to="signing-problems">Error messages</Ref>”).
            </p>
        </>
    );
}
