import { Access, Ref, Steps, Ui, Warning } from "../../components/Manual";

/**
 * The manual page "admin-organizations" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Access permissions={["administration.all"]} />
            <p>
                The <Ui>Administration → Organizations</Ui> page shows every organization of the cabinet. Each has its own database, its own
                database accounts and its own key that encrypts its files.
            </p>
            <ul>
                <li>
                    <Ui>State</Ui>: <Ui>Active</Ui>, <Ui>Suspended</Ui>, <Ui>Being created</Ui> or <Ui>Being deleted</Ui>. The{" "}
                    <Ui>Database not up to date</Ui> badge means the database could not be updated at the start: the organization is
                    temporarily unavailable while the others work. Tell the server's operator.
                </li>
                <li>
                    <Ui>Database</Ui>: the database's name, whether it has its own PostgreSQL roles and when it was last updated.
                </li>
                <li>
                    <Ui>Staff</Ui>: how many people hold positions in the organization.
                </li>
            </ul>
            <h2>A new organization</h2>
            <p>
                The <Ui>New organization</Ui> button opens the unit form: choose a type marked as an organization and leave the parent unit
                empty (see “<Ref to="structure-manage">Managing the structure</Ref>”). The cabinet creates the organization's database, its
                accounts and its key and adds the root of the structure.
            </p>
            <h2>Suspending and resuming</h2>
            <Steps>
                <li>
                    Click <Ui>Suspend</Ui> in the organization's row and confirm.
                </li>
                <li>
                    The organization stops answering requests, leaves the switcher, and people who work only there are signed out. The data
                    stays.
                </li>
                <li>
                    To bring the organization back, click <Ui>Resume</Ui>.
                </li>
            </Steps>
            <p>
                <Ui>New database passwords</Ui> replaces the passwords of the organization's database accounts with random ones; the cabinet
                uses them at once. Do it when a password may have reached anyone else.
            </p>
            <h2>Deleting</h2>
            <Warning title="No way back">
                Only a suspended organization can be deleted. Its database, files, encryption key and people's memberships go away with it
                for good. The organization can be brought back only from a backup made by the server's operator.
            </Warning>
            <Steps>
                <li>
                    Suspend the organization, then click <Ui>Delete</Ui>.
                </li>
                <li>Type the organization's code to confirm.</li>
                <li>
                    Click <Ui>Delete for good</Ui>.
                </li>
            </Steps>
            <p>Each of these actions is recorded in the platform's audit log.</p>
        </>
    );
}
