import { Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "account-change-password" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Open <Ui>Profile</Ui> → the <Ui>Security</Ui> tab → <Ui>Change password</Ui>.
                </li>
                <li>
                    Enter your <Ui>Current password</Ui>, <Ui>New password</Ui> and <Ui>Repeat new password</Ui>.
                </li>
                <li>
                    Click <Ui>Save password</Ui>. The message “Password changed” appears.
                </li>
            </Steps>
            <p>
                After the password is changed, <strong>the sign-ins on all other devices are ended</strong> within a few minutes — you will
                need to sign in again there. You stay signed in in this browser.
            </p>
            <p>
                If you don't remember your current password, you cannot change it here — ask an administrator to reset the password (see “
                <Ref to="account-forgot">Forgot your password or changed your phone</Ref>”).
            </p>
        </>
    );
}
