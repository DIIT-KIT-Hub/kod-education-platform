import { Info } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Code, Ref, Table } from "../../components/Manual";

/**
 * The manual page "documents-lifecycle" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
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
                            Signers sign or decline. While there are no signatures yet, the author can withdraw the document back to drafts.
                            A document in signing cannot be edited.
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
                            The author corrects the document (edits the file, details or route) and sends it again, or archives it. It can
                            be deleted if it has no signatures.
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
                Each document automatically gets a <strong>registration number</strong> in the <Code>YYYY/NNNNNN</Code> format, for example{" "}
                <Code>2026/000123</Code>.
            </p>
            <Alert tone="outline" icon={Info} title="When a document can be changed">
                The author can edit a document while it is “Draft” or “Rejected”. A document that is in signing, signed or archived cannot
                be changed — all signers sign the same file. For details, see “
                <Ref to="documents-edit">Editing a draft or rejected document</Ref>”.
            </Alert>
        </>
    );
}
