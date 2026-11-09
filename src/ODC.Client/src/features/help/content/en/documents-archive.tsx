import { Trash2 } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Table } from "../../components/Manual";

/**
 * The manual page "documents-archive" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
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
                            After confirming “Delete the document permanently?”, the document is deleted together with its file. Message:
                            “Document deleted”.
                        </td>
                    </tr>
                </tbody>
            </Table>
            <p>
                A document with signatures cannot be deleted — only archived: “A document with signatures can't be deleted — it can only be
                archived.”
            </p>
        </>
    );
}
