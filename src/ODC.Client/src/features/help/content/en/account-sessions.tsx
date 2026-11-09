import { LogOut, Menu } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "account-sessions" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>A sign-in works in the browser on the device where you signed in, and it ends automatically:</p>
            <ul>
                <li>
                    after <strong>1 hour of inactivity</strong>;
                </li>
                <li>
                    no later than <strong>12 hours after sign-in</strong>, even if you have been working all the time — then just sign in
                    again;
                </li>
                <li>
                    when you close the browser, if you didn't check <Ui>Remember me on this device</Ui> when signing in.
                </li>
            </ul>
            <p>
                To sign out, click the sign-out button <Icon icon={LogOut} /> next to your name at the bottom of the side menu (on a phone —
                in the <Icon icon={Menu} /> menu) and confirm “Sign out of the cabinet?”. This signs you out of this browser only.
            </p>
            <p>
                To end your sign-ins on other devices — for example, if you forgot to sign out on someone else's computer — click{" "}
                <Ui>Sign out on other devices</Ui> in <Ui>Profile</Ui> → <Ui>Security</Ui> (see “
                <Ref to="profile-sessions">Signing out on other devices</Ref>”).
            </p>
            <p>
                Sign-ins on other devices do not end instantly: a device loses access <strong>within a few minutes</strong> (at most 5
                minutes). This happens when you change your password or sign out on other devices, and also when an administrator blocks the
                account, resets the password or the authenticator, or signs out all of the user's sessions.
            </p>
        </>
    );
}
