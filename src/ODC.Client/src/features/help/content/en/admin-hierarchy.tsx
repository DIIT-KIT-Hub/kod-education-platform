import { Ref } from "../../components/Manual";

/**
 * The manual page "admin-hierarchy" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>To prevent any administrator from expanding their own permissions, the following rules apply:</p>
            <ul>
                <li>
                    Administrative permissions apply only in the organizations of your positions (see “
                    <Ref to="admin-scope">Administration within organizations</Ref>”).
                </li>
                <li>
                    You can manage only accounts whose permissions you all have too. Otherwise: “This account has more permissions than you,
                    so you can't manage it.” The same rule applies to import.
                </li>
                <li>
                    Without the “Manage roles” permission you can't assign a role that grants more permissions than you have, nor the system
                    administrator role.
                </li>
                <li>Individual permissions can be changed only by holders of the “Manage roles” permission.</li>
                <li>You can't block your own account.</li>
                <li>Every administrative action is recorded in the audit log.</li>
            </ul>
        </>
    );
}
