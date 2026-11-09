import adminUnitsImage from "../../images/en/admin-units.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "structure-manage" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The <Ui>Manage structure</Ui> button on the structure page (or <Ui>Administration</Ui> → <Ui>Structure</Ui>) opens the
                “Organizations and units” table.
            </p>
            <Figure src={adminUnitsImage} alt="Managing the units" caption="Administration → Units." />
            <ul>
                <li>
                    The table lists the organizations whose structure you manage (an administrator of all organizations sees the whole
                    cabinet). Search by name or code, filter by organization (when there are several) and by type; columns “Name” (and the
                    parent unit), “Code”, “Type”, “Employees”, “Status”.
                </li>
                <li>
                    <Ui>Tree</Ui> returns to the structure view; <Ui>Unit types</Ui> opens the type catalog.
                </li>
                <li>
                    <Ui>New organization</Ui> creates a top-level organization — only an administrator of all organizations sees this
                    button. <Ui>New unit</Ui> creates a unit within your organization; a subsidiary organization within your organization
                    can be created as a unit with an organization type.
                </li>
                <li>
                    You can edit with the <Ui>Edit</Ui> button in the table or on the unit's page.
                </li>
            </ul>
            <h2>How to create an organization from scratch</h2>
            <Steps>
                <li>
                    Make sure the “<Ref to="structure-types">Unit types</Ref>” catalog has the levels you need (for example, “Organization”,
                    “Department”, “Division”) — add your own if needed.
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
            <h2>Unit fields</h2>
            <ul>
                <li>
                    <Ui>Full name</Ui> (required, up to 256 characters) and <Ui>Short name</Ui> (up to 64).
                </li>
                <li>
                    <Ui>Code</Ui> (required, up to 32 characters): Latin letters, digits, “-” and “_”; automatically converted to uppercase.
                    The code is unique across all organizations and is used in user import (the unit code column, “Код підрозділу”) — change
                    it with care.
                </li>
                <li>
                    <Ui>Type</Ui> (required) — from the list of types; types are grouped into “Organizations” and “Units”. The{" "}
                    <Ui>Add it</Ui> link below the field opens the creation of a new type.
                </li>
                <li>
                    <Ui>Parent unit</Ui> — what the unit is part of; the list contains only units of organizations whose structure you
                    manage. The “— none (top-level organization) —” option is available only for organization types and only to an
                    administrator of all organizations (or for an existing top-level organization); a unit cannot be nested in itself or in
                    one of its own nested units.
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
                Possible errors: “A unit with code “X” already exists.”, “Select a unit type.”, “Only organizations can be at the top level
                of the structure…”, “A unit cannot be nested in itself.”, “Only Latin letters, digits, “-” and “_”.”, “The unit belongs to
                an organization whose structure you don't manage.”, “Only an administrator of all organizations can create and move
                top-level organizations…”, “Only an administrator of all organizations can turn an organization into a regular unit…”
            </p>
            <p>
                A unit of another organization opens read-only: “The unit belongs to an organization whose structure you don't manage, so
                you can only view it.”
            </p>
        </>
    );
}
