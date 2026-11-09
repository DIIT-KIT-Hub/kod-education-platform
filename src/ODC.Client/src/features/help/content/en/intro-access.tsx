import { CircleCheck, Lock, Users } from "lucide-react";
import { Badge } from "@/shared/ui/Badge";
import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "intro-access" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                What you can see and do in ODC is determined by <strong>permissions</strong>. Permissions are granted by a{" "}
                <strong>role</strong>, which an administrator assigns together with a position in a unit (for example, “Lead engineer” in
                the development department with the “Employee” role).
            </p>
            <ul>
                <li>A person can hold several positions in different units — the permissions of all their roles are then combined.</li>
                <li>
                    An administrator can also <strong>grant</strong> or <strong>deny</strong> a specific permission to a specific person.
                </li>
                <li>
                    Administrative permissions (users, structure, news) apply only within the organizations of the positions whose roles
                    grant them (see “<Ref to="admin-scope">Administration within organizations</Ref>”).
                </li>
                <li>
                    You can see your positions, roles and permissions in <Ui>Profile</Ui> (see “<Ref to="profile-overview">My profile</Ref>
                    ”).
                </li>
                <li>
                    At the beginning of each section of this manual there is a badge:{" "}
                    <Badge tone="inverse" icon={CircleCheck}>
                        Available to you
                    </Badge>
                    ,{" "}
                    <Badge tone="dashed" icon={Users}>
                        For all users
                    </Badge>{" "}
                    or{" "}
                    <Badge tone="outline" icon={Lock}>
                        Permission required
                    </Badge>{" "}
                    with the name of the required permission.
                </li>
            </ul>
            <p>
                The full list of roles and permissions is in the “<Ref to="roles">Roles and permissions</Ref>” chapter. If you need access
                to a module, contact the head of your unit or the system administrator. Trying to open an unavailable page shows “Access
                restricted” — “You don't have access to this section”.
            </p>
            <p>Changes to permissions take effect within 5 minutes at most, without signing in again.</p>
        </>
    );
}
