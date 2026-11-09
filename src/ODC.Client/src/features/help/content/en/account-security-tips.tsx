import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "account-security-tips" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>Don't use your ODC password for other services. A password manager is the most convenient way to store passwords.</li>
                <li>
                    Never share your password, activation codes from emails, authenticator codes or recovery codes with anyone — ODC staff
                    never ask for them.
                </li>
                <li>On shared computers, don't check “Remember me” and always sign out when you finish.</li>
                <li>
                    Forgot to sign out on someone else's computer or lost your phone — click <Ui>Sign out on other devices</Ui> on the{" "}
                    <Ui>Security</Ui> tab of your profile.
                </li>
                <li>
                    The private key of your electronic signature and its password are separate from your ODC account; they are never sent to
                    the server (see the “<Ref to="signing">Signing and verifying files</Ref>” chapter).
                </li>
            </ul>
        </>
    );
}
