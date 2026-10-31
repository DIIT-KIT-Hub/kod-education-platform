import { CircleCheck } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Chapter, Ref, Topic, Ui } from "../../components/Manual";
import { AdministratorRoleName, PermissionReference, SystemRoleReference } from "../../components/RoleReference";

/**
 * Roles and permissions: the reference of every permission and the standard roles (English).
 * @returns The chapter.
 */
export function RolesChapter() {
    return (
        <Chapter id="roles">
            <Topic id="roles-permissions">
                <p>
                    A permission is the right to perform a specific action. Below are all permissions of the cabinet, grouped by module.
                    Your own permissions are marked <Icon icon={CircleCheck} />.
                </p>
                <PermissionReference />
                <p>
                    Several document permissions determine <strong>whose</strong> documents you see — see “
                    <Ref to="documents-visibility">Whose documents you see</Ref>”.
                </p>
            </Topic>
            <Topic id="roles-system">
                <p>
                    When KOD is deployed, it creates standard roles common to most organizations. The system administrator can change their
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
            </Topic>
        </Chapter>
    );
}
