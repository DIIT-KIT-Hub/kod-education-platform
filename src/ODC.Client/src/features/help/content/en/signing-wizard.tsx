import signingFilesImage from "../../images/en/signing-files.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "signing-wizard" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>Signing</Ui> in the menu. The <Ui>Sign</Ui> tab contains a three-step wizard. The top of the page shows which step
                you're on; you can return to a completed step with the <Ui>Back</Ui> button or by clicking its number.
            </p>
            <Figure src={signingFilesImage} alt="Signing files" caption="The “Signing” tool: sign any files on your computer." />
            <h2>Step 1. Files</h2>
            <Steps>
                <li>
                    Drag files into the <Ui>Drag files here or click to select</Ui> area, or click it and select the files.
                </li>
                <li>
                    A list of files with their sizes appears below the area. Each file can be up to <strong>25 MB</strong>.
                </li>
                <li>
                    Click <Ui>Next</Ui>. The button is enabled only when at least one file is selected.
                </li>
            </Steps>
            <h2>Step 2. Parameters</h2>
            <Steps>
                <li>
                    Choose the <Ui>Signature format</Ui>. Formats unavailable for the selected files are disabled (for example, PAdES is
                    only for PDF; CAdES and ASiC-S are only for a single file).
                </li>
                <li>
                    For CAdES and XAdES, choose the <Ui>Signature type</Ui>: <Ui>Detached</Ui> (the signature file sits next to the
                    document) or <Ui>Enveloped</Ui> (the data is inside the signature).
                </li>
                <li>
                    Choose the <Ui>Signature level</Ui>. <strong>B-LT</strong> is recommended — it contains everything needed for
                    verification years later.
                </li>
                <li>
                    Click <Ui>Next</Ui>. How to choose a format — see “<Ref to="signing-formats">Signature formats and levels</Ref>”.
                </li>
            </Steps>
            <h2>Step 3. Key and signature</h2>
            <Steps>
                <li>Check the summary at the top: which files, format and level will be used.</li>
                <li>
                    Key type — <Ui>File key</Ui>. Leave the provider set to <Ui>Detect automatically</Ui>.
                </li>
                <li>
                    Select the key file in the <Ui>Select the key file</Ui> area.
                </li>
                <li>
                    Enter the <Ui>Key password</Ui> and click <Ui>Read key</Ui> (or press Enter). The password is cleared from the field
                    immediately.
                </li>
                <li>
                    The key owner card appears: full name, organization, position, individual tax number / EDRPOU code, provider,
                    certificate number and validity period. Make sure this is your key. You can choose a different key with the{" "}
                    <Ui>Another key</Ui> button.
                </li>
                <li>
                    Click <Ui>Sign</Ui>.
                </li>
                <li>
                    After successful signing, “File signed” appears with the format, level and time. Click <Ui>Download …</Ui> to save the
                    result. To sign other files, click <Ui>Sign other files</Ui> — you'll need to read the key again.
                </li>
            </Steps>
            <p>The link to the signed file is valid for 10 minutes — save the file right away.</p>
        </>
    );
}
