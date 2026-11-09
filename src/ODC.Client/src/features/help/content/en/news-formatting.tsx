import { Table, Ui } from "../../components/Manual";

/**
 * The manual page "news-formatting" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The text of a post is formatted with the toolbar buttons above the field — like in an ordinary word processor. Keyboard
                shortcuts: <kbd>Ctrl</kbd>+<kbd>B</kbd> — bold, <kbd>Ctrl</kbd>+<kbd>I</kbd> — italic, <kbd>Ctrl</kbd>+<kbd>U</kbd> —
                underline, <kbd>Ctrl</kbd>+<kbd>Z</kbd> / <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> — undo / redo.
            </p>
            <Table>
                <thead>
                    <tr>
                        <th>Button</th>
                        <th>What it does</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>¶, H2, H3, H4</td>
                        <td>Normal text, heading, subheading, small heading</td>
                    </tr>
                    <tr>
                        <td>B, I, U, S, &lt;/&gt;</td>
                        <td>Bold, italic, underline, strikethrough, code</td>
                    </tr>
                    <tr>
                        <td>Lists, quote, divider</td>
                        <td>Bulleted and numbered lists, a highlighted quote, a horizontal line</td>
                    </tr>
                    <tr>
                        <td>Alignment</td>
                        <td>Left, center, right (for paragraphs and headings)</td>
                    </tr>
                    <tr>
                        <td>Link</td>
                        <td>
                            Select text, click <Ui>Link</Ui>, enter the address and click <Ui>Apply</Ui>. You can link to websites, e-mail
                            addresses, phone numbers and pages of the cabinet; links to other sites open in a new tab
                        </td>
                    </tr>
                    <tr>
                        <td>Table</td>
                        <td>
                            Inserts a 3×3 table with a header row. While the cursor is in the table, buttons appear to add a row or a
                            column, remove a row or a column, or delete the table
                        </td>
                    </tr>
                    <tr>
                        <td>Photo in the text</td>
                        <td>One or several photos across the width of the text</td>
                    </tr>
                    <tr>
                        <td>Photo gallery</td>
                        <td>
                            Several photos as tiles; readers open them full screen and browse. If the cursor is in a gallery, new photos are
                            added to it
                        </td>
                    </tr>
                </tbody>
            </Table>
            <p>
                <strong>Photos.</strong> JPEG, PNG, WebP, AVIF and GIF up to 15 MB are supported. Photos can also be pasted from the
                clipboard (<kbd>Ctrl</kbd>+<kbd>V</kbd>) or dragged into the text. Each photo is uploaded right away and made smaller, and
                the hidden data of the picture (including the place it was taken) is removed. Galleries also get thumbnails. A photo removed
                from the text is deleted when the post is saved.
            </p>
            <p>
                For security, only this formatting is kept: embedded pages, images from other sites and custom styles are not saved. Posts
                created in the previous version of the editor are shown as before and are saved in the new format after the first edit.
            </p>
        </>
    );
}
