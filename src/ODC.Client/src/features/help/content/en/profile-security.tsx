import profileSecurityImage from "../../images/en/profile-security.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "profile-security" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The <Ui>Security</Ui> tab has four cards. The first three are described below, the <Ui>Other devices</Ui> card — in “
                <Ref to="profile-sessions">Signing out on other devices</Ref>”.
            </p>
            <Figure
                src={profileSecurityImage}
                alt="Account security"
                caption="Profile → Security: the password, recovery codes and sign-ins on other devices."
            />
            <h2>Password</h2>
            <p>
                The <Ui>Change password</Ui> button opens the password change form (see “
                <Ref to="account-change-password">Changing your password</Ref>”).
            </p>
            <h2>Two-factor authentication</h2>
            <p>
                Shows the status: <Ui>Enabled</Ui> or <Ui>Not set up</Ui>. Changed your phone? Ask the administrator to reset your
                authenticator — you will set up the new one the next time you sign in.
            </p>
            <h2>Recovery codes</h2>
            <p>Shows how many codes are left. If you are running low on codes or suspect someone has seen them, generate a new set:</p>
            <Steps>
                <li>
                    Open the authenticator app and enter the current 6-digit code in the field next to the <Ui>New codes</Ui> button. This
                    is an extra confirmation: without your phone the codes cannot be changed, even by someone who has access to your
                    signed-in browser.
                </li>
                <li>
                    Click <Ui>New codes</Ui> and confirm “Generate new codes? The old ones will stop working.”
                </li>
                <li>
                    10 new codes appear. Click <Ui>Copy</Ui> and save them — they will not be shown again. The old codes no longer work.
                </li>
            </Steps>
            <p>
                A wrong code shows the message “The code is incorrect or has already been used. Enter the next code from the app.” Wrong
                codes count towards the failed attempts limit (5 attempts — blocked for 15 minutes).
            </p>
        </>
    );
}
