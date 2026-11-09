/**
 * The manual page "documents-visibility" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    You <strong>always</strong> see documents you are the author of and documents whose route you are in.
                </li>
                <li>
                    With the <strong>“Unit documents”</strong> permission — also documents of the units of your positions and all nested
                    units (for example, a department director sees the documents of the department and all its divisions and sections).
                </li>
                <li>
                    With the <strong>“Organization documents”</strong> permission — documents of the entire organization where you hold a
                    position, with all its units (head of organization, deputies, records clerks). Documents of other organizations in the
                    cabinet are not visible.
                </li>
                <li>
                    With the <strong>“All documents”</strong> permission — documents of all organizations in the cabinet (usually only for
                    system administrators and auditors).
                </li>
            </ul>
            <p>A document you can't see is not shown in the registry, and a direct link to it shows “Document not found”.</p>
        </>
    );
}
