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
            <Topic id="account-first-login">
                <p>
                    There is no self-registration in KOD: accounts are created by the HR department or the system administrator. They give
                    you an <strong>email address</strong> (your login) and a <strong>temporary password</strong>. The first sign-in has
                    three parts and takes 3–5 minutes.
                </p>
                <p>
                    Have a smartphone ready with an authenticator app: <strong>Google Authenticator</strong>,{" "}
                    <strong>Microsoft Authenticator</strong> or any other app that supports TOTP one-time codes (for example, 1Password,
                    Bitwarden, Authy).
                </p>
                <h4>Part 1. The password from the administrator</h4>
                <Steps>
                    <li>
                        Open the sign-in page and enter your <Ui>Email</Ui> and <Ui>Password</Ui> — the temporary password you were given.
                    </li>
                    <li>
                        Click <Ui>Continue</Ui>.
                    </li>
                </Steps>
                <h4>Part 2. Your own password (“Step 1 of 2”)</h4>
                <Steps>
                    <li>
                        The system opens the <Ui>Create your own password</Ui> page. The temporary password is valid for the first sign-in
                        only.
                    </li>
                    <li>
                        Enter a <Ui>New password</Ui> and <Ui>Repeat password</Ui>. Requirements: 12 to 128 characters, uppercase and
                        lowercase letters and digits, at least 5 different characters. The new password must differ from the temporary one.
                    </li>
                    <li>
                        Click <Ui>Save password</Ui>. If the password appears in known data breaches, the system asks you to choose another
                        one — this protects you from guessing attacks based on “popular” passwords.
                    </li>
                </Steps>
                <h4>Part 3. The authenticator (“Step 2 of 2”)</h4>
                <Steps>
                    <li>
                        On the <Ui>Protect your sign-in</Ui> page, open the authenticator app on your phone and add a new account.
                    </li>
                    <li>
                        Scan the QR code with your phone camera. If scanning is inconvenient, choose manual entry in the app and type the
                        key shown under the QR code (the <Ui>Copy</Ui> button copies it).
                    </li>
                    <li>
                        A “KOD” entry with a 6-digit code that changes every 30 seconds appears in the app. Enter the current code in the{" "}
                        <Ui>Code from the app</Ui> field and click <Ui>Confirm and sign in</Ui>.
                    </li>
                    <li>
                        The system shows <strong>10 recovery codes</strong>. Click <Ui>Copy codes</Ui> and keep them in a safe place (a
                        password manager, a printed sheet in a safe). They will not be shown again.
                    </li>
                    <li>
                        Click <Ui>Go to the cabinet</Ui>.
                    </li>
                </Steps>
                <h4>Part 4. Profile photo</h4>
                <p>
                    The cabinet opens the <Ui>Add a profile photo</Ui> page: choose a photo, place the face inside the circle and click{" "}
                    <Ui>Save photo</Ui> (see “<Ref to="profile-photo">Profile photo</Ref>”). The other sections are unavailable without a
                    photo.
                </p>
                <Alert tone="outline" icon={Info} title="Why this is mandatory">
                    Two-factor authentication is mandatory for all accounts, including administrators. Even if someone learns your password,
                    they cannot sign in without your phone.
                </Alert>
                <p>
                    If you don't finish setting up the authenticator within 10 minutes, the page returns you to the sign-in page — just sign
                    in again, this time with your own password.
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
                <p>KOD has no password recovery by email — this way nobody can take over your access through your mailbox.</p>
                <ul>
                    <li>
                        <strong>Forgot your password</strong> — contact the HR department or the system administrator. You will get a new
                        temporary password; when you sign in, the system asks you to replace it with your own.
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
                    If an administrator gave you a temporary password and your authenticator is already set up, after signing in with a code
                    the system immediately opens the password change page: “You signed in with a temporary password. Set your own password
                    to continue.” Other pages are unavailable until you change the password.
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
                    also when an administrator blocks the account, issues a temporary password, resets the authenticator or signs out all of
                    the user's sessions.
                </p>
            </Topic>
            <Topic id="account-security-tips">
                <ul>
                    <li>
                        Don't use your KOD password for other services. A password manager is the most convenient way to store passwords.
                    </li>
                    <li>Never share authenticator codes or recovery codes with anyone — KOD staff never ask for them.</li>
                    <li>On shared computers, don't check “Remember me” and always sign out when you finish.</li>
                    <li>
                        Forgot to sign out on someone else's computer or lost your phone — click <Ui>Sign out on other devices</Ui> on the{" "}
                        <Ui>Security</Ui> tab of your profile.
                    </li>
                    <li>
                        The private key of your electronic signature and its password are separate from your KOD account; they are never
                        sent to the server (see the “<Ref to="signing">Signing and verifying files</Ref>” chapter).
                    </li>
                </ul>
            </Topic>
        </Chapter>
    );
}
