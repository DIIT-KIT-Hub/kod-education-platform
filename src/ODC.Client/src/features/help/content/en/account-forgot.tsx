import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "account-forgot" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                ODC has no self-service password recovery — this way nobody can take over your access knowing only your email. You set a new
                password after an administrator resets the old one.
            </p>
            <ul>
                <li>
                    <strong>Forgot your password</strong> — contact the HR department or the system administrator. After the password is
                    reset, you receive the email “Your ODC password has been reset”. Open it (or click <Ui>Activate account</Ui> on the
                    sign-in page) and go through the activation: email → code from the email → new password → authenticator code (it stays
                    linked) — and you are in the cabinet (see “<Ref to="account-activation">Account activation</Ref>”).
                </li>
                <li>
                    <strong>Changed or lost your phone</strong> — sign in with a recovery code and ask the administrator to reset your
                    authenticator. You will set up the new phone the next time you sign in. If you have no recovery codes, the administrator
                    resets the authenticator and you set up a new one when you sign in.
                </li>
                <li>
                    <strong>Suspect someone else knows your password</strong> — change your password immediately: the sign-ins on all other
                    devices end within a few minutes (see “<Ref to="account-change-password">Changing your password</Ref>”).
                </li>
            </ul>
        </>
    );
}
