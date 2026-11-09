import adminUserNewImage from "../../images/en/admin-user-new.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "admin-users-create" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Click <Ui>Add</Ui>.
                </li>
                <li>
                    Fill in “Personal details”: <Ui>Last name</Ui>, <Ui>First name</Ui> (required), <Ui>Middle name</Ui>, <Ui>Phone</Ui>,{" "}
                    <Ui>Email (sign-in)</Ui> (required, unique).
                </li>
                <li>
                    Add at least one position (see “<Ref to="admin-users-edit">Personal details and positions</Ref>”) — without a position
                    the user won't have access to the modules.
                </li>
                <li>
                    Click <Ui>Create account</Ui> — “The account has been created. The user will get an invitation e-mail and activate it.”
                </li>
            </Steps>
            <Figure
                src={adminUserNewImage}
                alt="The new user form"
                caption="A new user: the details, positions and roles. No password: the person sets it themself."
            />
            <p>
                The account is created <strong>without a password</strong>: administrators neither issue nor see passwords. An email “Your
                ODC account has been created” with an <Ui>Activate the account</Ui> button arrives at the given address. The user confirms
                the email with a one-time code, chooses their own password, links an authenticator and adds a photo (see “
                <Ref to="account-activation">Account activation</Ref>”). Until then, the user's page shows the “Awaiting activation” badge.
            </p>
            <p>
                If the email got lost, click <Ui>Send the invitation again</Ui> in the “Account security” card (see “
                <Ref to="admin-users-security">Account security: blocking, password, 2FA, sessions</Ref>”). The account can also be
                activated without the email — with the <Ui>Activate account</Ui> button on the sign-in page.
            </p>
            <p>
                Possible errors: “A user with this email already exists.”, “Enter a valid email address.”, “Select a unit and a role for
                each position.”, “You can assign positions only in units of organizations you manage.”, “Assign at least one position in a
                unit of your organization…”
            </p>
        </>
    );
}
