import { Code, Ui } from "../../components/Manual";

/**
 * The manual page "admin-export" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    In the user list, the <Ui>XLSX</Ui> / <Ui>CSV</Ui> buttons export users{" "}
                    <strong>with the current filters applied</strong> (search, status, unit, role).
                </li>
                <li>
                    The “Export” card on the import page exports <strong>all</strong> users of your organizations.
                </li>
            </ul>
            <p>
                Only positions in your organizations are exported — so the export can be imported back without errors about other
                organizations' units.
            </p>
            <p>
                The format matches the import (the same 9 columns, each position on a separate row, status “активний” / “заблокований”);
                passwords are not exported. So you can edit the export and import it back for bulk changes. The file name is{" "}
                <Code>users-YYYYMMDD-HHMM.xlsx</Code> or <Code>.csv</Code> (CSV uses the “;” delimiter and opens in Excel without any
                setup).
            </p>
        </>
    );
}
