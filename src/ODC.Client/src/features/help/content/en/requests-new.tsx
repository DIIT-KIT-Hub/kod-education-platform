import requestsNewImage from "../../images/en/requests-new.webp";
import { Figure, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "requests-new" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Found a bug, have an idea how to make the cabinet better, or don't know how to do something? Write a request — the people
                who look after the cabinet will see it. Requests are available to every user.
            </p>
            <Figure
                src={requestsNewImage}
                alt="The new request form"
                caption="A new request: the category, the subject, the description, files and the priority."
            />
            <Steps>
                <li>
                    Open <Ui>Requests</Ui> in the menu (the <Ui>Communication</Ui> section) and click <Ui>New request</Ui>.
                </li>
                <li>
                    Choose <Ui>What happened?</Ui>: <Ui>Bug</Ui>, <Ui>Suggestion</Ui>, <Ui>Question</Ui>, <Ui>Access and rights</Ui> or{" "}
                    <Ui>Other</Ui>.
                </li>
                <li>
                    Fill in <Ui>Subject</Ui> (briefly, up to 200 characters) and <Ui>Description</Ui>: what you did, what you expected and
                    what happened instead; for a bug, the steps to repeat it.
                </li>
                <li>
                    If needed, click <Ui>Attach files</Ui> or drop files on the button: screenshots, documents, archives — up to 10 files of
                    up to 25 MB each, 50 MB in all (the same types as in chats). Photos show in the request as thumbnails.
                </li>
                <li>
                    Choose the <Ui>Priority</Ui> (<Ui>Low</Ui>, <Ui>Normal</Ui>, <Ui>High</Ui>, <Ui>Critical</Ui> — when work has stopped)
                    and click <Ui>Send request</Ui>.
                </li>
            </Steps>
            <p>
                The request gets a number (for example “No. 12”), and the people who handle requests get a notification about it in the
                cabinet and an e-mail with a link to the request (when the administrator has set up e-mail). The list of your requests with
                their states is on the <Ui>Requests</Ui> page; search works by number or subject.
            </p>
        </>
    );
}
