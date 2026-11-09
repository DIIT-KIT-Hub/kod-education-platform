import { Ui } from "../../components/Manual";

/**
 * The manual page "chats-write" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Type in the <Ui>Type a message</Ui> field at the bottom of the chat and press <kbd>Enter</kbd> or <Ui>Send</Ui>.{" "}
                <kbd>Shift</kbd>+<kbd>Enter</kbd> starts a new line. The message appears at once marked “Sending…”, and for the other
                members without reloading the page. While you type, the others see “… is typing…” under the messages.
            </p>
            <ul>
                <li>
                    <strong>Formatting.</strong> The <Ui>Formatting</Ui> button (the letter T) opens the bar: <Ui>Bold</Ui>, <Ui>Italic</Ui>
                    , <Ui>Underline</Ui>, <Ui>Strikethrough</Ui>, <Ui>Bulleted list</Ui>, <Ui>Numbered list</Ui>, <Ui>Quote</Ui>,{" "}
                    <Ui>Inline code</Ui>, <Ui>Code block</Ui> and <Ui>Link</Ui>. While the bar is open, <kbd>Enter</kbd> starts a new line
                    (handy for lists) and <kbd>Ctrl</kbd>+<kbd>Enter</kbd> sends. The usual shortcuts work too: <kbd>Ctrl</kbd>+<kbd>B</kbd>
                    , <kbd>Ctrl</kbd>+<kbd>I</kbd>, <kbd>Ctrl</kbd>+<kbd>U</kbd>, and “- ” or “1. ” at the start of a line begins a list.
                </li>
                <li>
                    <strong>Links.</strong> An address you paste or type becomes a link by itself; the <Ui>Link</Ui> button turns the
                    selected text into a link. Links open in a new tab.
                </li>
                <li>
                    <strong>Files and photos.</strong> Click <Ui>Attach files</Ui> (the paper clip), drag files into the field or paste a
                    screenshot (<kbd>Ctrl</kbd>+<kbd>V</kbd>). One message takes up to 10 files, each up to 25 MB, up to 50 MB in total.
                    Photos (JPEG, PNG, WebP, AVIF) show in the chat and open full screen with a <Ui>Download</Ui> button; other files show
                    as cards to download. You can send documents (PDF, Word, Excel, PowerPoint, OpenDocument, RTF, TXT, CSV), signature
                    files (P7S, ASiC), archives (ZIP, 7Z, RAR), audio and video; executables, pages and scripts cannot be sent.
                </li>
                <li>
                    If the connection dropped and a message did not get through, it is marked “Not sent” with the <Ui>Send again</Ui> and{" "}
                    <Ui>Remove</Ui> buttons. Sending again never creates a duplicate.
                </li>
            </ul>
        </>
    );
}
