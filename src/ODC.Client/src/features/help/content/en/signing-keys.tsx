import { Info } from "lucide-react";
import { Alert } from "@/shared/ui/Alert";
import { Code, Table, Ui } from "../../components/Manual";

/**
 * The manual page "signing-keys" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <strong>File keys</strong> from all qualified trust service providers (QTSPs) on the Central Certification Authority's list
                are supported: the State Tax Service, Diia, PrivatBank, monobank, Oschadbank, PUMB, Ukrgasbank, DepositSign, Vchasno,
                MASTERKEY, the Treasury and others — more than 30 providers.
            </p>
            <Table>
                <thead>
                    <tr>
                        <th>File type</th>
                        <th>Where it comes from</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Key-6.dat</td>
                        <td>
                            The classic key of most providers (State Tax Service, Diia and others). Certificates are found automatically.
                        </td>
                    </tr>
                    <tr>
                        <td>*.pfx, *.p12</td>
                        <td>A PKCS#12 container (often PrivatBank and bank QTSPs).</td>
                    </tr>
                    <tr>
                        <td>*.jks</td>
                        <td>Java KeyStore (PrivatBank and others).</td>
                    </tr>
                    <tr>
                        <td>*.pk8, *.zs2</td>
                        <td>Other key container formats.</td>
                    </tr>
                </tbody>
            </Table>
            <p>
                Usually leave the <Ui>Trust service provider</Ui> field set to <Ui>Detect automatically</Ui>. Select a specific provider
                only if automatic detection didn't work.
            </p>
            <p>
                If the provider doesn't support automatic certificate download, expand{" "}
                <Ui>Add certificates manually (if the provider doesn't support automatic download)</Ui> and add the <Code>*.cer</Code>,{" "}
                <Code>*.crt</Code> or <Code>*.p7b</Code> certificate files issued to you by the provider.
            </p>
            <Alert tone="outline" icon={Info} title="Hardware tokens and cloud keys">
                The “Hardware token” options (Almaz-1K, Krystal-1 tokens, etc.) and “Cloud / Diia.Signature” are shown as disabled: they
                require separate provider components (the IIT web agent or a cloud signing service) that are not connected to ODC at the
                moment.
            </Alert>
        </>
    );
}
