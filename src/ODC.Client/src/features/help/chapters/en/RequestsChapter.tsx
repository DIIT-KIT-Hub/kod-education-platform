import { Chapter, Ref, Steps, Topic, Ui } from "../../components/Manual";

/**
 * Requests (English).
 * @returns The chapter.
 */
export function RequestsChapter() {
    return (
        <Chapter id="requests">
            <Topic id="requests-new">
                <p>
                    Found a bug, have an idea how to make the cabinet better, or don't know how to do something? Write a request — the
                    people who look after the cabinet will see it. Requests are available to every user.
                </p>
                <Steps>
                    <li>
                        Open <Ui>Requests</Ui> in the menu (the <Ui>Communication</Ui> section) and click <Ui>New request</Ui>.
                    </li>
                    <li>
                        Choose <Ui>What happened?</Ui>: <Ui>Bug</Ui>, <Ui>Suggestion</Ui>, <Ui>Question</Ui>, <Ui>Access and rights</Ui> or{" "}
                        <Ui>Other</Ui>.
                    </li>
                    <li>
                        Fill in <Ui>Subject</Ui> (briefly, up to 200 characters) and <Ui>Description</Ui>: what you did, what you expected
                        and what happened instead; for a bug, the steps to repeat it.
                    </li>
                    <li>
                        If needed, click <Ui>Attach files</Ui> or drop files on the button: screenshots, documents, archives — up to 10
                        files of up to 25 MB each, 50 MB in all (the same types as in chats). Photos show in the request as thumbnails.
                    </li>
                    <li>
                        Choose the <Ui>Priority</Ui> (<Ui>Low</Ui>, <Ui>Normal</Ui>, <Ui>High</Ui>, <Ui>Critical</Ui> — when work has
                        stopped) and click <Ui>Send request</Ui>.
                    </li>
                </Steps>
                <p>
                    The request gets a number (for example “No. 12”), and the people who handle requests get a notification about it in the
                    cabinet and an e-mail with a link to the request (when the administrator has set up e-mail). The list of your requests
                    with their states is on the <Ui>Requests</Ui> page; search works by number or subject.
                </p>
            </Topic>
            <Topic id="requests-follow">
                <p>
                    The page of a request shows its whole history: your description, the answers (marked <Ui>Support</Ui>) and short notes
                    about changes of the state or the handler. On the right are the <Ui>Details</Ui>: state, handler, category, priority,
                    dates.
                </p>
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
                        The <Ui>Close request</Ui> button closes it when the question is settled or no longer relevant. Nobody can write in
                        a closed request — create a new one.
                    </li>
                </ul>
                <p>Only you and the people with the “Handling requests” permission see your request; only they can download its files.</p>
            </Topic>
        </Chapter>
    );
}
