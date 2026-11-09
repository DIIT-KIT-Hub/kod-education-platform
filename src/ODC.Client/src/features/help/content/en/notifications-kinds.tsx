import { Bell } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Ref, Table } from "../../components/Manual";

/**
 * The manual page "notifications-kinds" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Notifications arrive at once, without reloading the page: the number on the bell <Icon icon={Bell} /> grows and a short
                message appears at the bottom of the screen (except new chat messages — they show in the chats themselves).
            </p>
            <Table>
                <thead>
                    <tr>
                        <th>Notification</th>
                        <th>When it comes</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Reply to your comment</td>
                        <td>Someone answered your comment on a news article; the link leads straight to the reply.</td>
                    </tr>
                    <tr>
                        <td>Document awaits your signature</td>
                        <td>
                            It is your turn to sign a document (in a sequential route, once the previous signers have signed). Once per
                            sending for signing.
                        </td>
                    </tr>
                    <tr>
                        <td>All signers signed the document / Document rejected</td>
                        <td>For the author of a document: the route is complete or a signer declined (with the reason).</td>
                    </tr>
                    <tr>
                        <td>New message</td>
                        <td>
                            A message in a chat. All unread messages of one chat collect in one notification with their number; when you
                            read the chat, the notification becomes read by itself.
                        </td>
                    </tr>
                    <tr>
                        <td>New reply in a request / Request state</td>
                        <td>
                            An answer or a change of state in your request (see “<Ref to="requests-follow">Answers, states and closing</Ref>
                            ”).
                        </td>
                    </tr>
                    <tr>
                        <td>New request / A request is assigned to you</td>
                        <td>
                            For the people who handle requests (see “<Ref to="admin-requests">Handling requests</Ref>”).
                        </td>
                    </tr>
                </tbody>
            </Table>
            <p>
                You never get notifications about your own actions. When you open a document, a chat or a request, the notifications about
                it become read.
            </p>
        </>
    );
}
