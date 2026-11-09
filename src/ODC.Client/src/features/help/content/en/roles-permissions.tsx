import { CircleCheck } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Ref } from "../../components/Manual";
import { PermissionReference } from "../../components/RoleReference";

/**
 * The manual page "roles-permissions" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                A permission is the right to perform a specific action. Below are all permissions of the cabinet, grouped by module. Your
                own permissions are marked <Icon icon={CircleCheck} />.
            </p>
            <PermissionReference />
            <p>
                Several document permissions determine <strong>whose</strong> documents you see — see “
                <Ref to="documents-visibility">Whose documents you see</Ref>”.
            </p>
        </>
    );
}
