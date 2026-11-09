import { Ref, Steps, Table, Ui } from "../../components/Manual";

/**
 * The manual page "documents-author" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
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
                            After confirming “Send the document for signing?”, the document gets the “In signing” status. Message: “Document
                            sent for signing”.
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
            <h2>Resending a rejected document</h2>
            <Steps>
                <li>Read the reason for declining in the signing route.</li>
                <li>
                    Correct the document with the <Ui>Edit</Ui> button (see “<Ref to="documents-edit">Editing</Ref>”) and click{" "}
                    <Ui>Save and send for signing</Ui>. If no corrections are needed, just click <Ui>Send for signing</Ui> on the document
                    page.
                </li>
            </Steps>
            <ul>
                <li>
                    Those who have already signed the <strong>current file</strong> don't sign again: their signatures are kept, and their
                    route step stays “Signed”.
                </li>
                <li>
                    The other steps (including the one who declined) return to “Awaiting signature”; the reasons for declining are cleared.
                </li>
                <li>With sequential order, the first signer who hasn't signed yet becomes first in line.</li>
                <li>
                    If after the changes all signers in the route have already signed the current file, the document immediately gets the
                    “Signed” status.
                </li>
            </ul>
            <p>Actions available only to the author show other users “Only the document author can perform this action.”</p>
        </>
    );
}
