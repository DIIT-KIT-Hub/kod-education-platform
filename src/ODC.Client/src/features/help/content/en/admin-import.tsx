import adminImportImage from "../../images/en/admin-import.webp";
import { Figure, Ref, Steps, Table, Ui } from "../../components/Manual";

/**
 * The manual page "admin-import" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Import and export</Ui> (or <Ui>Import</Ui> in the user list). Import creates new accounts and
                updates existing ones (matched by email).
            </p>
            <Figure src={adminImportImage} alt="Importing users" caption="Import: download the template, fill it in and add the file." />
            <h2>Preparing the file</h2>
            <Steps>
                <li>
                    In the “Template” card, download the <Ui>XLSX</Ui> or <Ui>CSV</Ui> template — it contains three examples. Expand{" "}
                    <Ui>Role codes</Ui> to see the allowed codes.
                </li>
                <li>
                    Fill in the columns (the first row holds the headers; keep the Ukrainian header names from the template, case doesn't
                    matter):
                </li>
            </Steps>
            <Table>
                <thead>
                    <tr>
                        <th>Column</th>
                        <th>Required</th>
                        <th>What to enter</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Email</td>
                        <td>yes</td>
                        <td>The user's sign-in name. The headers “e-mail”, “пошта” and “електронна пошта” are also accepted.</td>
                    </tr>
                    <tr>
                        <td>Прізвище</td>
                        <td>yes</td>
                        <td>Last name.</td>
                    </tr>
                    <tr>
                        <td>Ім'я</td>
                        <td>yes</td>
                        <td>First name.</td>
                    </tr>
                    <tr>
                        <td>По батькові</td>
                        <td>no</td>
                        <td>Middle name.</td>
                    </tr>
                    <tr>
                        <td>Телефон</td>
                        <td>no</td>
                        <td>Phone number in the +380… format; an empty cell doesn't erase an existing number.</td>
                    </tr>
                    <tr>
                        <td>Код підрозділу</td>
                        <td>for a position</td>
                        <td>Unit code from the structure (for example, SALES). Codes are unique across all organizations.</td>
                    </tr>
                    <tr>
                        <td>Код ролі</td>
                        <td>for a position</td>
                        <td>Role code (for example, head-of-unit).</td>
                    </tr>
                    <tr>
                        <td>Посада</td>
                        <td>no</td>
                        <td>Position title (for example, “Associate professor”).</td>
                    </tr>
                    <tr>
                        <td>Статус</td>
                        <td>no</td>
                        <td>
                            “заблокований”, “заблоковано”, “blocked”, “inactive”, “0”, “ні” or “no” — the account will be blocked; anything
                            else or empty — active.
                        </td>
                    </tr>
                </tbody>
            </Table>
            <ul>
                <li>Several rows with the same email are several positions of one person. The first row sets the primary position.</li>
                <li>If the file has no positions for an existing user, their positions don't change.</li>
                <li>CSV must be UTF-8 encoded; the delimiter is detected automatically. For XLSX, the first sheet is read.</li>
                <li>Limits: up to 5 MB and up to 10,000 rows.</li>
            </ul>
            <h2>Validation and import</h2>
            <Steps>
                <li>
                    Drag the file into the “1. Select a file” card and click <Ui>Validate</Ui> — the file is checked without saving
                    anything.
                </li>
                <li>
                    The “2. Validation result” card shows “Rows in file”, “Will be created”, “Will be updated”. If there are errors, a “Row
                    / Email / Error” table appears. Fix the file and validate again: import is possible only when there are no errors.
                </li>
                <li>
                    When there are no errors, click <Ui>Import</Ui>. The import is applied completely or not at all.
                </li>
                <li>
                    If new accounts were created, the report shows how many invitations were sent: “New accounts: N. Invitations sent: M.
                    Users activate their accounts themselves — nobody hands out passwords.” New accounts are created without passwords, and
                    there is no passwords file (see “<Ref to="account-activation">Account activation</Ref>”).
                </li>
            </Steps>
            <h2>Common row errors</h2>
            <ul>
                <li>“Invalid email.”, “Last name and first name are required.”</li>
                <li>“Unit with code “X” not found.”, “Role with code “X” not found.”</li>
                <li>“Unit “X” belongs to an organization you don't manage…” — import assigns positions only in your organizations.</li>
                <li>
                    “An account with this email already exists and has positions in organizations you don't manage…” — such an account is
                    changed by an administrator of all of its organizations.
                </li>
                <li>
                    “Specify the unit code and the role code…” — a new account without a position would end up outside your organizations.
                </li>
                <li>“You can't assign the role “X”.” — the role grants more permissions than you have.</li>
                <li>“This account has more permissions than you, so the import can't change it.”</li>
                <li>“You can't block your own account.”</li>
            </ul>
            <p>
                If sending email is not configured in the cabinet, the report warns about it — tell the users to activate their accounts
                with the <Ui>Activate</Ui> button on the sign-in page.
            </p>
            <p>Blocking through import ends the sign-ins of the blocked users within a few minutes.</p>
        </>
    );
}
