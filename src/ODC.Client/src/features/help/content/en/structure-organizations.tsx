import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "structure-organizations" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                One cabinet can serve <strong>several organizations</strong> — for example, a group of companies, a parent institution with
                subsidiaries, or several independent organizations on a shared server.
            </p>
            <ul>
                <li>
                    An <strong>organization</strong> is a unit whose type is marked “This is an organization”. Only such units can be at the
                    top level of the tree.
                </li>
                <li>
                    An organization can be nested in another one (for example, a subsidiary in a holding) — then it is also part of the
                    “parent” organization.
                </li>
                <li>
                    On the page of any unit, the <Ui>Organization: …</Ui> badge shows which organization it belongs to (the nearest one
                    above it in the tree).
                </li>
                <li>
                    The <strong>“Organization documents”</strong> permission opens the documents of the entire organization where you hold a
                    position, with all its units, but not the documents of other organizations.
                </li>
                <li>
                    Each organization manages its own users, structure and news: administrative permissions apply only in the organizations
                    of your positions (see “<Ref to="admin-scope">Administration within organizations</Ref>”).
                </li>
            </ul>
            <p>
                A person can hold positions in different organizations at the same time. News belongs to an organization or to the whole
                cabinet, and everyone can read it. Roles, the unit type catalog and the audit log are shared by the whole cabinet.
            </p>
        </>
    );
}
