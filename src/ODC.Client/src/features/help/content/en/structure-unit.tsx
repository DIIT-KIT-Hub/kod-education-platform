import { Badge } from "@/shared/ui/Badge";
import { Figure, Ui } from "../../components/Manual";
import structureUnitImage from "../../images/en/structure-unit.webp";

/**
 * The manual page "structure-unit" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    At the top — the path from the organization to the unit, the unit type, code, short name (if any), the organization it
                    belongs to, and the “Inactive” badge.
                </li>
                <li>Cards of nested units with their type and number of employees.</li>
                <li>
                    The <strong>Employees</strong> block: search by last and first name, columns “Employee”, “Position”, “Email” (click to
                    write an email), “Phone”. Managers are listed first — holders of roles marked as “Managerial position” (with the{" "}
                    <Badge tone="inverse">Head</Badge> badge next to the name), then everyone else by last name. A secondary position is
                    marked “(secondary)”.
                </li>
                <li>
                    With the “Manage structure” permission, the pages of units in your organizations have <Ui>Add unit</Ui> (a new unit
                    inside the current one) and <Ui>Edit</Ui> buttons.
                </li>
            </ul>
            <Figure src={structureUnitImage} alt="A unit page with its staff" caption="A unit page: the child units and the staff." />
            <p>You can open your own positions right from your profile — each position links to the page of its unit.</p>
        </>
    );
}
