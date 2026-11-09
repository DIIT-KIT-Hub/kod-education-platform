import { Menu } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Figure, Ref, Ui } from "../../components/Manual";
import structureImage from "../../images/en/structure.webp";

/**
 * The manual page "structure-view" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>Structure</Ui> in the menu (on a phone — via the <Icon icon={Menu} /> menu). The page shows a tree:{" "}
                <strong>organizations</strong> are at the top level, and below them are their units of any depth (departments, directorates,
                divisions, sections, branches, etc.).
            </p>
            <Figure
                src={structureImage}
                alt="The tree of organizations and units"
                caption="Structure: the tree of organizations and units."
            />
            <ul>
                <li>Expand and collapse branches to see nested units.</li>
                <li>
                    The <Ui>Unit name, abbreviation or code</Ui> box above the tree keeps only the matching units with the branches that
                    lead to them (those branches open).
                </li>
                <li>Next to each unit are the icon and name of its type and the number of employees.</li>
                <li>Click a name to open the unit's page.</li>
                <li>Inactive (dissolved) units are not shown in the tree.</li>
            </ul>
            <p>
                The names of the structure levels are not built into the system: the administrator defines them in the “
                <Ref to="structure-types">Unit types</Ref>” catalog, so the structure speaks the language of your organization — a company,
                an institution, a school or a medical facility, a nonprofit.
            </p>
            <p>
                The unit determines on whose behalf documents are created and the scope of document visibility for managers (see “
                <Ref to="documents-visibility">Whose documents you see</Ref>”).
            </p>
        </>
    );
}
