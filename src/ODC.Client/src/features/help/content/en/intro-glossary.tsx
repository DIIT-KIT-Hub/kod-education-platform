import { Table } from "../../components/Manual";

/**
 * The manual page "intro-glossary" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Table>
                <thead>
                    <tr>
                        <th>Term</th>
                        <th>Meaning</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>QES</td>
                        <td>Qualified electronic signature — the equivalent of a handwritten signature with the same legal force.</td>
                    </tr>
                    <tr>
                        <td>Private key</td>
                        <td>A file (for example, Key-6.dat) used to apply a QES. It is protected by your password, which only you know.</td>
                    </tr>
                    <tr>
                        <td>QTSP / provider</td>
                        <td>
                            Qualified trust service provider that issued your key and certificate (the State Tax Service, Diia, banks,
                            etc.).
                        </td>
                    </tr>
                    <tr>
                        <td>Certificate</td>
                        <td>
                            The public part of the key: who the owner is (full name, taxpayer number, organization), who issued it and until
                            when it is valid.
                        </td>
                    </tr>
                    <tr>
                        <td>Signing route</td>
                        <td>The list of people who must sign a document and the order of signing.</td>
                    </tr>
                    <tr>
                        <td>Registration number</td>
                        <td>A unique number the system assigns to a document.</td>
                    </tr>
                    <tr>
                        <td>SHA-256</td>
                        <td>
                            The “fingerprint” of a file. Any change to the file changes the fingerprint; before signing, the browser
                            compares it with the registered one, so a modified document cannot be signed.
                        </td>
                    </tr>
                    <tr>
                        <td>2FA / authenticator</td>
                        <td>
                            Two-factor authentication: in addition to the password, sign-in is confirmed with a one-time code from an app on
                            your phone.
                        </td>
                    </tr>
                    <tr>
                        <td>Recovery codes</td>
                        <td>One-time codes for signing in without your phone.</td>
                    </tr>
                    <tr>
                        <td>Session (sign-in)</td>
                        <td>
                            A sign-in from a particular browser on a particular device. It ends after 1 hour of inactivity, no later than 12
                            hours after sign-in, or when you sign out. Sign-ins on other devices can be ended on the “Security” tab of your
                            profile.
                        </td>
                    </tr>
                    <tr>
                        <td>Role / permission</td>
                        <td>
                            A permission is the right to perform a specific action; a role is a set of permissions assigned together with a
                            position.
                        </td>
                    </tr>
                    <tr>
                        <td>Organization</td>
                        <td>
                            A top-level (or nested) unit whose type is marked as an organization: a company, agency, institution,
                            foundation. One cabinet can contain several.
                        </td>
                    </tr>
                    <tr>
                        <td>Unit / unit type</td>
                        <td>
                            A part of an organization (department, division, sector, branch…). The type sets the level name and icon and is
                            configured by an administrator.
                        </td>
                    </tr>
                    <tr>
                        <td>Audit log</td>
                        <td>A record of all important actions and security events: who did what and when.</td>
                    </tr>
                </tbody>
            </Table>
        </>
    );
}
