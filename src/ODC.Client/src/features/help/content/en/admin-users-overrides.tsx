import { Ui } from "../../components/Manual";

/**
 * The manual page "admin-users-overrides" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The “Individual permissions” card (visible with the “Manage roles” permission) lets you grant or deny specific permissions
                on top of the position roles. For each permission choose:
            </p>
            <ul>
                <li>
                    <Ui>By roles</Ui> — as defined by the position roles (default);
                </li>
                <li>
                    <Ui>Grant</Ui> — the permission applies even if the roles don't give it;
                </li>
                <li>
                    <Ui>Deny</Ui> — the permission doesn't apply even if a role gives it.
                </li>
            </ul>
            <p>
                The “in effect” badge marks the permissions the user actually has. Denials don't apply to system administrators. Use
                individual permissions for exceptions; for typical cases, configure a role instead.
            </p>
        </>
    );
}
