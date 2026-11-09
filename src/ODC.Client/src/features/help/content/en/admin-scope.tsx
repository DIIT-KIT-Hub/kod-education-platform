import { Ref, Table } from "../../components/Manual";

/**
 * The manual page "admin-scope" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Several organizations can work in one cabinet, and each administers its own people, structure and news. That is why the
                permissions “View users”, “Manage users”, “Import users”, “Export users”, “Account security”, “Manage structure” and “Manage
                news” apply <strong>only in the organizations of your positions</strong> — including all their units and nested (subsidiary)
                organizations.
            </p>
            <ul>
                <li>
                    A permission applies in the organization of the position whose role grants it. For example, an HR specialist of
                    organization A who also holds a regular staff position in organization B manages accounts only in A. A permission
                    granted individually applies in the organizations of all your positions.
                </li>
                <li>
                    The <strong>“Administer all organizations”</strong> permission extends your administrative permissions to the whole
                    cabinet. By default only the “System administrator” role has it; grant it only to administrators of the entire cabinet.
                </li>
            </ul>
            <Table>
                <thead>
                    <tr>
                        <th>What</th>
                        <th>Organization administrator</th>
                        <th>Administrator of all organizations</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>User list, user card, export</td>
                        <td>
                            Accounts that have at least one position in their organizations. The export includes only positions in these
                            organizations.
                        </td>
                        <td>All accounts, including those without positions.</td>
                    </tr>
                    <tr>
                        <td>Positions</td>
                        <td>Assigns and changes positions only in units of their own organizations.</td>
                        <td>In any unit.</td>
                    </tr>
                    <tr>
                        <td>Personal details, email, blocking, password, 2FA, sessions, individual permissions</td>
                        <td>Only for accounts whose positions are all in their organizations.</td>
                        <td>
                            For all accounts (subject to the “<Ref to="admin-hierarchy">authority limits</Ref>”).
                        </td>
                    </tr>
                    <tr>
                        <td>Import</td>
                        <td>
                            Unit codes of their own organizations only; updates only accounts whose positions are all in their
                            organizations; a new account must have a position.
                        </td>
                        <td>Without these restrictions.</td>
                    </tr>
                    <tr>
                        <td>Structure</td>
                        <td>Creates, edits and moves units and subsidiary organizations within their own organizations.</td>
                        <td>
                            Also creates and moves top-level organizations, turns an organization into a regular unit and maintains the unit
                            type catalog.
                        </td>
                    </tr>
                    <tr>
                        <td>News</td>
                        <td>Publishes and edits news of their own organizations.</td>
                        <td>Also news of the whole cabinet and of any organization.</td>
                    </tr>
                </tbody>
            </Table>
            <h2>Account in several organizations</h2>
            <p>
                If a person holds positions both in your organization and in another one, you see them in the list, and their card is marked
                “Account in several organizations”. You change only their positions in your organizations; positions in other organizations
                are shown as “Position in another organization” and cannot be edited, and if the primary position is there, it stays
                primary. The personal details, email, status and security of such an account are changed by an administrator who manages all
                of its organizations, or by an administrator of all organizations. Otherwise you will see: “The user also holds positions in
                organizations you don't manage…”
            </p>
            <p>
                An account without any position belongs to no organization, so only an administrator of all organizations can see it. An
                organization administrator cannot leave a user without positions: “Assign at least one position in a unit of your
                organization…” — to revoke access, block the account instead.
            </p>
            <p>
                Roles, the unit type catalog and the audit log are shared by the whole cabinet: the “Audit log” permission opens events of
                all organizations.
            </p>
        </>
    );
}
