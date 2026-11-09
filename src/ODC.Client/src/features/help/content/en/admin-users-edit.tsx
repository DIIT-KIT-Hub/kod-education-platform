import adminUserEditImage from "../../images/en/admin-user-edit.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "admin-users-edit" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open the user (<Ui>Open</Ui> in the list). At the top are the full name, email and badges: “Active” / “Blocked”, “2FA
                enabled” / “2FA not set up”, “Awaiting activation” (the user hasn't chosen a password yet), “No photo”.
            </p>
            <Figure src={adminUserEditImage} alt="A user card" caption="A user card: positions, permissions and account actions." />
            <h2>Positions</h2>
            <p>The role of a position defines permissions; the unit defines the scope of access to documents.</p>
            <Steps>
                <li>
                    Click <Ui>Add position</Ui>.
                </li>
                <li>
                    Select a <Ui>Unit</Ui> and a <Ui>Role</Ui>, and enter the <Ui>Position title</Ui> (for example, “Chief accountant”; up
                    to 128 characters). The unit list contains only units of organizations you manage; positions in other organizations are
                    assigned by their administrators (see “<Ref to="admin-scope">Administration within organizations</Ref>”).
                </li>
                <li>
                    Check <Ui>Primary</Ui> for the main position (only one; if none is selected, the first one becomes primary). The primary
                    position is shown in lists and signing routes; the others count as secondary positions.
                </li>
                <li>
                    Remove an unneeded position with <Ui>Remove position</Ui>. The same “unit + role” pair cannot appear twice.
                </li>
                <li>
                    Click <Ui>Save changes</Ui> — “Changes saved.” The new permissions take effect for the user within 5 minutes at most,
                    without signing in again.
                </li>
            </Steps>
            <p>
                Without the “Manage roles” permission you can assign only roles whose permissions you all have yourself; the system
                administrator role is unavailable. Error: “You can't assign the role “X” because it grants more permissions than you have.”
            </p>
            <p>An account cannot be deleted — only blocked. This preserves the history of documents and signatures.</p>
        </>
    );
}
