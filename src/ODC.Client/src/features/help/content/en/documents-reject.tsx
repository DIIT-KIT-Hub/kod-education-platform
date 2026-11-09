import documentsRejectImage from "../../images/en/documents-reject.webp";
import { Figure, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "documents-reject" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                If the document contains a mistake or you disagree with it, decline to sign — the author will see the reason and can prepare
                a corrected document.
            </p>
            <Figure
                src={documentsRejectImage}
                alt="The decline window"
                caption="Declining: give the reason; the author sees it on the document card."
            />
            <Steps>
                <li>
                    Open the document and click <Ui>Decline</Ui>.
                </li>
                <li>
                    In the “Decline signing” window, enter the <Ui>Reason</Ui> (required, up to 1000 characters) — specifically what needs
                    to be fixed.
                </li>
                <li>
                    Click <Ui>Decline</Ui>. “Decline recorded” appears.
                </li>
            </Steps>
            <p>
                The very first decline stops the route: the document gets the “Rejected” status, and the other signers can no longer sign
                it. The reason is shown in red in the route. The author will correct the document and send it again — then it will reappear
                in your queue.
            </p>
        </>
    );
}
