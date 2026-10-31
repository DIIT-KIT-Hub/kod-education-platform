import { Info, Trash2 } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Icon } from "@/shared/ui/Icon";
import { Chapter, Code, Ref, Steps, Table, Topic, Ui } from "../../components/Manual";

/**
 * Documents (English).
 * @returns The chapter.
 */
export function DocumentsChapter() {
    return (
        <Chapter id="documents">
            <Topic id="documents-list">
                <p>
                    Open <Ui>Documents</Ui> in the menu. This is the registry of all documents you have access to.
                </p>
                <h4>Tabs</h4>
                <ul>
                    <li>
                        <Ui>Awaiting my signature</Ui> — opens by default: documents where it is <strong>your turn</strong> to sign.
                    </li>
                    <li>
                        <Ui>Created by me</Ui> — documents you are the author of.
                    </li>
                    <li>
                        <Ui>I'm in the route</Ui> — documents where you are a signer (whether or not you have already signed).
                    </li>
                    <li>
                        <Ui>Signed by me</Ui> — documents you have already signed. Open a document card from here to download the file
                        together with its signatures.
                    </li>
                    <li>
                        <Ui>All available</Ui> — all documents you are allowed to see (see “
                        <Ref to="documents-visibility">Whose documents you see</Ref>”).
                    </li>
                </ul>
                <h4>Search and filters</h4>
                <ul>
                    <li>
                        Search box — by title or registration number; the list updates as soon as you stop typing (or at once with Enter).
                    </li>
                    <li>
                        Status filter — <Ui>All statuses</Ui> or one of the statuses: “Draft”, “In signing”, “Signed”, “Rejected”,
                        “Archived”.
                    </li>
                    <li>
                        Type filter — <Ui>All types</Ui> or: “Order”, “Directive”, “Memo”, “Application”, “Report”, “Minutes”, “Contract”,
                        “Certificate”, “Regulation / instruction”, “Letter”, “Act”, “Other document”.
                    </li>
                    <li>Filters are applied as soon as you select them.</li>
                </ul>
                <h4>Table columns</h4>
                <ul>
                    <li>
                        <strong>Document</strong> — the title, with the type and unit below it.
                    </li>
                    <li>
                        <strong>Number</strong> — the registration number.
                    </li>
                    <li>
                        <strong>Status</strong> — plus the <Ui>Your signature</Ui> badge if it's your turn now.
                    </li>
                    <li>
                        <strong>Signatures</strong> — how many of the required signatures have been applied (for example, 2/3).
                    </li>
                    <li>
                        <strong>Author</strong>, <strong>Created</strong>.
                    </li>
                    <li>
                        <strong>Actions</strong> — <Ui>Sign</Ui> if it's your turn, otherwise <Ui>Open</Ui>.
                    </li>
                </ul>
                <p>
                    Sort by clicking the “Document”, “Number”, “Status” and “Created” headers (clicking again reverses the order). By
                    default, the newest documents are at the top. If nothing is found, you'll see “No documents found” with a suggestion to
                    change the search or filters.
                </p>
            </Topic>
            <Topic id="documents-lifecycle">
                <Table>
                    <thead>
                        <tr>
                            <th>Status</th>
                            <th>What it means</th>
                            <th>What's next</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Draft</td>
                            <td>The document has been created but not sent for signing.</td>
                            <td>The author edits it, sends it for signing or deletes it.</td>
                        </tr>
                        <tr>
                            <td>In signing</td>
                            <td>The document is going through the signing route.</td>
                            <td>
                                Signers sign or decline. While there are no signatures yet, the author can withdraw the document back to
                                drafts. A document in signing cannot be edited.
                            </td>
                        </tr>
                        <tr>
                            <td>Signed</td>
                            <td>All signers have signed. The “Completed” date is filled in.</td>
                            <td>The document can no longer change. It can only be archived.</td>
                        </tr>
                        <tr>
                            <td>Rejected</td>
                            <td>One of the signers declined and gave a reason. The route is stopped.</td>
                            <td>
                                The author corrects the document (edits the file, details or route) and sends it again, or archives it. It
                                can be deleted if it has no signatures.
                            </td>
                        </tr>
                        <tr>
                            <td>Archived</td>
                            <td>The final state for completed documents.</td>
                            <td>A document cannot be restored from the archive.</td>
                        </tr>
                    </tbody>
                </Table>
                <p>Signer statuses in the route: “Awaiting signature”, “Signed”, “Declined”.</p>
                <p>
                    Each document automatically gets a <strong>registration number</strong> in the <Code>YYYY/NNNNNN</Code> format, for
                    example <Code>2026/000123</Code>.
                </p>
                <Alert tone="outline" icon={Info} title="When a document can be changed">
                    The author can edit a document while it is “Draft” or “Rejected”. A document that is in signing, signed or archived
                    cannot be changed — all signers sign the same file. For details, see “
                    <Ref to="documents-edit">Editing a draft or rejected document</Ref>”.
                </Alert>
            </Topic>
            <Topic id="documents-create">
                <p>
                    Click <Ui>New document</Ui> in the registry or on the home page. The page has three cards.
                </p>
                <h4>File</h4>
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
                <h4>Details</h4>
                <ul>
                    <li>
                        <Ui>Title</Ui> (required, up to 300 characters), for example “On approval of the vacation schedule”.
                    </li>
                    <li>
                        <Ui>Document type</Ui> (required), “Memo” by default.
                    </li>
                    <li>
                        <Ui>Unit</Ui> (required) — the unit on whose behalf the document is issued. The list contains the units of your
                        positions and the units nested in them; with the “Organization documents” permission — all units of your
                        organizations; with the “All documents” permission — all units of the cabinet. If there is only one such unit, it is
                        selected automatically.
                    </li>
                    <li>
                        <Ui>Description</Ui> (up to 4000 characters) — a short summary or a comment for the signers.
                    </li>
                </ul>
                <h4>Signing route</h4>
                <p>
                    Add signers — see “<Ref to="documents-route">Signing route</Ref>”.
                </p>
                <h4>Saving</h4>
                <ul>
                    <li>
                        <Ui>Create and send for signing</Ui> — the document immediately gets the “In signing” status, and it becomes the
                        first signers' turn. The button is enabled only when at least one signer has been added. Message: “Document created
                        and sent for signing”.
                    </li>
                    <li>
                        <Ui>Save draft</Ui> — the document is saved with the “Draft” status; you can send it later from its page. Message:
                        “Draft saved”.
                    </li>
                </ul>
                <p>After saving, the document page opens.</p>
                <h4>Possible errors</h4>
                <ul>
                    <li>“Select the document file.”, “This file type is not supported.”, “The file size cannot exceed 25 MB.”</li>
                    <li>“Enter the document title.”, “Select a unit.”, “Select a unit you belong to.”</li>
                    <li>“Some signers are inactive or don't exist.” — one of the signers has been blocked; remove them from the route.</li>
                </ul>
            </Topic>
            <Topic id="documents-route">
                <Steps>
                    <li>
                        Choose the signing order: <Ui>Sequential</Ui> (one after another, the default) or <Ui>Parallel</Ui> (in any order).
                    </li>
                    <li>
                        In the <Ui>Find an employee</Ui> box, start typing a last name, first name or email (at least 2 characters). Up to
                        10 active employees with their primary positions appear.
                    </li>
                    <li>Click an employee — they are added to the end of the route with a sequence number.</li>
                    <li>
                        Change the order with the <Ui>Up</Ui> and <Ui>Down</Ui> buttons, remove signers with <Ui>Remove</Ui>.
                    </li>
                </Steps>
                <ul>
                    <li>
                        <strong>Sequential:</strong> only the first signer in line who hasn't signed yet can sign. The next one sees the
                        document under “Awaiting my signature” after the previous signer has signed. Suitable for bottom-up approval
                        (specialist → head of division → director of department → head of organization).
                    </li>
                    <li>
                        <strong>Parallel:</strong> all signers can sign at the same time in any order. Suitable for documents signed by a
                        committee.
                    </li>
                    <li>A maximum of 50 signers, each person only once.</li>
                    <li>A document without signers can only be saved as a draft.</li>
                </ul>
            </Topic>
            <Topic id="documents-card">
                <p>
                    Open a document from the registry (its title or <Ui>Open</Ui>). At the top are the title, status, type and number, and
                    on the right — the action buttons available to you right now.
                </p>
                <ul>
                    <li>
                        <strong>Document file</strong> — the file name, size and type, the <Ui>Download</Ui> button and the{" "}
                        <strong>SHA-256</strong> line — the file's “fingerprint”, used to verify that the file hasn't been changed.
                    </li>
                    <li>
                        <strong>Details</strong> — registration number, type, unit (a link to its page), author, signing order, the
                        “Created”, “Sent for signing” and “Completed” dates, and the description.
                    </li>
                    <li>
                        <strong>Signatures</strong> — all applied QES: the full name from the certificate; signature format, level and type;
                        date; provider, certificate number, individual tax number / EDRPOU code; a download button: <Ui>Signature file</Ui>,
                        for PAdES <Ui>Signed PDF</Ui>, and for cumulative signatures <Ui>Version after this signature</Ui>. If there are no
                        signatures yet — “No signatures yet”.
                    </li>
                    <li>
                        <strong>Signing route</strong> — a timeline: step number, full name, position and unit, status and decision date.
                        The reason for declining is highlighted in red.
                    </li>
                </ul>
                <p>
                    If it's your turn now, the hint “The document is awaiting your signature” appears at the top. The author of a draft or
                    rejected document sees the <Ui>Edit</Ui> button, and for a rejected one also the “Document rejected” hint with advice on
                    what to do next.
                </p>
                <p>
                    If the document doesn't exist or you don't have access to it, you'll see “Document not found” with a{" "}
                    <Ui>To registry</Ui> button.
                </p>
            </Topic>
            <Topic id="documents-edit">
                <p>
                    The author can change a document with the <strong>“Draft”</strong> or <strong>“Rejected”</strong> status — for example,
                    to fix a mistake pointed out by a signer.
                </p>
                <Steps>
                    <li>
                        Open the document and click <Ui>Edit</Ui>. The “Edit document” page opens with the current data.
                    </li>
                    <li>
                        Replace the file if needed: in the “File” card, click <Ui>Replace file</Ui> and select a new one. The current file
                        is shown above the selection area.
                    </li>
                    <li>
                        Change the <Ui>Title</Ui>, <Ui>Document type</Ui>, <Ui>Unit</Ui> or <Ui>Description</Ui>.
                    </li>
                    <li>
                        Change the route: the signing order, new signers, the sequence with the <Ui>Up</Ui> / <Ui>Down</Ui> buttons,
                        unneeded signers with <Ui>Remove</Ui>.
                    </li>
                    <li>
                        Click <Ui>Save changes</Ui> (the document keeps its current status) or <Ui>Save and send for signing</Ui>.{" "}
                        <Ui>Cancel</Ui> returns to the document without changes.
                    </li>
                </Steps>
                <h4>What happens to signatures already applied</h4>
                <p>A rejected document may have signatures from those who signed before it was declined. When editing:</p>
                <Table>
                    <thead>
                        <tr>
                            <th>What changed</th>
                            <th>Signatures</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Title, type, unit, description, order of signers</td>
                            <td>Kept — these details are not part of the signed file.</td>
                        </tr>
                        <tr>
                            <td>A signer was removed from the route</td>
                            <td>Their signature is deleted.</td>
                        </tr>
                        <tr>
                            <td>The file was replaced</td>
                            <td>
                                <strong>All</strong> signatures are deleted: they were applied to the previous file. All signers will sign
                                the new file.
                            </td>
                        </tr>
                    </tbody>
                </Table>
                <p>
                    The editor warns you in advance: signers whose signatures will be kept are marked <Ui>Signature will be kept</Ui>, and
                    if saving would delete signatures, a red warning with their count appears.
                </p>
                <p>
                    The document's registration number doesn't change when it is edited. Every change is recorded in the audit log as
                    “Document changed” (with the new file name and the number of revoked signatures).
                </p>
            </Topic>
            <Topic id="documents-sign">
                <p>You can sign a document when it's your turn in the route and you have the “Sign documents” permission.</p>
                <Steps>
                    <li>
                        Open the document (the <Ui>Awaiting my signature</Ui> tab or the <Ui>Sign</Ui> button in the registry) and review
                        the file carefully — the <Ui>Download</Ui> button in the “Document file” card.
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
                        PDF). The first signer chooses the format and type; later signers sign in the same format, so these fields are
                        already fixed for them. Click <Ui>Next</Ui>.
                    </li>
                    <li>
                        <strong>The “Key and signature” step.</strong> Select the key file, enter the password, click <Ui>Read key</Ui>,
                        check the owner's details and click <Ui>Sign</Ui>. More about keys — “
                        <Ref to="signing-keys">Keys and providers</Ref>”.
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
                    <strong>All signatures in one file.</strong> Each next signer adds their signature to the file the previous signers
                    signed: CAdES gives one <Code>.p7s</Code> with every signature, PAdES one PDF with every signature, ASiC one container.
                    The “Document” step shows how many signatures the file already has. XAdES cannot do this: its signatures are
                    independent, each over the same original.
                </p>
                <p>
                    <strong>Taking turns in a parallel route.</strong> While one signer is signing the document (up to 10 minutes), the
                    others see “… is signing the document right now” and wait — the window checks again every few seconds. This keeps two
                    signatures from landing on the same version. If someone signed the document in the meantime, you'll see “Another signer
                    signed the document while you were signing…”: the latest version is loaded, so just click <Ui>Sign</Ui> again.
                </p>
                <p>
                    Closing the signing window (the close button or a click outside the window) releases the key — the next time you open
                    it, you'll need to read the key again.
                </p>
                <p>
                    Possible errors: “The document is not awaiting your signature right now.” (it isn't your turn yet or the document has
                    changed — refresh the page), “The PAdES format is available only for PDF documents.”, “The data was just changed by
                    another user. Refresh the page and try again.”, as well as key errors (see “
                    <Ref to="signing-problems">Error messages</Ref>”).
                </p>
            </Topic>
            <Topic id="documents-reject">
                <p>
                    If the document contains a mistake or you disagree with it, decline to sign — the author will see the reason and can
                    prepare a corrected document.
                </p>
                <Steps>
                    <li>
                        Open the document and click <Ui>Decline</Ui>.
                    </li>
                    <li>
                        In the “Decline signing” window, enter the <Ui>Reason</Ui> (required, up to 1000 characters) — specifically what
                        needs to be fixed.
                    </li>
                    <li>
                        Click <Ui>Decline</Ui>. “Decline recorded” appears.
                    </li>
                </Steps>
                <p>
                    The very first decline stops the route: the document gets the “Rejected” status, and the other signers can no longer
                    sign it. The reason is shown in red in the route. The author will correct the document and send it again — then it will
                    reappear in your queue.
                </p>
            </Topic>
            <Topic id="documents-author">
                <Table>
                    <thead>
                        <tr>
                            <th>Button</th>
                            <th>When available</th>
                            <th>What it does</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Send for signing</td>
                            <td>The document is “Draft” or “Rejected” and has signers.</td>
                            <td>
                                After confirming “Send the document for signing?”, the document gets the “In signing” status. Message:
                                “Document sent for signing”.
                            </td>
                        </tr>
                        <tr>
                            <td>Withdraw</td>
                            <td>The document is “In signing” and nobody has signed it yet.</td>
                            <td>
                                After confirming “Withdraw the document to drafts?”, the document returns to “Draft”. Message: “Document
                                returned to drafts”.
                            </td>
                        </tr>
                    </tbody>
                </Table>
                <h4>Resending a rejected document</h4>
                <Steps>
                    <li>Read the reason for declining in the signing route.</li>
                    <li>
                        Correct the document with the <Ui>Edit</Ui> button (see “<Ref to="documents-edit">Editing</Ref>”) and click{" "}
                        <Ui>Save and send for signing</Ui>. If no corrections are needed, just click <Ui>Send for signing</Ui> on the
                        document page.
                    </li>
                </Steps>
                <ul>
                    <li>
                        Those who have already signed the <strong>current file</strong> don't sign again: their signatures are kept, and
                        their route step stays “Signed”.
                    </li>
                    <li>
                        The other steps (including the one who declined) return to “Awaiting signature”; the reasons for declining are
                        cleared.
                    </li>
                    <li>With sequential order, the first signer who hasn't signed yet becomes first in line.</li>
                    <li>
                        If after the changes all signers in the route have already signed the current file, the document immediately gets
                        the “Signed” status.
                    </li>
                </ul>
                <p>Actions available only to the author show other users “Only the document author can perform this action.”</p>
            </Topic>
            <Topic id="documents-files">
                <ul>
                    <li>
                        <Ui>Download</Ui> in the “Document file” card — the original file without signatures.
                    </li>
                    <li>
                        <Ui>Document with all signatures</Ui> in the same card — one file that holds the document and every signature
                        applied to it (CAdES, PAdES, ASiC). Send this file to counterparties and verify it on the “Signature verification”
                        page or at czo.gov.ua.
                    </li>
                    <li>
                        <Ui>Version after this signature</Ui> in the “Signatures” card — the file as it was right after each signature (for
                        XAdES, each signer's <Ui>Signature file</Ui>). Files are named after the document: a signed PDF keeps the original
                        name, containers add their extension (<Code>contract.pdf.p7s</Code>, <Code>.asics</Code>, <Code>.asice</Code>, XAdES
                        — <Code>.xml</Code>).
                    </li>
                    <li>
                        To verify a signature, open <Ui>Signing</Ui> → <Ui>Verify</Ui> and add the signature file (for a detached signature,
                        together with the original). See “<Ref to="signing-verify">Verifying a signature</Ref>”.
                    </li>
                </ul>
                <p>
                    Documents and signatures are stored on the server without public access and are served only to users who are allowed to
                    see the document.
                </p>
            </Topic>
            <Topic id="documents-archive">
                <Table>
                    <thead>
                        <tr>
                            <th>Button</th>
                            <th>Who and when</th>
                            <th>What it does</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Archive</td>
                            <td>The author or a user with the “Manage documents” permission; the document is “Signed” or “Rejected”.</td>
                            <td>
                                After confirming “Move the document to the archive?”, the status becomes “Archived”. A document cannot be
                                restored from the archive. Message: “Document moved to the archive”.
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <Icon icon={Trash2} /> (delete)
                            </td>
                            <td>The author or “Manage documents”; the document has no signatures and isn't archived.</td>
                            <td>
                                After confirming “Delete the document permanently?”, the document is deleted together with its file.
                                Message: “Document deleted”.
                            </td>
                        </tr>
                    </tbody>
                </Table>
                <p>
                    A document with signatures cannot be deleted — only archived: “A document with signatures can't be deleted — it can only
                    be archived.”
                </p>
            </Topic>
            <Topic id="documents-visibility">
                <ul>
                    <li>
                        You <strong>always</strong> see documents you are the author of and documents whose route you are in.
                    </li>
                    <li>
                        With the <strong>“Unit documents”</strong> permission — also documents of the units of your positions and all nested
                        units (for example, a department director sees the documents of the department and all its divisions and sections).
                    </li>
                    <li>
                        With the <strong>“Organization documents”</strong> permission — documents of the entire organization where you hold
                        a position, with all its units (head of organization, deputies, records clerks). Documents of other organizations in
                        the cabinet are not visible.
                    </li>
                    <li>
                        With the <strong>“All documents”</strong> permission — documents of all organizations in the cabinet (usually only
                        for system administrators and auditors).
                    </li>
                </ul>
                <p>A document you can't see is not shown in the registry, and a direct link to it shows “Document not found”.</p>
            </Topic>
        </Chapter>
    );
}
