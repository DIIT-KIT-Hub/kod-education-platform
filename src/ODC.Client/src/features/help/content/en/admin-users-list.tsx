import adminUsersImage from "../../images/en/admin-users.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "admin-users-list" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Users</Ui>. Self-registration is disabled — all accounts are created here or by import. The
                list shows the accounts of your organizations (an administrator of all organizations sees the whole cabinet).
            </p>
            <Figure src={adminUsersImage} alt="The user list" caption="Users: search, filters by state, unit and role, export." />
            <ul>
                <li>
                    Search by full name or email; filters: status (<Ui>All statuses</Ui> / <Ui>Active</Ui> / <Ui>Blocked</Ui> /{" "}
                    <Ui>Awaiting activation</Ui>), <Ui>All units</Ui> (the selected unit together with all nested ones; select an
                    organization to see all of its people), <Ui>All roles</Ui>.
                </li>
                <li>
                    Columns: <strong>User</strong> (full name and email), <strong>Position</strong> (primary position and unit; for people
                    in several organizations — the position in your organization), <strong>Status</strong>, <strong>Last sign-in</strong>,{" "}
                    <Ui>Open</Ui>.
                </li>
                <li>
                    Statuses: “Active”, “Blocked” (by an administrator), “Temporarily locked” (after failed sign-in attempts), “Awaiting
                    activation” — the user hasn't confirmed the email and chosen a password yet, and also “No 2FA” — the user hasn't set up
                    an authenticator yet.
                </li>
                <li>
                    Buttons: <Ui>XLSX</Ui> / <Ui>CSV</Ui> — export the list with the current filters; <Ui>Import</Ui>; <Ui>Add</Ui> — a new
                    account.
                </li>
            </ul>
            <p>Without the “Manage users” permission, the user card opens read-only.</p>
        </>
    );
}
