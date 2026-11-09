import loginImage from "../../images/en/login.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "account-sign-in" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Enter your <Ui>Email</Ui> and <Ui>Password</Ui>.
                </li>
                <li>
                    If you wish, check <Ui>Remember me on this device</Ui> — the sign-in is then kept after the browser is closed (without
                    it, the sign-in ends as soon as you close the browser). Don't do this on shared computers.
                </li>
                <li>
                    Click <Ui>Continue</Ui>.
                </li>
                <li>
                    On the <Ui>Authenticator code</Ui> page, enter the 6-digit code from the app in the <Ui>Verification code</Ui> field and
                    click <Ui>Sign in</Ui>.
                </li>
            </Steps>
            <Figure
                src={loginImage}
                alt="The sign-in page"
                caption="The sign-in page: e-mail, password and, on the next step, the authenticator code."
            />
            <p>
                Each code can be used <strong>only once</strong>. If a code has just been used (for example, you signed out and are signing
                in again right away), wait for the next code in the app — the system will say “The code is incorrect or no longer valid. Try
                the next code.”
            </p>
            <p>
                If codes are not accepted, make sure automatic time is turned on on your phone: the codes depend on the clock, and the
                allowed drift is up to 30 seconds.
            </p>
            <p>
                After signing in you return to the page that redirected you to sign in, or to <Ui>Home</Ui>.
            </p>
            <p>
                To change the interface language before signing in, use the language button in the top-right corner of the sign-in page.
                After you sign in, the language saved in your profile applies (see “<Ref to="profile-overview">My profile</Ref>”).
            </p>
        </>
    );
}
