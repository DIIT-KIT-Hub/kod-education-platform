import { Table } from "../../components/Manual";

/**
 * The manual page "admin-users-security" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>The “Account security” card on the user's page. Each action requires confirmation.</p>
            <Table>
                <thead>
                    <tr>
                        <th>Button</th>
                        <th>Required permission</th>
                        <th>What happens</th>
                        <th>When to use</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Block</td>
                        <td>Manage users</td>
                        <td>A new sign-in becomes impossible, the sign-ins on all devices end within a few minutes. “Account blocked.”</td>
                        <td>Dismissal, long absence, suspected compromise.</td>
                    </tr>
                    <tr>
                        <td>Unblock</td>
                        <td>Manage users</td>
                        <td>Sign-in is allowed again. “Account unblocked.”</td>
                        <td>The employee returns.</td>
                    </tr>
                    <tr>
                        <td>Send the invitation again</td>
                        <td>Account security</td>
                        <td>
                            The user gets the invitation email with the “Activate the account” button again. The button is shown only for
                            active accounts awaiting activation. “Invitation sent to ….”
                        </td>
                        <td>The user didn't receive or deleted the invitation email.</td>
                    </tr>
                    <tr>
                        <td>Reset password</td>
                        <td>Account security</td>
                        <td>
                            The password is removed, the sign-ins on all devices end within a few minutes. The user receives the email “Your
                            ODC password has been reset” and sets a new password through the activation (email → code → password → code from
                            the authenticator, which stays linked). Until then the account is marked “Awaiting activation”. The
                            administrator never sees the new password. “The password has been reset. The user will get an e-mail with
                            instructions.”
                        </td>
                        <td>The user forgot their password.</td>
                    </tr>
                    <tr>
                        <td>Reset authenticator</td>
                        <td>Account security</td>
                        <td>
                            2FA is turned off, recovery codes are revoked, the sign-ins on all devices end within a few minutes. On next
                            sign-in the user sets up a new phone. “Authenticator reset.”
                        </td>
                        <td>The user changed or lost their phone.</td>
                    </tr>
                    <tr>
                        <td>Sign out all sessions</td>
                        <td>Account security</td>
                        <td>
                            All of the user's sign-ins on all devices end within a few minutes; the temporary lockout after failed sign-in
                            attempts is also lifted. “Sessions signed out.”
                        </td>
                        <td>The user forgot to sign out on someone else's device; the account needs unlocking after failed sign-ins.</td>
                    </tr>
                </tbody>
            </Table>
            <p>“Within a few minutes” means at most 5 minutes.</p>
            <p>
                Before resetting a password or an authenticator, make sure the request really comes from the account owner (in person or via
                a known phone number). Below the card are the “Created” and “Last sign-in” dates. You cannot block your own account.
            </p>
        </>
    );
}
