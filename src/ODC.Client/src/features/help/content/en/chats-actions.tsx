import { Ref, Ui } from "../../components/Manual";

/**
 * The manual page "chats-actions" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>Point at a message (on a phone the buttons are always shown):</p>
            <ul>
                <li>
                    <Ui>React</Ui> — the same emotions as under news (<Ref to="news-discussion">Reactions and comments</Ref>). One reaction
                    per person: another one replaces it, the same one again removes it. The emotions with their counts show under the
                    message; click one to join in.
                </li>
                <li>
                    <Ui>Forward</Ui> — any message: choose a chat or find a colleague by name (the one-to-one chat with them opens by
                    itself). The message appears there under your name marked “Forwarded from …” — always with the author of the original,
                    even when you forward a forwarded message. Files and photos are copied, so deleting the original keeps the copy. A
                    forwarded message cannot be edited.
                </li>
                <li>
                    <Ui>Edit</Ui> — your own messages only: the text opens for editing, <Ui>Save</Ui> (or <kbd>Enter</kbd>) saves it and
                    “edited” appears next to the time. Attachments stay.
                </li>
                <li>
                    <Ui>Delete</Ui> — any message for yourself, and your own also for everyone (
                    <Ref to="chats-delete">Deleting chats and messages</Ref>).
                </li>
            </ul>
            <p>
                A chat is marked as read when you see its newest messages. In a one-to-one chat “Seen” appears under your message as soon as
                the other person has read it. If you scrolled up and new messages arrived, the <Ui>New messages</Ui> button takes you down;
                older messages load as you scroll up.
            </p>
        </>
    );
}
