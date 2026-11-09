/**
 * The manual page "faq-support" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    <strong>Account, full name, positions, forgotten password</strong> — the HR department.
                </li>
                <li>
                    <strong>Access to modules, authenticator reset, blocking, technical problems</strong> — the system administrator.
                </li>
                <li>
                    <strong>Signing route, document content, declined signing</strong> — the author of the document.
                </li>
                <li>
                    <strong>Electronic signature key, its password, validity period, revocation</strong> — your trust service provider (for
                    example, the State Tax Service, Diia or a bank). ODC does not store and cannot recover your key.
                </li>
            </ul>
            <p>
                When reporting a problem, include: what you were doing, the exact text of the message, the time and the browser. Important
                actions are recorded in the audit log, so the administrator can quickly find the event.
            </p>
        </>
    );
}
