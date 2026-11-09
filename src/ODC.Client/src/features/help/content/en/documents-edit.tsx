import { Steps, Table, Ui } from "../../components/Manual";

/**
 * The manual page "documents-edit" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The author can change a document with the <strong>“Draft”</strong> or <strong>“Rejected”</strong> status — for example, to
                fix a mistake pointed out by a signer.
            </p>
            <Steps>
                <li>
                    Open the document and click <Ui>Edit</Ui>. The “Edit document” page opens with the current data.
                </li>
                <li>
                    Replace the file if needed: in the “File” card, click <Ui>Replace file</Ui> and select a new one. The current file is
                    shown above the selection area.
                </li>
                <li>
                    Change the <Ui>Title</Ui>, <Ui>Document type</Ui>, <Ui>Unit</Ui> or <Ui>Description</Ui>.
                </li>
                <li>
                    Change the route: the signing order, new signers, the sequence with the <Ui>Up</Ui> / <Ui>Down</Ui> buttons, unneeded
                    signers with <Ui>Remove</Ui>.
                </li>
                <li>
                    Click <Ui>Save changes</Ui> (the document keeps its current status) or <Ui>Save and send for signing</Ui>.{" "}
                    <Ui>Cancel</Ui> returns to the document without changes.
                </li>
            </Steps>
            <h2>What happens to signatures already applied</h2>
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
                            <strong>All</strong> signatures are deleted: they were applied to the previous file. All signers will sign the
                            new file.
                        </td>
                    </tr>
                </tbody>
            </Table>
            <p>
                The editor warns you in advance: signers whose signatures will be kept are marked <Ui>Signature will be kept</Ui>, and if
                saving would delete signatures, a red warning with their count appears.
            </p>
            <p>
                The document's registration number doesn't change when it is edited. Every change is recorded in the audit log as “Document
                changed” (with the new file name and the number of revoked signatures).
            </p>
        </>
    );
}
