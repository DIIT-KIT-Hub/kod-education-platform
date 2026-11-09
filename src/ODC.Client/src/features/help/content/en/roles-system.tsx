import { Ui } from "../../components/Manual";
import { AdministratorRoleName, SystemRoleReference } from "../../components/RoleReference";

/**
 * The manual page "roles-system" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                When ODC is deployed, it creates standard roles common to most organizations. The system administrator can change their
                names and permissions and create custom roles for their domain — the current set of roles in your cabinet is shown in{" "}
                <Ui>Administration</Ui> → <Ui>Roles</Ui> (if you have access).
            </p>
            <SystemRoleReference />
            <p>
                The{" "}
                <strong>
                    “<AdministratorRoleName />”
                </strong>{" "}
                role always has all permissions, including new ones added in future versions.
            </p>
        </>
    );
}
