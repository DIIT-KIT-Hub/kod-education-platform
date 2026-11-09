import requestsListImage from "../../images/en/requests-list.webp";
import requestsCardImage from "../../images/en/requests-card.webp";
import { Figure, Ref, Ui } from "../../components/Manual";

/**
 * The manual page "requests-follow" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The page of a request shows its whole history: your description, the answers (marked <Ui>Support</Ui>) and short notes about
                changes of the state or the handler. On the right are the <Ui>Details</Ui>: state, handler, category, priority, dates.
            </p>
            <Figure src={requestsListImage} alt="My requests" caption="My requests with their state." />
            <Figure
                src={requestsCardImage}
                alt="A request with its messages"
                caption="A request: the messages, the files and the current state."
            />
            <ul>
                <li>
                    Every answer and change of state brings a notification (see “<Ref to="notifications-kinds">Notifications</Ref>”); an
                    open request page updates itself.
                </li>
                <li>
                    Answer or add details in the box at the bottom, with files; <kbd>Ctrl</kbd>+<kbd>Enter</kbd> sends.
                </li>
                <li>
                    States: <Ui>New</Ui> (not handled yet), <Ui>In progress</Ui>, <Ui>Waiting for the author</Ui> (you were asked
                    something), <Ui>Resolved</Ui> and <Ui>Closed</Ui>. If the problem is still there after “Resolved”, just write — the
                    request goes back to work.
                </li>
                <li>
                    The <Ui>Close request</Ui> button closes it when the question is settled or no longer relevant. Nobody can write in a
                    closed request — create a new one.
                </li>
            </ul>
            <p>Only you and the people with the “Handling requests” permission see your request; only they can download its files.</p>
        </>
    );
}
