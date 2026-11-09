import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "signing-how" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                ODC applies a <strong>qualified electronic signature (QES)</strong> using IIT's official “CSK User” library — the same one
                used by the Diia portal (ID.GOV.UA). The signature has the same legal force as a handwritten one.
            </p>
            <ul>
                <li>
                    <strong>The key and password never leave your device.</strong> The key file is read in the browser, in a separate
                    protected part of the page. Neither the key nor its password is sent to the server.
                </li>
                <li>
                    Only service requests go to the provider through the cabinet: whether your certificate is valid and the exact time
                    stamp. The document itself is never sent to the provider.
                </li>
                <li>
                    The key is released from memory right after signing, and also when you go back in the wizard, close the signing window
                    or leave the page.
                </li>
                <li>
                    The list of providers and their certificates is updated automatically, so keys with new or reissued certificates work
                    without any extra setup.
                </li>
            </ul>
            <p>Where the signature is used:</p>
            <ul>
                <li>
                    <strong>Signing documents</strong> in the “Documents” module — the signature is stored in the system together with the
                    document (see “<Ref to="documents-sign">Signing a document</Ref>”).
                </li>
                <li>
                    <strong>Signing any files</strong> on the <Ui>Signing</Ui> page — you download the result to your device; the files
                    themselves are not stored in the system. Only the fact of signing goes to the audit log: the file names, the format and
                    the certificate. Save the signed file right away — it cannot be downloaded from the system later. If you need a history
                    with the files, create a document in the Documents module.
                </li>
                <li>
                    <strong>Verifying signed files</strong> on the <Ui>Verify</Ui> tab.
                </li>
            </ul>
        </>
    );
}
