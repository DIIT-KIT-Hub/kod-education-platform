import { Menu } from "lucide-react";
import { Badge } from "@/shared/ui/Badge";
import { Icon } from "@/shared/ui/Icon";
import { Chapter, Ref, Steps, Topic, Ui } from "../../components/Manual";

/**
 * Structure (English).
 * @returns The chapter.
 */
export function StructureChapter() {
    return (
        <Chapter id="structure">
            <Topic id="structure-view">
                <p>
                    Open <Ui>Structure</Ui> in the menu (on a phone — via the <Icon icon={Menu} /> menu). The page shows a tree:{" "}
                    <strong>organizations</strong> are at the top level, and below them are their units of any depth (departments,
                    directorates, divisions, sections, branches, etc.).
                </p>
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
                    <Ref to="structure-types">Unit types</Ref>” catalog, so the structure speaks the language of your organization — a
                    company, an institution, a school or a medical facility, a nonprofit.
                </p>
                <p>
                    The unit determines on whose behalf documents are created and the scope of document visibility for managers (see “
                    <Ref to="documents-visibility">Whose documents you see</Ref>”).
                </p>
            </Topic>
            <Topic id="structure-organizations">
                <p>
                    One cabinet can serve <strong>several organizations</strong> — for example, a group of companies, a parent institution
                    with subsidiaries, or several independent organizations on a shared server.
                </p>
                <ul>
                    <li>
                        An <strong>organization</strong> is a unit whose type is marked “This is an organization”. Only such units can be at
                        the top level of the tree.
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
                        The <strong>“Organization documents”</strong> permission opens the documents of the entire organization where you
                        hold a position, with all its units, but not the documents of other organizations.
                    </li>
                    <li>
                        Each organization manages its own users, structure and news: administrative permissions apply only in the
                        organizations of your positions (see “<Ref to="admin-scope">Administration within organizations</Ref>”).
                    </li>
                </ul>
                <p>
                    A person can hold positions in different organizations at the same time. News belongs to an organization or to the whole
                    cabinet, and everyone can read it. Roles, the unit type catalog and the audit log are shared by the whole cabinet.
                </p>
            </Topic>
            <Topic id="structure-unit">
                <ul>
                    <li>
                        At the top — the path from the organization to the unit, the unit type, code, short name (if any), the organization
                        it belongs to, and the “Inactive” badge.
                    </li>
                    <li>Cards of nested units with their type and number of employees.</li>
                    <li>
                        The <strong>Employees</strong> block: search by last and first name, columns “Employee”, “Position”, “Email” (click
                        to write an email), “Phone”. Managers are listed first — holders of roles marked as “Managerial position” (with the{" "}
                        <Badge tone="inverse">Head</Badge> badge next to the name), then everyone else by last name. A secondary position is
                        marked “(secondary)”.
                    </li>
                    <li>
                        With the “Manage structure” permission, the pages of units in your organizations have <Ui>Add unit</Ui> (a new unit
                        inside the current one) and <Ui>Edit</Ui> buttons.
                    </li>
                </ul>
                <p>You can open your own positions right from your profile — each position links to the page of its unit.</p>
            </Topic>
            <Topic id="structure-manage">
                <p>
                    The <Ui>Manage structure</Ui> button on the structure page (or <Ui>Administration</Ui> → <Ui>Structure</Ui>) opens the
                    “Organizations and units” table.
                </p>
                <ul>
                    <li>
                        The table lists the organizations whose structure you manage (an administrator of all organizations sees the whole
                        cabinet). Search by name or code, filter by organization (when there are several) and by type; columns “Name” (and
                        the parent unit), “Code”, “Type”, “Employees”, “Status”.
                    </li>
                    <li>
                        <Ui>Tree</Ui> returns to the structure view; <Ui>Unit types</Ui> opens the type catalog.
                    </li>
                    <li>
                        <Ui>New organization</Ui> creates a top-level organization — only an administrator of all organizations sees this
                        button. <Ui>New unit</Ui> creates a unit within your organization; a subsidiary organization within your
                        organization can be created as a unit with an organization type.
                    </li>
                    <li>
                        You can edit with the <Ui>Edit</Ui> button in the table or on the unit's page.
                    </li>
                </ul>
                <h4>How to create an organization from scratch</h4>
                <Steps>
                    <li>
                        Make sure the “<Ref to="structure-types">Unit types</Ref>” catalog has the levels you need (for example,
                        “Organization”, “Department”, “Division”) — add your own if needed.
                    </li>
                    <li>
                        Click <Ui>New organization</Ui> and enter the name, code and a type marked as an organization. Leave the{" "}
                        <Ui>Parent unit</Ui> field empty.
                    </li>
                    <li>
                        Open the new organization in the tree and add units with the <Ui>Add unit</Ui> button — the parent unit is filled in
                        automatically.
                    </li>
                    <li>
                        Assign employees to positions in the new units (see “<Ref to="admin-users-edit">Personal details and positions</Ref>
                        ”).
                    </li>
                </Steps>
                <h4>Unit fields</h4>
                <ul>
                    <li>
                        <Ui>Full name</Ui> (required, up to 256 characters) and <Ui>Short name</Ui> (up to 64).
                    </li>
                    <li>
                        <Ui>Code</Ui> (required, up to 32 characters): Latin letters, digits, “-” and “_”; automatically converted to
                        uppercase. The code is unique across all organizations and is used in user import (the unit code column, “Код
                        підрозділу”) — change it with care.
                    </li>
                    <li>
                        <Ui>Type</Ui> (required) — from the list of types; types are grouped into “Organizations” and “Units”. The{" "}
                        <Ui>Add it</Ui> link below the field opens the creation of a new type.
                    </li>
                    <li>
                        <Ui>Parent unit</Ui> — what the unit is part of; the list contains only units of organizations whose structure you
                        manage. The “— none (top-level organization) —” option is available only for organization types and only to an
                        administrator of all organizations (or for an existing top-level organization); a unit cannot be nested in itself or
                        in one of its own nested units.
                    </li>
                    <li>
                        <Ui>Display order</Ui> — the number by which units of the same level are sorted.
                    </li>
                    <li>
                        <Ui>Active</Ui> — clear it to “close” a unit. Inactive units are kept for the document history.
                    </li>
                </ul>
                <p>
                    Click <Ui>Save</Ui> — “Unit saved”. A unit cannot be deleted, only made inactive.
                </p>
                <p>
                    Possible errors: “A unit with code “X” already exists.”, “Select a unit type.”, “Only organizations can be at the top
                    level of the structure…”, “A unit cannot be nested in itself.”, “Only Latin letters, digits, “-” and “_”.”, “The unit
                    belongs to an organization whose structure you don't manage.”, “Only an administrator of all organizations can create
                    and move top-level organizations…”, “Only an administrator of all organizations can turn an organization into a regular
                    unit…”
                </p>
                <p>
                    A unit of another organization opens read-only: “The unit belongs to an organization whose structure you don't manage,
                    so you can only view it.”
                </p>
            </Topic>
            <Topic id="structure-types">
                <p>
                    <Ui>Administration</Ui> → <Ui>Unit types</Ui> (or the <Ui>Unit types</Ui> button above the units table) — the catalog of
                    structure levels. On first launch, default types are created: “Organization”, “Department”, “Directorate”, “Division”,
                    “Section”, “Service”, “Group / team”, “Branch / representative office”. You can rename or delete them or add your own —
                    “Faculty department”, “Ward”, “Workshop”, “Clinic”, “Chapter”, etc.
                </p>
                <p>
                    The catalog is shared by all organizations in the cabinet, so only an administrator of all organizations can change it;
                    administrators of individual organizations see the catalog and use its types.
                </p>
                <ul>
                    <li>
                        The table shows the type with its icon and description, the code, the level (“Organization” / “Unit”) and the number
                        of units of this type.
                    </li>
                    <li>
                        <Ui>New type</Ui> — create a type; <Ui>Edit</Ui> — change it.
                    </li>
                </ul>
                <h4>Type fields</h4>
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
                    You can <Ui>Delete type</Ui> only when no unit uses it; otherwise, change the type of those units first. All type
                    changes are recorded in the audit log.
                </p>
            </Topic>
        </Chapter>
    );
}
