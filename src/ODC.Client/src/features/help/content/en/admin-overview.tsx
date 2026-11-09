import adminImage from "../../images/en/admin.webp";
import { Figure, Ref, Table, Ui } from "../../components/Manual";

/**
 * The manual page "admin-overview" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The <Ui>Administration</Ui> menu item is visible to users with at least one administrative permission. The page contains
                tiles, each shown only if you have the matching permission:
            </p>
            <Figure src={adminImage} alt="The administration page" caption="Administration: the sections your permissions open." />
            <Table>
                <thead>
                    <tr>
                        <th>Tile</th>
                        <th>What it contains</th>
                        <th>Required permission</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Users</td>
                        <td>Accounts, positions, blocking, password and 2FA resets.</td>
                        <td>View users</td>
                    </tr>
                    <tr>
                        <td>Import and export</td>
                        <td>Bulk adding of users from XLSX/CSV and exporting them.</td>
                        <td>Import users or Export users</td>
                    </tr>
                    <tr>
                        <td>Roles and permissions</td>
                        <td>Permission sets for positions and modules.</td>
                        <td>Manage roles</td>
                    </tr>
                    <tr>
                        <td>Structure</td>
                        <td>
                            Organizations and their units: departments, divisions, branches (see “
                            <Ref to="structure-manage">Managing organizations and units</Ref>”).
                        </td>
                        <td>Manage structure</td>
                    </tr>
                    <tr>
                        <td>Unit types</td>
                        <td>
                            Names of the structure levels: organization, department, division, section… (see “
                            <Ref to="structure-types">Unit types</Ref>”).
                        </td>
                        <td>Manage structure</td>
                    </tr>
                    <tr>
                        <td>News</td>
                        <td>
                            Posts and announcements (see “<Ref to="news-list">Managing posts</Ref>”).
                        </td>
                        <td>Manage news</td>
                    </tr>
                    <tr>
                        <td>Requests</td>
                        <td>Bugs, suggestions and questions of users: answers, handlers, state.</td>
                        <td>Handling requests</td>
                    </tr>
                    <tr>
                        <td>Audit log</td>
                        <td>Sign-ins, permission changes, document signing.</td>
                        <td>Audit log</td>
                    </tr>
                </tbody>
            </Table>
            <p>
                You manage users, structure and news only in the organizations of your positions — see “
                <Ref to="admin-scope">Administration within organizations</Ref>”.
            </p>
        </>
    );
}
