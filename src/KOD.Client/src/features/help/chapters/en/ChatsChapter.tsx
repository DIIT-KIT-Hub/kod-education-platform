import { Chapter, Ref, Steps, Topic, Ui } from "../../components/Manual";

/**
 * Chats (English).
 * @returns The chapter.
 */
export function ChatsChapter() {
    return (
        <Chapter id="chats">
            <Topic id="chats-start">
                <p>
                    Open <Ui>Chats</Ui> in the menu. On the left are your chats (the most recent at the top): the photo of the other person
                    or of the group members, the name, the newest message and its time. The number on the right counts unread messages; the
                    total also shows next to <Ui>Chats</Ui> in the menu. The <Ui>Search chats</Ui> field filters the list by name. The
                    chosen chat opens on the right; on a phone it takes the whole screen and the <Ui>Back to chats</Ui> arrow returns to the
                    list.
                </p>
                <Steps>
                    <li>
                        Click <Ui>New chat</Ui> (the pencil next to the title).
                    </li>
                    <li>
                        In the <Ui>Name or e-mail</Ui> field type at least two characters and choose a person from the list (the position
                        under the name tells namesakes apart). You can choose several people — they appear above the search, the cross
                        removes one.
                    </li>
                    <li>
                        One person — <Ui>Start the chat</Ui>: your one-to-one chat with them opens (the same one if it already exists).
                        Several people — <Ui>Create a group chat</Ui>; the <Ui>Chat title</Ui> field is optional, without a title the chat
                        is named after its members.
                    </li>
                </Steps>
                <p>
                    You can write to colleagues of every organization of the cabinet who have chats; blocked people and people with chats
                    turned off are not found. A one-to-one chat appears for the other person with its first message.
                </p>
            </Topic>
            <Topic id="chats-write">
                <p>
                    Type in the <Ui>Type a message</Ui> field at the bottom of the chat and press <kbd>Enter</kbd> or <Ui>Send</Ui>.{" "}
                    <kbd>Shift</kbd>+<kbd>Enter</kbd> starts a new line. The message appears at once marked “Sending…”, and for the other
                    members without reloading the page. While you type, the others see “… is typing…” under the messages.
                </p>
                <ul>
                    <li>
                        <strong>Formatting.</strong> The <Ui>Formatting</Ui> button (the letter T) opens the bar: <Ui>Bold</Ui>,{" "}
                        <Ui>Italic</Ui>, <Ui>Underline</Ui>, <Ui>Strikethrough</Ui>, <Ui>Bulleted list</Ui>, <Ui>Numbered list</Ui>,{" "}
                        <Ui>Quote</Ui>, <Ui>Inline code</Ui>, <Ui>Code block</Ui> and <Ui>Link</Ui>. While the bar is open, <kbd>Enter</kbd>{" "}
                        starts a new line (handy for lists) and <kbd>Ctrl</kbd>+<kbd>Enter</kbd> sends. The usual shortcuts work too:{" "}
                        <kbd>Ctrl</kbd>+<kbd>B</kbd>, <kbd>Ctrl</kbd>+<kbd>I</kbd>, <kbd>Ctrl</kbd>+<kbd>U</kbd>, and “- ” or “1. ” at the
                        start of a line begins a list.
                    </li>
                    <li>
                        <strong>Links.</strong> An address you paste or type becomes a link by itself; the <Ui>Link</Ui> button turns the
                        selected text into a link. Links open in a new tab.
                    </li>
                    <li>
                        <strong>Files and photos.</strong> Click <Ui>Attach files</Ui> (the paper clip), drag files into the field or paste
                        a screenshot (<kbd>Ctrl</kbd>+<kbd>V</kbd>). One message takes up to 10 files, each up to 25 MB, up to 50 MB in
                        total. Photos (JPEG, PNG, WebP, AVIF) show in the chat and open full screen with a <Ui>Download</Ui> button; other
                        files show as cards to download. You can send documents (PDF, Word, Excel, PowerPoint, OpenDocument, RTF, TXT, CSV),
                        signature files (P7S, ASiC), archives (ZIP, 7Z, RAR), audio and video; executables, pages and scripts cannot be
                        sent.
                    </li>
                    <li>
                        If the connection dropped and a message did not get through, it is marked “Not sent” with the <Ui>Send again</Ui>{" "}
                        and <Ui>Remove</Ui> buttons. Sending again never creates a duplicate.
                    </li>
                </ul>
            </Topic>
            <Topic id="chats-actions">
                <p>Point at a message (on a phone the buttons are always shown):</p>
                <ul>
                    <li>
                        <Ui>React</Ui> — the same emotions as under news (<Ref to="news-discussion">Reactions and comments</Ref>). One
                        reaction per person: another one replaces it, the same one again removes it. The emotions with their counts show
                        under the message; click one to join in.
                    </li>
                    <li>
                        <Ui>Edit</Ui> — your own messages only: the text opens for editing, <Ui>Save</Ui> (or <kbd>Enter</kbd>) saves it and
                        “edited” appears next to the time. Attachments stay.
                    </li>
                    <li>
                        <Ui>Delete</Ui> — your own messages only, after a confirmation. The message and its files are deleted for everyone
                        and “Message deleted” stays in its place.
                    </li>
                </ul>
                <p>
                    A chat is marked as read when you see its newest messages. In a one-to-one chat “Seen” appears under your message as
                    soon as the other person has read it. If you scrolled up and new messages arrived, the <Ui>New messages</Ui> button
                    takes you down; older messages load as you scroll up.
                </p>
            </Topic>
            <Topic id="chats-groups">
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
                        Only the <Ui>Owner</Ui> — the person who created the chat — removes people (the button next to a name). When the
                        owner leaves, the longest-standing member becomes the owner.
                    </li>
                    <li>
                        <Ui>Leave the chat</Ui> — the chat disappears from your list; you can come back only if someone adds you again. When
                        the last member leaves, the chat is deleted with its messages and files.
                    </li>
                </ul>
                <p>
                    These changes show as notes in the middle of the chat (“… added to the chat: …”). In a one-to-one chat the{" "}
                    <Ui>About the chat</Ui> button shows the other person, and <Ui>Create a group chat</Ui> starts a new group with them —
                    the one-to-one chat stays as it is.
                </p>
            </Topic>
            <Topic id="chats-privacy">
                <ul>
                    <li>
                        Only its members see a chat and its files — neither administrators nor managers have access to other people's chats.
                        A member who was removed stops receiving the chat at once.
                    </li>
                    <li>
                        Files are kept in the server's private storage and given only to the members of the chat, always as downloads (the
                        browser never opens them as a page). Photos are compressed before saving and their metadata (including the location)
                        is removed.
                    </li>
                    <li>
                        Messages can contain only text, formatting and safe links — the server removes everything else, so a message cannot
                        run foreign code in your page.
                    </li>
                    <li>
                        Chats need the “Chats” permission; every predefined staff role has it, and external users can be given it separately
                        (<Ref to="roles-system">predefined roles</Ref>).
                    </li>
                </ul>
            </Topic>
        </Chapter>
    );
}
