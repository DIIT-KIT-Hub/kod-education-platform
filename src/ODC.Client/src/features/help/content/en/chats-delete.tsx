import { Ui } from "../../components/Manual";

/**
 * The manual page "chats-delete" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>As in messengers, you delete either only for yourself or for everyone:</p>
            <ul>
                <li>
                    <Ui>Delete</Ui> next to your own message opens a confirmation with the <Ui>Also delete for …</Ui> check box (in a group
                    chat <Ui>Delete for all members</Ui>). With the check box the message and its files disappear for everyone and “Message
                    deleted” stays in its place; without it the message disappears only for you.
                </li>
                <li>
                    Someone else’s message (and a “Message deleted” line) can be deleted only for yourself — the other members still see it.
                </li>
                <li>
                    <Ui>Delete chat</Ui> in the <Ui>About the chat</Ui> window of a one-to-one chat clears the history for you and removes
                    the chat from your list until a new message arrives: then the chat comes back, but only with the new messages. With{" "}
                    <Ui>Also delete for …</Ui> the chat with all its messages and files is deleted for both of you.
                </li>
                <li>
                    <Ui>Clear history</Ui> in the <Ui>Chat members</Ui> window of a group chat removes the messages only for you; you stay a
                    member and see new messages.
                </li>
                <li>
                    Only the owner of a group can <Ui>Delete chat for everyone</Ui>: the chat with all its messages and files is deleted for
                    all members. The others can <Ui>Leave the chat</Ui>; when the last one leaves, the chat is deleted too.
                </li>
            </ul>
            <p>What was deleted for everyone cannot be restored.</p>
        </>
    );
}
