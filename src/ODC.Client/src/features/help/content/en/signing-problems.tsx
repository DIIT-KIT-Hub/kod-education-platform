import { Table } from "../../components/Manual";

/**
 * The manual page "signing-problems" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Table>
                <thead>
                    <tr>
                        <th>Message</th>
                        <th>What to do</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Wrong password or damaged key file.</td>
                        <td>
                            Check the keyboard layout and Caps Lock, and select the right file (the key file itself, for example Key-6.dat,
                            not a certificate).
                        </td>
                    </tr>
                    <tr>
                        <td>The key certificate could not be found. Select the provider manually or add certificate files.</td>
                        <td>
                            Select your provider in the list instead of “Detect automatically”, or add certificates via “Add certificates
                            manually”.
                        </td>
                    </tr>
                    <tr>
                        <td>The key certificate has expired or is not yet valid.</td>
                        <td>The key has expired — get a new one from your provider.</td>
                    </tr>
                    <tr>
                        <td>The key certificate has been revoked (it is on the revocation list).</td>
                        <td>The key has been blocked by the provider. Contact the provider to get a new one.</td>
                    </tr>
                    <tr>
                        <td>Could not obtain a timestamp. Try the B-B signature level or try again later.</td>
                        <td>
                            The provider's timestamp server is temporarily unavailable. Try again in a few minutes; if the signature is
                            urgent, choose the B-B level.
                        </td>
                    </tr>
                    <tr>
                        <td>Could not check the certificate status (OCSP). Try again later.</td>
                        <td>The provider's server is temporarily unavailable. Try again later.</td>
                    </tr>
                    <tr>
                        <td>No connection to the provider's server (CA). Check your connection and try again.</td>
                        <td>Check your internet connection and try again.</td>
                    </tr>
                    <tr>
                        <td>Files larger than 25 MB are not supported.</td>
                        <td>Reduce the file size (for example, compress the PDF) or split it into parts.</td>
                    </tr>
                    <tr>
                        <td>Could not load the signing library.</td>
                        <td>Refresh the page. The library is several megabytes in size and loads when the signing panel opens.</td>
                    </tr>
                    <tr>
                        <td>This is a detached signature: also add the file that was signed.</td>
                        <td>When verifying, add the original document together with the signature file.</td>
                    </tr>
                    <tr>
                        <td>No signatures found in the file.</td>
                        <td>The selected file doesn't contain a signature — make sure you uploaded the signed file.</td>
                    </tr>
                </tbody>
            </Table>
        </>
    );
}
