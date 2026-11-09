import documentsListImage from "../../images/en/documents-list.webp";
import { Figure, Ref, Ui } from "../../components/Manual";

/**
 * The manual page "documents-list" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>Documents</Ui> in the menu. This is the registry of all documents you have access to.
            </p>
            <Figure
                src={documentsListImage}
                alt="The document register with tabs and filters"
                caption="The document register: tabs at the top, search and filters below."
            />
            <h2>Tabs</h2>
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
                    <Ui>Signed by me</Ui> — documents you have already signed. Open a document card from here to download the file together
                    with its signatures.
                </li>
                <li>
                    <Ui>All available</Ui> — all documents you are allowed to see (see “
                    <Ref to="documents-visibility">Whose documents you see</Ref>”).
                </li>
            </ul>
            <h2>Search and filters</h2>
            <ul>
                <li>Search box — by title or registration number; the list updates as soon as you stop typing (or at once with Enter).</li>
                <li>
                    Status filter — <Ui>All statuses</Ui> or one of the statuses: “Draft”, “In signing”, “Signed”, “Rejected”, “Archived”.
                </li>
                <li>
                    Type filter — <Ui>All types</Ui> or: “Order”, “Directive”, “Memo”, “Application”, “Report”, “Minutes”, “Contract”,
                    “Certificate”, “Regulation / instruction”, “Letter”, “Act”, “Other document”.
                </li>
                <li>Filters are applied as soon as you select them.</li>
            </ul>
            <h2>Table columns</h2>
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
                Sort by clicking the “Document”, “Number”, “Status” and “Created” headers (clicking again reverses the order). By default,
                the newest documents are at the top. If nothing is found, you'll see “No documents found” with a suggestion to change the
                search or filters.
            </p>
        </>
    );
}
