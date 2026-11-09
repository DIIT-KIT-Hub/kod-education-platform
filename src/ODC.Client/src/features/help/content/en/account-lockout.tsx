import { Ui } from "../../components/Manual";

/**
 * The manual page "account-lockout" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                After <strong>5 failed attempts</strong> in a row (a wrong password, authenticator code or recovery code), sign-in to the
                account is temporarily blocked for <strong>15 minutes</strong>. A wrong current password when changing your password and
                wrong verification codes in the profile also count towards the limit.
            </p>
            <p>
                For security reasons, the sign-in page gives the same answer to any error: “Incorrect email or password. After several
                failed attempts, sign-in is temporarily blocked.” — so outsiders cannot find out whether an account exists. If you are sure
                of your password but cannot sign in, wait 15 minutes.
            </p>
            <p>
                If your account has been <strong>blocked by an administrator</strong>, after entering the correct password you will see “The
                account is blocked. Contact your administrator.”
            </p>
            <p>
                An administrator can lift a temporary lockout earlier with the <Ui>Sign out all sessions</Ui> button on the user's page.
            </p>
        </>
    );
}
