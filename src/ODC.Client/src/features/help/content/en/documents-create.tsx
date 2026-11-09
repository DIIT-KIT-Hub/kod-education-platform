import documentsNewImage from "../../images/en/documents-new.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "documents-create" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Click <Ui>New document</Ui> in the registry or on the home page. The page has three cards.
            </p>
            <Figure
                src={documentsNewImage}
                alt="The new document form"
                caption="A new document: the file, the details and the signing route."
            />
            <h2>File</h2>
            <Steps>
                <li>
                    Drag a file into the <Ui>Drag a file here or click to select</Ui> area, or click it.
                </li>
                <li>
                    Allowed types: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, ODT, ODS, RTF, TXT, XML, JPG, JPEG, PNG, ZIP; size — up to{" "}
                    <strong>25 MB</strong>. If the document will be signed in PAdES format (signature inside the PDF), upload a PDF.
                </li>
                <li>If you haven't entered a title yet, it is filled in from the file name.</li>
            </Steps>
            <h2>Details</h2>
            <ul>
                <li>
                    <Ui>Title</Ui> (required, up to 300 characters), for example “On approval of the vacation schedule”.
                </li>
                <li>
                    <Ui>Document type</Ui> (required), “Memo” by default.
                </li>
                <li>
                    <Ui>Unit</Ui> (required) — the unit on whose behalf the document is issued. The list contains the units of your
                    positions and the units nested in them; with the “Organization documents” permission — all units of your organizations;
                    with the “All documents” permission — all units of the cabinet. If there is only one such unit, it is selected
                    automatically.
                </li>
                <li>
                    <Ui>Description</Ui> (up to 4000 characters) — a short summary or a comment for the signers.
                </li>
            </ul>
            <h2>Signing route</h2>
            <p>
                Add signers — see “<Ref to="documents-route">Signing route</Ref>”.
            </p>
            <h2>Saving</h2>
            <ul>
                <li>
                    <Ui>Create and send for signing</Ui> — the document immediately gets the “In signing” status, and it becomes the first
                    signers' turn. The button is enabled only when at least one signer has been added. Message: “Document created and sent
                    for signing”.
                </li>
                <li>
                    <Ui>Save draft</Ui> — the document is saved with the “Draft” status; you can send it later from its page. Message:
                    “Draft saved”.
                </li>
            </ul>
            <p>After saving, the document page opens.</p>
            <h2>Possible errors</h2>
            <ul>
                <li>“Select the document file.”, “This file type is not supported.”, “The file size cannot exceed 25 MB.”</li>
                <li>“Enter the document title.”, “Select a unit.”, “Select a unit you belong to.”</li>
                <li>“Some signers are inactive or don't exist.” — one of the signers has been blocked; remove them from the route.</li>
            </ul>
        </>
    );
}
