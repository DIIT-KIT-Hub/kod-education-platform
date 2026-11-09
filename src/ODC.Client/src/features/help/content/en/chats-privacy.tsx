import { Ref } from "../../components/Manual";

/**
 * The manual page "chats-privacy" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <ul>
                <li>
                    Only its members see a chat and its files — neither administrators nor managers have access to other people's chats. A
                    member who was removed stops receiving the chat at once.
                </li>
                <li>
                    Files are kept in the closed storage of the cabinet and given only to the members of the chat, always as downloads (the
                    browser never opens them as a page). Photos are compressed before saving and their metadata (including the location) is
                    removed.
                </li>
                <li>
                    Messages can contain only text, formatting and safe links — everything else is removed, so a message cannot run foreign
                    code in your page.
                </li>
                <li>
                    Chats need the “Chats” permission; every predefined staff role has it, and external users can be given it separately (
                    <Ref to="roles-system">predefined roles</Ref>).
                </li>
            </ul>
        </>
    );
}
