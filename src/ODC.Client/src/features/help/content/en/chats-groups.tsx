import { Ui } from "../../components/Manual";

/**
 * The manual page "chats-groups" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The <Ui>Members</Ui> button in the header of a group chat opens the <Ui>Chat members</Ui> window:
            </p>
            <ul>
                <li>
                    <Ui>Chat title</Ui> — any member can change or remove the title (<Ui>Save</Ui>).
                </li>
                <li>
                    <Ui>Add people</Ui> — any member finds colleagues and clicks <Ui>Add</Ui>. New members see the whole history of the
                    chat. A group can have up to 250 members.
                </li>
                <li>
                    Only the <Ui>Owner</Ui> — the person who created the chat — removes people (the button next to a name). When the owner
                    leaves, the longest-standing member becomes the owner.
                </li>
                <li>
                    <Ui>Leave the chat</Ui> — the chat disappears from your list; you can come back only if someone adds you again. When the
                    last member leaves, the chat is deleted with its messages and files.
                </li>
            </ul>
            <p>
                These changes show as notes in the middle of the chat (“… added to the chat: …”). In a one-to-one chat the{" "}
                <Ui>About the chat</Ui> button shows the other person, and <Ui>Create a group chat</Ui> starts a new group with them — the
                one-to-one chat stays as it is.
            </p>
        </>
    );
}
