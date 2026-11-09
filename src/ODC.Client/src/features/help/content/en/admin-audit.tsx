import adminAuditImage from "../../images/en/admin-audit.webp";
import { Figure, Table, Ui } from "../../components/Manual";

/**
 * The manual page "admin-audit" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Audit log</Ui> — a timeline of important events: who did what, when, and from which IP
                address.
            </p>
            <Figure
                src={adminAuditImage}
                alt="The audit log"
                caption="The audit log: who did what and when; filters by category and dates."
            />
            <ul>
                <li>Search by user name, details or IP.</li>
                <li>
                    Category filter: <Ui>All events</Ui>, “Authentication”, “Documents”, “Users”, “Roles”, “Structure”, “News”.
                </li>
                <li>
                    Period: <Ui>From date</Ui> and <Ui>To date</Ui> (inclusive). Dates are calendar days in Kyiv time, as are the event
                    times in the table: “to 25.10” covers events up to 23:59 on October 25, Kyiv time.
                </li>
                <li>Columns “Time” (newest first), “Event” (with details), “User”, “IP”. 50 entries per page.</li>
            </ul>
            <Table>
                <thead>
                    <tr>
                        <th>Category</th>
                        <th>Events</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Authentication</td>
                        <td>
                            Sign-in (with details: “Password + authenticator”, “Password + recovery code”), Failed sign-in attempt, Lockout
                            after sign-in attempts, Sign-out, Authenticator connected, Authenticator reset, New recovery codes, Password
                            change, Password reset, Activation code sent, Wrong activation code, Account activated, Sessions ended.
                        </td>
                    </tr>
                    <tr>
                        <td>Users</td>
                        <td>User created, User changed, User status change, User import, User export.</td>
                    </tr>
                    <tr>
                        <td>Roles</td>
                        <td>Role change, Role deletion.</td>
                    </tr>
                    <tr>
                        <td>Structure</td>
                        <td>Structure change.</td>
                    </tr>
                    <tr>
                        <td>Documents</td>
                        <td>
                            Document created, Document changed (with the new file name and the number of revoked signatures), Document sent
                            for signing, Document withdrawn, Document signed (with the format, provider and certificate number), Signing
                            declined, Document archived, Document deleted.
                        </td>
                    </tr>
                    <tr>
                        <td>News</td>
                        <td>News change, News deletion.</td>
                    </tr>
                </tbody>
            </Table>
            <p>
                A series of “Failed sign-in attempt” events for one account from different IP addresses may indicate password guessing —
                check whether the user has 2FA enabled and, if needed, reset the password: the user sets a new one through the activation.
            </p>
        </>
    );
}
