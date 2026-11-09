import requestsAdminImage from "../../images/en/requests-admin.webp";
import { Figure, Ref, Ui } from "../../components/Manual";

/**
 * The manual page "admin-requests" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                <Ui>Administration</Ui> → <Ui>Requests</Ui> lists the requests of all users of the cabinet. You get a notification about
                every new request (see “<Ref to="notifications-kinds">What notifications are about</Ref>”).
            </p>
            <Figure
                src={requestsAdminImage}
                alt="Requests for those who handle them"
                caption="Handling requests: filters, assignees and states."
            />
            <ul>
                <li>
                    The <Ui>All requests</Ui> / <Ui>Open</Ui> / <Ui>Assigned to me</Ui> filter, search by number, subject or author, state
                    and category filters; the <Ui>Priority</Ui>, <Ui>State</Ui> and <Ui>Updated</Ui> headers sort the list.
                </li>
                <li>
                    The <Ui>Awaiting answer</Ui> mark next to the number means the author wrote last — it is your turn.
                </li>
            </ul>
            <p>
                On the page of a request the <Ui>Handling</Ui> card is on the right:
            </p>
            <ul>
                <li>
                    <Ui>State</Ui>: <Ui>New</Ui>, <Ui>In progress</Ui>, <Ui>Waiting for the author</Ui>, <Ui>Resolved</Ui>, <Ui>Closed</Ui>.
                    The author is notified about every change. A closed request can be opened again by changing its state.
                </li>
                <li>
                    <Ui>Handler</Ui> — who takes care of the request (only people with the “Handling requests” permission); the{" "}
                    <Ui>Take it</Ui> button assigns you. Your first answer in a new request moves it to <Ui>In progress</Ui> and assigns you
                    when nobody has it yet. From then on only the handler gets the author's answers.
                </li>
                <li>
                    <Ui>Category</Ui> and <Ui>Priority</Ui> — correct them if the author chose inaccurately.
                </li>
            </ul>
            <p>
                Write your answer in the box under the history, with files if needed; the author gets a notification. The “Handling
                requests” permission applies to the whole cabinet; the system administrator always has it.
            </p>
        </>
    );
}
