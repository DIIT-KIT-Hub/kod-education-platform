import { Info, LogOut, Menu } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Icon } from "@/shared/ui/Icon";
import { Chapter, Code, Ref, Steps, Topic, Ui } from "../../components/Manual";

/**
 * Sign-in and account security (English).
 * @returns The chapter.
 */
export function AccountChapter() {
    return (
        <Chapter id="account">
            <Topic id="account-activation">
                <p>
                    There is no self-registration in ODC: accounts are created by the HR department or the system administrator — without a
                    password. You choose the password yourself during <strong>activation</strong>, so nobody but you knows it:
                    administrators neither issue nor see passwords. Activation takes 5–7 minutes.
                </p>
                <p>
                    As soon as the account is created, an email “Your ODC account has been created” with an <Ui>Activate the account</Ui>{" "}
                    button arrives at your work address. If there is no email, open the sign-in page and click <Ui>Activate account</Ui> in
                    the “New to ODC?” block — the result is the same.
                </p>
                <p>
                    Have a smartphone ready with an authenticator app: <strong>Google Authenticator</strong>,{" "}
                    <strong>Microsoft Authenticator</strong> or any other app that supports TOTP one-time codes (for example, 1Password,
                    Bitwarden, Authy).
                </p>
                <p>
                    Activation is one form with the steps <strong>E-mail → Code → Password → Authenticator → Photo</strong>; the current
                    step is marked at the top of the page. It is tied to the browser where you started it: if you reload the page, you
                    continue from the same step.
                </p>
                <h4>Step 1. E-mail</h4>
                <Steps>
                    <li>
                        In the <Ui>Email</Ui> field, enter your work address — the one the account was created for.
                    </li>
                    <li>
                        Click <Ui>Send code</Ui>.
                    </li>
                </Steps>
                <h4>Step 2. Code</h4>
                <Steps>
                    <li>
                        If the account is waiting for activation, an email with a <strong>6-digit one-time code</strong> arrives at that
                        address (the code is not in the subject — open the email).
                    </li>
                    <li>
                        Enter the code in the <Ui>Code from the e-mail</Ui> field and click <Ui>Confirm</Ui>.
                    </li>
                </Steps>
                <ul>
                    <li>
                        The code is valid for <strong>10 minutes</strong> and works only once; after 5 wrong attempts it stops working —
                        send a new one.
                    </li>
                    <li>
                        <Ui>Send the code again</Ui> becomes available 60 seconds after the previous email; at most 5 codes per hour can be
                        sent to one address. A new code cancels all the previous ones.
                    </li>
                    <li>
                        Mistyped the address — click <Ui>Change e-mail</Ui> and start over.
                    </li>
                </ul>
                <Alert tone="outline" icon={Info} title="No email?">
                    For privacy reasons, the page gives the same answer for any address — so outsiders cannot find out whether an account
                    exists. Check the Spam folder and the address. If there is still no email, contact your administrator: perhaps the
                    account has not been created yet or is blocked. If you have already activated the account, no code is sent — just sign
                    in with your password.
                </Alert>
                <h4>Step 3. Password</h4>
                <Steps>
                    <li>
                        Enter a <Ui>New password</Ui> and <Ui>Confirm password</Ui>. Requirements: 12 to 128 characters, uppercase and
                        lowercase letters and digits.
                    </li>
                    <li>
                        Click <Ui>Save password</Ui>. If the password appears in known data breaches, the system asks you to choose another
                        one — this protects you from guessing attacks based on “popular” passwords.
                    </li>
                </Steps>
                <p>
                    Set the password within <strong>15 minutes</strong> of entering the code; otherwise you will have to start the
                    activation again from the email step.
                </p>
                <h4>Step 4. Authenticator</h4>
                <Steps>
                    <li>
                        On the <Ui>Secure your sign-in</Ui> page, open the authenticator app on your phone and add a new account.
                    </li>
                    <li>
                        Scan the QR code with your phone camera. If scanning is inconvenient, choose manual entry in the app and type the
                        key shown under the QR code (the <Ui>Copy</Ui> button copies it).
                    </li>
                    <li>
                        An “ODC” entry with a 6-digit code that changes every 30 seconds appears in the app. Enter the current code in the{" "}
                        <Ui>Code from the app</Ui> field and click <Ui>Verify and sign in</Ui>.
                    </li>
                    <li>
                        The system shows <strong>10 recovery codes</strong>. Click <Ui>Copy codes</Ui> and keep them in a safe place (a
                        password manager, a printed sheet in a safe). They will not be shown again.
                    </li>
                    <li>
                        Click <Ui>Next</Ui>.
                    </li>
                </Steps>
                <p>
                    If an authenticator is already linked (for example, after a password reset), this step is shorter: enter the 6-digit
                    code from the app and click <Ui>Confirm</Ui>. No phone at hand — <Ui>Sign in with a recovery code</Ui>.
                </p>
                <h4>Step 5. Photo</h4>
                <p>
                    On the <Ui>Add a profile photo</Ui> step, choose a photo, place the face inside the circle and click <Ui>Save photo</Ui>{" "}
                    (see “<Ref to="profile-photo">Profile photo</Ref>”). The cabinet cannot be used without a photo. Then the cabinet opens
                    with the message “Your account is activated. Welcome to ODC!”
                </p>
                <Alert tone="outline" icon={Info} title="Why this is mandatory">
                    Two-factor authentication is mandatory for all accounts, including administrators. Even if someone learns your password,
                    they cannot sign in without your phone.
                </Alert>
                <p>
                    If you don't link the authenticator within 10 minutes of saving the password, the password already works: sign in on the
                    sign-in page with your new password — the system continues with linking the authenticator.
                </p>
            </Topic>
            <Topic id="account-sign-in">
                <Steps>
                    <li>
                        Enter your <Ui>Email</Ui> and <Ui>Password</Ui>.
                    </li>
                    <li>
                        If you wish, check <Ui>Remember me on this device</Ui> — the sign-in is then kept after the browser is closed
                        (without it, the sign-in ends as soon as you close the browser). Don't do this on shared computers.
                    </li>
                    <li>
                        Click <Ui>Continue</Ui>.
                    </li>
                    <li>
                        On the <Ui>Authenticator code</Ui> page, enter the 6-digit code from the app in the <Ui>Verification code</Ui> field
                        and click <Ui>Sign in</Ui>.
                    </li>
                </Steps>
                <p>
                    Each code can be used <strong>only once</strong>. If a code has just been used (for example, you signed out and are
                    signing in again right away), wait for the next code in the app — the system will say “The code is incorrect or no
                    longer valid. Try the next code.”
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
            </Topic>
            <Topic id="account-recovery">
                <p>
                    If you don't have your phone at hand, sign in with one of the recovery codes you received when setting up the
                    authenticator.
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
            </Topic>
            <Topic id="account-lockout">
                <p>
                    After <strong>5 failed attempts</strong> in a row (a wrong password, authenticator code or recovery code), sign-in to
                    the account is temporarily blocked for <strong>15 minutes</strong>. A wrong current password when changing your password
                    and wrong verification codes in the profile also count towards the limit.
                </p>
                <p>
                    For security reasons, the sign-in page gives the same answer to any error: “Incorrect email or password. After several
                    failed attempts, sign-in is temporarily blocked.” — so outsiders cannot find out whether an account exists. If you are
                    sure of your password but cannot sign in, wait 15 minutes.
                </p>
                <p>
                    If your account has been <strong>blocked by an administrator</strong>, after entering the correct password you will see
                    “The account is blocked. Contact your administrator.”
                </p>
                <p>
                    An administrator can lift a temporary lockout earlier with the <Ui>Sign out all sessions</Ui> button on the user's page.
                </p>
            </Topic>
            <Topic id="account-forgot">
                <p>
                    ODC has no self-service password recovery — this way nobody can take over your access knowing only your email. You set a
                    new password after an administrator resets the old one.
                </p>
                <ul>
                    <li>
                        <strong>Forgot your password</strong> — contact the HR department or the system administrator. After the password is
                        reset, you receive the email “Your ODC password has been reset”. Open it (or click <Ui>Activate account</Ui> on the
                        sign-in page) and go through the activation: email → code from the email → new password → authenticator code (it
                        stays linked) — and you are in the cabinet (see “<Ref to="account-activation">Account activation</Ref>”).
                    </li>
                    <li>
                        <strong>Changed or lost your phone</strong> — sign in with a recovery code and ask the administrator to reset your
                        authenticator. You will set up the new phone the next time you sign in. If you have no recovery codes, the
                        administrator resets the authenticator and you set up a new one when you sign in.
                    </li>
                    <li>
                        <strong>Suspect someone else knows your password</strong> — change your password immediately: the sign-ins on all
                        other devices end within a few minutes (see “<Ref to="account-change-password">Changing your password</Ref>”).
                    </li>
                </ul>
            </Topic>
            <Topic id="account-change-password">
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
                    After the password is changed, <strong>the sign-ins on all other devices are ended</strong> within a few minutes — you
                    will need to sign in again there. You stay signed in in this browser.
                </p>
                <p>
                    If you don't remember your current password, you cannot change it here — ask an administrator to reset the password (see
                    “<Ref to="account-forgot">Forgot your password or changed your phone</Ref>”).
                </p>
            </Topic>
            <Topic id="account-sessions">
                <p>A sign-in works in the browser on the device where you signed in, and it ends automatically:</p>
                <ul>
                    <li>
                        after <strong>1 hour of inactivity</strong>;
                    </li>
                    <li>
                        no later than <strong>12 hours after sign-in</strong>, even if you have been working all the time — then just sign
                        in again;
                    </li>
                    <li>
                        when you close the browser, if you didn't check <Ui>Remember me on this device</Ui> when signing in.
                    </li>
                </ul>
                <p>
                    To sign out, click the sign-out button <Icon icon={LogOut} /> next to your name at the bottom of the side menu (on a
                    phone — in the <Icon icon={Menu} /> menu) and confirm “Sign out of the cabinet?”. This signs you out of this browser
                    only.
                </p>
                <p>
                    To end your sign-ins on other devices — for example, if you forgot to sign out on someone else's computer — click{" "}
                    <Ui>Sign out on other devices</Ui> in <Ui>Profile</Ui> → <Ui>Security</Ui> (see “
                    <Ref to="profile-sessions">Signing out on other devices</Ref>”).
                </p>
                <p>
                    Sign-ins on other devices do not end instantly: a device loses access <strong>within a few minutes</strong> (at most 5
                    minutes, when its access token is renewed). This happens when you change your password or sign out on other devices, and
                    also when an administrator blocks the account, resets the password or the authenticator, or signs out all of the user's
                    sessions.
                </p>
            </Topic>
            <Topic id="account-security-tips">
                <ul>
                    <li>
                        Don't use your ODC password for other services. A password manager is the most convenient way to store passwords.
                    </li>
                    <li>
                        Never share your password, activation codes from emails, authenticator codes or recovery codes with anyone — ODC
                        staff never ask for them.
                    </li>
                    <li>On shared computers, don't check “Remember me” and always sign out when you finish.</li>
                    <li>
                        Forgot to sign out on someone else's computer or lost your phone — click <Ui>Sign out on other devices</Ui> on the{" "}
                        <Ui>Security</Ui> tab of your profile.
                    </li>
                    <li>
                        The private key of your electronic signature and its password are separate from your ODC account; they are never
                        sent to the server (see the “<Ref to="signing">Signing and verifying files</Ref>” chapter).
                    </li>
                </ul>
            </Topic>
        </Chapter>
    );
}
