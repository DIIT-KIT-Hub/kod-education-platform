import { Code, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "account-recovery" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                If you don't have your phone at hand, sign in with one of the recovery codes you received when setting up the authenticator.
            </p>
            <Steps>
                <li>Enter your email and password as usual.</li>
                <li>
                    On the code page, click <Ui>Sign in with a recovery code</Ui>.
                </li>
                <li>
                    Enter the code in the <Ui>Recovery code</Ui> field (format <Code>XXXXX-XXXXX</Code>) and click <Ui>Sign in</Ui>.
                </li>
                <li>
                    Each recovery code works once. After signing in, generate new codes in your profile (see “
                    <Ref to="profile-security">Security: password, 2FA, recovery codes</Ref>”).
                </li>
            </Steps>
            <p>A sign-in with a recovery code is not remembered after the browser is closed.</p>
        </>
    );
}
