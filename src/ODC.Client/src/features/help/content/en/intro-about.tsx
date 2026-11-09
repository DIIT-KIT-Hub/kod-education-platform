/**
 * The manual page "intro-about" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <strong>ODC — the Organizational Documents Cabinet</strong> — is an electronic document management system for an
                organization of any kind: a company, a government agency, a school or university, a hospital, a non-profit. The structure
                (organizations, their units and the names of the levels) is set up by an administrator, so the cabinet speaks the language
                of your organization. In it, employees and external users:
            </p>
            <ul>
                <li>
                    create documents (orders, instructions, memos, contracts, certificates, applications), build a signing route and send
                    them for signing;
                </li>
                <li>
                    sign documents with a <strong>qualified electronic signature</strong> right in the browser — the key and its password
                    never leave the device;
                </li>
                <li>sign and verify any files;</li>
                <li>read news and announcements;</li>
                <li>browse the organizational structure: organizations, their departments, divisions, branches and employees;</li>
                <li>manage their own profile, password, two-factor authentication and sign-ins on other devices.</li>
            </ul>
            <p>
                Administrators and responsible employees also manage users (including import and export), roles and permissions,
                organizations and their structure, unit types and news, and review the audit log.
            </p>
            <p>
                The cabinet works in any modern browser (Chrome, Edge, Firefox, Safari) on a computer, tablet or phone. Nothing needs to be
                installed.
            </p>
        </>
    );
}
