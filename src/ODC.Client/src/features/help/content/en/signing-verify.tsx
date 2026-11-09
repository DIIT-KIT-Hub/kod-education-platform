import signingVerifyImage from "../../images/en/signing-verify.webp";
import { Code, Figure, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "signing-verify" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Open <Ui>Signing</Ui> → the <Ui>Verify</Ui> tab.
                </li>
                <li>
                    Drag in the signed file (<Code>*.p7s</Code>, <Code>*.pdf</Code>, <Code>*.xml</Code>, <Code>*.asics</Code>,{" "}
                    <Code>*.asice</Code>). For a detached signature, also add the original file.
                </li>
                <li>
                    Click <Ui>Verify</Ui>.
                </li>
                <li>
                    If the signatures are valid, “Signatures are valid” appears with the format, container name and number of signatures,
                    and for each signature — the signer's full name, organization, individual tax number / EDRPOU code, provider,
                    certificate number and signing time (or timestamp).
                </li>
                <li>
                    If the signature contains the data inside, the <Ui>Download …</Ui> button lets you save the original document.
                </li>
                <li>If verification fails, “Verification failed” appears with an explanation of the reason.</li>
            </Steps>
            <Figure
                src={signingVerifyImage}
                alt="Checking a signature"
                caption="Checking a signature: add a signed file to see who signed it and when."
            />
            <p>Verification takes place in your browser — the file is not sent to the server.</p>
        </>
    );
}
