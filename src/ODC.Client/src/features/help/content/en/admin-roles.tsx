import { TriangleAlert } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Code, Figure, Steps, Ui } from "../../components/Manual";
import adminRolesImage from "../../images/en/admin-roles.webp";
import adminRoleEditImage from "../../images/en/admin-role-edit.webp";

/**
 * The manual page "admin-roles" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Roles and permissions</Ui>. A role is a set of permissions for a position; a user receives the
                permissions of all their positions.
            </p>
            <Figure src={adminRolesImage} alt="The roles" caption="Roles and permissions." />
            <Figure
                src={adminRoleEditImage}
                alt="Editing a role"
                caption="Editing a role: the permissions grouped by the parts of the cabinet."
            />
            <ul>
                <li>
                    List: search by name or code, columns “Role”, “Code”, “Type” (“System” / “Custom”, and also “Managerial”), “Permissions”
                    (count), “Users” (count), <Ui>Configure</Ui>.
                </li>
                <li>
                    <Ui>New role</Ui> — create a custom role for the needs of your field (for example, “Project manager”, “Dean”, “Chief
                    physician”).
                </li>
            </ul>
            <h2>Configuring a role</h2>
            <Steps>
                <li>
                    In the “Role” card, enter the <Ui>Code</Ui> (only when creating: lowercase Latin letters, digits and “-”, up to 64
                    characters, for example <Code>project-manager</Code> — used in import), the <Ui>Name</Ui> (required) and the{" "}
                    <Ui>Description</Ui>. The <Ui>Managerial position</Ui> checkbox lists holders of the role first among the unit's staff,
                    with the “Head” badge.
                </li>
                <li>In the “Permissions” card, check the permissions you need — they are grouped by module, each with a description.</li>
                <li>
                    Click <Ui>Save</Ui> — “Role saved”. The changes take effect for all holders of the role within 5 minutes at most,
                    without signing in again.
                </li>
            </Steps>
            <ul>
                <li>
                    The names, descriptions and permissions of system roles can be changed (for example, rename “Head of organization” to
                    “Rector” or “CEO”), but the system roles themselves can't be deleted. The “System administrator” role always has all
                    permissions.
                </li>
                <li>
                    <Ui>Delete role</Ui> — only for custom roles that aren't assigned to anyone (otherwise: “The role is assigned to users —
                    change their positions first.”).
                </li>
            </ul>
            <Alert
                tone="outline"
                icon={TriangleAlert}
                title="Be careful with the “Manage roles” and “Administer all organizations” permissions"
            >
                Anyone with “Manage roles” can change the permissions of roles and assign any role, including system administrator.
                “Administer all organizations” extends administrative permissions to the whole cabinet. Grant these permissions only to
                administrators of the entire cabinet.
            </Alert>
        </>
    );
}
