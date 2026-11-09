import adminUnitTypesImage from "../../images/en/admin-unit-types.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "structure-types" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Unit types</Ui> (or the <Ui>Unit types</Ui> button above the units table) — the catalog of
                structure levels. On first launch, default types are created: “Organization”, “Department”, “Directorate”, “Division”,
                “Section”, “Service”, “Group / team”, “Branch / representative office”. You can rename or delete them or add your own —
                “Faculty department”, “Ward”, “Workshop”, “Clinic”, “Chapter”, etc.
            </p>
            <Figure src={adminUnitTypesImage} alt="Unit types" caption="Administration → Unit types." />
            <p>
                The catalog is shared by all organizations in the cabinet, so only an administrator of all organizations can change it;
                administrators of individual organizations see the catalog and use its types.
            </p>
            <ul>
                <li>
                    The table shows the type with its icon and description, the code, the level (“Organization” / “Unit”) and the number of
                    units of this type.
                </li>
                <li>
                    <Ui>New type</Ui> — create a type; <Ui>Edit</Ui> — change it.
                </li>
            </ul>
            <h2>Type fields</h2>
            <ul>
                <li>
                    <Ui>Name</Ui> (required, up to 128 characters, unique) — what the level is called in your organization.
                </li>
                <li>
                    <Ui>Code</Ui> (required, up to 32 characters): Latin letters, digits, “-”, “_”; converted to uppercase.
                </li>
                <li>
                    <Ui>Description</Ui> — a hint for administrators; <Ui>Order in lists</Ui> — the type's position in selection lists.
                </li>
                <li>
                    <Ui>This is an organization</Ui> — units of this type can be at the top level and limit the “Organization documents”
                    permission. The checkbox can't be cleared while there are top-level organizations of this type.
                </li>
                <li>
                    <Ui>Icon</Ui> — the icon that marks the type in the tree, tables and cards.
                </li>
            </ul>
            <p>
                You can <Ui>Delete type</Ui> only when no unit uses it; otherwise, change the type of those units first. All type changes
                are recorded in the audit log.
            </p>
        </>
    );
}
