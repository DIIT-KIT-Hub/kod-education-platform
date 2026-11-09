import { Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "profile-sessions" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The <Ui>Other devices</Ui> card on the <Ui>Security</Ui> tab ends all your sign-ins except this browser. You will need it if
                you left yourself signed in on someone else's computer or lost your phone.
            </p>
            <Steps>
                <li>
                    Open <Ui>Profile</Ui> → the <Ui>Security</Ui> tab.
                </li>
                <li>
                    In the <Ui>Other devices</Ui> card, click <Ui>Sign out on other devices</Ui> and confirm “Sign out on all other
                    devices?”.
                </li>
                <li>The message “Signed out on other devices” appears.</li>
            </Steps>
            <p>
                You stay signed in in this browser. Other devices lose access <strong>within a few minutes</strong> (at most 5 minutes).
                After that, a new sign-in is needed on those devices.
            </p>
            <p>
                If you suspect someone else knows your password, change it (see “
                <Ref to="account-change-password">Changing your password</Ref>”) — this also ends the sign-ins on all other devices.
            </p>
        </>
    );
}
