import chatsImage from "../../images/en/chats.webp";
import { Figure, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "chats-start" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>Chats</Ui> in the menu. On the left are your chats (the most recent at the top): the photo of the other person or
                of the group members, the name, the newest message and its time. The number on the right counts unread messages; the total
                also shows next to <Ui>Chats</Ui> in the menu. The <Ui>Search chats</Ui> field filters the list by name. The chosen chat
                opens on the right; on a phone it takes the whole screen and the <Ui>Back to chats</Ui> arrow returns to the list.
            </p>
            <Figure
                src={chatsImage}
                alt="The chats page with an open conversation"
                caption="Chats: the conversations on the left, the open one on the right."
            />
            <Steps>
                <li>
                    Click <Ui>New chat</Ui> (the pencil next to the title).
                </li>
                <li>
                    In the <Ui>Name or e-mail</Ui> field type at least two characters and choose a person from the list (the position under
                    the name tells namesakes apart). You can choose several people — they appear above the search, the cross removes one.
                </li>
                <li>
                    One person — <Ui>Start the chat</Ui>: your one-to-one chat with them opens (the same one if it already exists). Several
                    people — <Ui>Create a group chat</Ui>; the <Ui>Chat title</Ui> field is optional, without a title the chat is named
                    after its members.
                </li>
            </Steps>
            <p>
                You can write to colleagues of every organization of the cabinet who have chats; blocked people and people with chats turned
                off are not found. A one-to-one chat appears for the other person with its first message.
            </p>
        </>
    );
}
