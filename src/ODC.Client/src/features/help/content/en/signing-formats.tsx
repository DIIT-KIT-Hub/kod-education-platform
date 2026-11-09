import { Code, Table } from "../../components/Manual";

/**
 * The manual page "signing-formats" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Table>
                <thead>
                    <tr>
                        <th>Format</th>
                        <th>Result</th>
                        <th>When to choose</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>CAdES</td>
                        <td>
                            <Code>*.p7s</Code>
                        </td>
                        <td>
                            A universal format for any single file. “Detached” — the signature file sits next to the original; “Enveloped” —
                            a single file containing both the document and the signature. The most common format in Ukraine.
                        </td>
                    </tr>
                    <tr>
                        <td>XAdES</td>
                        <td>
                            <Code>*.xml</Code>
                        </td>
                        <td>An XML signature; several files can be signed at once. Needed if the recipient requires XML.</td>
                    </tr>
                    <tr>
                        <td>PAdES</td>
                        <td>
                            <Code>*_signed.pdf</Code>
                        </td>
                        <td>A signature inside the PDF — the document opens in a regular PDF viewer. Only for a single PDF file.</td>
                    </tr>
                    <tr>
                        <td>ASiC-S</td>
                        <td>
                            <Code>*.asics</Code>
                        </td>
                        <td>A container (ZIP) with one file and its signature. A European standard.</td>
                    </tr>
                    <tr>
                        <td>ASiC-E</td>
                        <td>
                            <Code>*.asice</Code>
                        </td>
                        <td>A container with several files and one signature covering all of them.</td>
                    </tr>
                </tbody>
            </Table>
            <Table>
                <thead>
                    <tr>
                        <th>Level</th>
                        <th>What it contains</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>B-B — basic</td>
                        <td>Only the signature and the certificate. The signing time comes from your computer's clock.</td>
                    </tr>
                    <tr>
                        <td>B-T — with a timestamp</td>
                        <td>Adds a qualified timestamp from the provider: a precise, provable signing time.</td>
                    </tr>
                    <tr>
                        <td>B-LT — with long-term validation data</td>
                        <td>
                            Plus proof that the certificate was valid at the moment of signing, and the certificate chain. The signature can
                            be verified years later, even after the certificate has expired. <strong>Recommended.</strong>
                        </td>
                    </tr>
                    <tr>
                        <td>B-LTA — archival</td>
                        <td>Plus an archival timestamp for long-term storage over many years.</td>
                    </tr>
                </tbody>
            </Table>
        </>
    );
}
