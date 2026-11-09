import { Info } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";
import activationImage from "../../images/en/activation.webp";

/**
 * The manual page "account-activation" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                There is no self-registration in ODC: accounts are created by the HR department or the system administrator — without a
                password. You choose the password yourself during <strong>activation</strong>, so nobody but you knows it: administrators
                neither issue nor see passwords. Activation takes 5–7 minutes.
            </p>
            <Figure
                src={activationImage}
                alt="The account activation page"
                caption="Activation: enter your work e-mail and a code will arrive there."
            />
            <p>
                As soon as the account is created, an email “Your ODC account has been created” with an <Ui>Activate the account</Ui> button
                arrives at your work address. If there is no email, open the sign-in page and click <Ui>Activate account</Ui> in the “New to
                ODC?” block — the result is the same.
            </p>
            <p>
                Have a smartphone ready with an authenticator app: <strong>Google Authenticator</strong>,{" "}
                <strong>Microsoft Authenticator</strong> or any other app that supports TOTP one-time codes (for example, 1Password,
                Bitwarden, Authy).
            </p>
            <p>
                Activation is one form with the steps <strong>E-mail → Code → Password → Authenticator → Photo</strong>; the current step is
                marked at the top of the page. It is tied to the browser where you started it: if you reload the page, you continue from the
                same step.
            </p>
            <h2>Step 1. E-mail</h2>
            <Steps>
                <li>
                    In the <Ui>Email</Ui> field, enter your work address — the one the account was created for.
                </li>
                <li>
                    Click <Ui>Send code</Ui>.
                </li>
            </Steps>
            <h2>Step 2. Code</h2>
            <Steps>
                <li>
                    If the account is waiting for activation, an email with a <strong>6-digit one-time code</strong> arrives at that address
                    (the code is not in the subject — open the email).
                </li>
                <li>
                    Enter the code in the <Ui>Code from the e-mail</Ui> field and click <Ui>Confirm</Ui>.
                </li>
            </Steps>
            <ul>
                <li>
                    The code is valid for <strong>10 minutes</strong> and works only once; after 5 wrong attempts it stops working — send a
                    new one.
                </li>
                <li>
                    <Ui>Send the code again</Ui> becomes available 60 seconds after the previous email; at most 5 codes per hour can be sent
                    to one address. A new code cancels all the previous ones.
                </li>
                <li>
                    Mistyped the address — click <Ui>Change e-mail</Ui> and start over.
                </li>
            </ul>
            <Alert tone="outline" icon={Info} title="No email?">
                For privacy reasons, the page gives the same answer for any address — so outsiders cannot find out whether an account
                exists. Check the Spam folder and the address. If there is still no email, contact your administrator: perhaps the account
                has not been created yet or is blocked. If you have already activated the account, no code is sent — just sign in with your
                password.
            </Alert>
            <h2>Step 3. Password</h2>
            <Steps>
                <li>
                    Enter a <Ui>New password</Ui> and <Ui>Confirm password</Ui>. Requirements: 12 to 128 characters, uppercase and lowercase
                    letters and digits.
                </li>
                <li>
                    Click <Ui>Save password</Ui>. If the password appears in known data breaches, the system asks you to choose another one
                    — this protects you from guessing attacks based on “popular” passwords.
                </li>
            </Steps>
            <p>
                Set the password within <strong>15 minutes</strong> of entering the code; otherwise you will have to start the activation
                again from the email step.
            </p>
            <h2>Step 4. Authenticator</h2>
            <Steps>
                <li>
                    On the <Ui>Secure your sign-in</Ui> page, open the authenticator app on your phone and add a new account.
                </li>
                <li>
                    Scan the QR code with your phone camera. If scanning is inconvenient, choose manual entry in the app and type the key
                    shown under the QR code (the <Ui>Copy</Ui> button copies it).
                </li>
                <li>
                    An “ODC” entry with a 6-digit code that changes every 30 seconds appears in the app. Enter the current code in the{" "}
                    <Ui>Code from the app</Ui> field and click <Ui>Verify and sign in</Ui>.
                </li>
                <li>
                    The system shows <strong>10 recovery codes</strong>. Click <Ui>Copy codes</Ui> and keep them in a safe place (a password
                    manager, a printed sheet in a safe). They will not be shown again.
                </li>
                <li>
                    Click <Ui>Next</Ui>.
                </li>
            </Steps>
            <p>
                If an authenticator is already linked (for example, after a password reset), this step is shorter: enter the 6-digit code
                from the app and click <Ui>Confirm</Ui>. No phone at hand — <Ui>Sign in with a recovery code</Ui>.
            </p>
            <h2>Step 5. Photo</h2>
            <p>
                On the <Ui>Add a profile photo</Ui> step, choose a photo, place the face inside the circle and click <Ui>Save photo</Ui>{" "}
                (see “<Ref to="profile-photo">Profile photo</Ref>”). The cabinet cannot be used without a photo. Then the cabinet opens with
                the message “Your account is activated. Welcome to ODC!”
            </p>
            <Alert tone="outline" icon={Info} title="Why this is mandatory">
                Two-factor authentication is mandatory for all accounts, including administrators. Even if someone learns your password,
                they cannot sign in without your phone.
            </Alert>
            <p>
                If you don't link the authenticator within 10 minutes of saving the password, the password already works: sign in on the
                sign-in page with your new password — the system continues with linking the authenticator.
            </p>
        </>
    );
}
