import { Steps, Ui } from "../../components/Manual";

/**
 * The manual page "documents-route" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Choose the signing order: <Ui>Sequential</Ui> (one after another, the default) or <Ui>Parallel</Ui> (in any order).
                </li>
                <li>
                    In the <Ui>Find an employee</Ui> box, start typing a last name, first name or email (at least 2 characters). Up to 10
                    active employees with their primary positions appear.
                </li>
                <li>Click an employee — they are added to the end of the route with a sequence number.</li>
                <li>
                    Change the order with the <Ui>Up</Ui> and <Ui>Down</Ui> buttons, remove signers with <Ui>Remove</Ui>.
                </li>
            </Steps>
            <ul>
                <li>
                    <strong>Sequential:</strong> only the first signer in line who hasn't signed yet can sign. The next one sees the
                    document under “Awaiting my signature” after the previous signer has signed. Suitable for bottom-up approval (specialist
                    → head of division → director of department → head of organization).
                </li>
                <li>
                    <strong>Parallel:</strong> all signers can sign at the same time in any order. Suitable for documents signed by a
                    committee.
                </li>
                <li>A maximum of 50 signers, each person only once.</li>
                <li>A document without signers can only be saved as a draft.</li>
            </ul>
        </>
    );
}
