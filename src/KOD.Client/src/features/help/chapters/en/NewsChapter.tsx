import { Chapter, Ref, Steps, Table, Topic, Ui } from "../../components/Manual";

/**
 * News (English).
 * @returns The chapter.
 */
export function NewsChapter() {
    return (
        <Chapter id="news">
            <Topic id="news-read">
                <p>
                    Open <Ui>News</Ui> in the menu.
                </p>
                <ul>
                    <li>
                        Category tabs: <Ui>All</Ui>, “News”, “Announcement”, “Event”, “Training and development”, “Projects and
                        achievements”.
                    </li>
                    <li>
                        The <Ui>Search news</Ui> field searches titles and summaries.
                    </li>
                    <li>
                        Cards show the cover image, category, the <Ui>Pinned</Ui> badge, title, summary, date, the author with a photo and
                        the numbers of views, reactions and comments. Pinned posts always come first, then from newest to oldest.
                    </li>
                    <li>There are 9 cards per page; the number can be changed (9, 18, 36, 72).</li>
                </ul>
                <p>
                    Click a card to open the post: category, date, number of views, cover image, author, summary and full text. Photos in
                    the text and in galleries open full screen: browse with the arrow keys, the buttons or a swipe, close with{" "}
                    <kbd>Esc</kbd>. The <Ui>All news</Ui> button returns to the feed. The latest news is also shown on the home page.
                </p>
                <p>A view is counted once per reader. If a post has been deleted or unpublished — “News post not found”.</p>
            </Topic>
            <Topic id="news-discussion">
                <p>
                    Reactions and comments are below the text of a post. New comments and reactions of other readers appear without
                    reloading the page.
                </p>
                <ul>
                    <li>
                        <strong>Reactions.</strong> Click <Ui>React</Ui> (the smiley with a plus) and choose an emotion: 👍 Like, 👎
                        Dislike, ❤️ Love, 👏 Applause, 🎉 Congratulations, 😄 Funny, 😮 Wow, 🤔 Doubtful, 😢 Sad, 😡 Outrageous. Each reader
                        has one reaction per post: another emotion replaces the previous one, clicking your own again removes it. Comments
                        can be reacted to the same way.
                    </li>
                    <li>
                        <strong>Comment.</strong> Type in the “Write a comment…” field (up to 2,000 characters) and click <Ui>Send</Ui> (or
                        press Ctrl+Enter). Comments are shown as threads, 10 per page; choose the order in <Ui>Comment order</Ui>:{" "}
                        <Ui>Best</Ui> (more reactions and replies), <Ui>Newest</Ui> or <Ui>Oldest</Ui>.
                    </li>
                    <li>
                        <strong>Reply.</strong> <Ui>Reply</Ui> under any comment opens a reply field — the reply goes under that very
                        comment, so a thread can have several levels. Above a reply you see whom it answers (“in reply to …”). The line to
                        the left of a thread collapses it (<Ui>Collapse the thread</Ui>). The first 5 replies are shown under a comment,{" "}
                        <Ui>More replies</Ui> opens the rest; very deep threads open on their own with <Ui>Continue this thread</Ui>, and{" "}
                        <Ui>Back to all comments</Ui> returns to the discussion.
                    </li>
                    <li>
                        <strong>Your comment</strong> can be edited with <Ui>Edit</Ui> (it gets an “(edited)” mark) or deleted with{" "}
                        <Ui>Delete</Ui>. If someone has already replied, “The comment was deleted.” stays in its place so the thread holds
                        together; nobody can reply to it any more, and it goes away with its last reply.
                    </li>
                    <li>
                        <strong>Moderation.</strong> News editors of the organization the post belongs to can delete any comment under it;
                        such deletions are recorded in the audit log (“Comment deleted”).
                    </li>
                </ul>
                <p>
                    Only published posts can be commented on. If the editor has turned comments off, the post shows “Comments on this post
                    are turned off.” — reactions still work.
                </p>
            </Topic>
            <Topic id="news-list">
                <p>
                    In the news feed, click <Ui>Manage</Ui> (or <Ui>Administration</Ui> → <Ui>News</Ui>). Here you see the posts of the
                    organizations whose news you manage (an administrator of all organizations sees all of them), including drafts — only
                    editors can see them.
                </p>
                <ul>
                    <li>Search — by title or summary.</li>
                    <li>
                        Columns: <strong>Title</strong> (and category), <strong>Status</strong> (“Draft” / “Published”, “Pinned”),{" "}
                        <strong>Published</strong>, <strong>Modified</strong>, <strong>Organization</strong> (“Entire cabinet” — for general
                        news), <strong>Author</strong>, the <Ui>Edit</Ui> button.
                    </li>
                    <li>
                        <Ui>New post</Ui> — create a news post.
                    </li>
                </ul>
            </Topic>
            <Topic id="news-editor">
                <Steps>
                    <li>
                        Click <Ui>New post</Ui> (in the feed or on the management page) or <Ui>Edit</Ui> next to an existing one.
                    </li>
                    <li>
                        Fill in the <Ui>Title</Ui> (up to 200 characters) and <Ui>Summary</Ui> (up to 500 characters, 1–2 sentences for the
                        card).
                    </li>
                    <li>
                        Write the <Ui>Text</Ui> in the visual editor (up to 50,000 characters) — headings, lists, tables, photos and
                        galleries, see “<Ref to="news-formatting">Text formatting, photos and galleries</Ref>”.
                    </li>
                    <li>
                        In the “Publication” card, choose the <Ui>Organization</Ui> the post belongs to: the communications specialists of
                        that organization will be able to edit it. The <Ui>Entire cabinet (all organizations)</Ui> option is available only
                        to an administrator of all organizations. Published news is read by employees of all organizations, and the post's
                        card shows its organization.
                    </li>
                    <li>
                        Choose a <Ui>Category</Ui>. The <Ui>Published</Ui> checkbox makes the post visible to everyone (it is checked for a
                        new post; uncheck it to save a draft). <Ui>Pin to top</Ui> — the post will always be first. <Ui>Allow comments</Ui>{" "}
                        (on) — readers can comment; reactions are always available.
                    </li>
                    <li>
                        Optionally add a cover image: <Ui>Choose an image</Ui> — JPEG, PNG, WebP, AVIF or GIF up to 15 MB, preferably with a
                        16:9 aspect ratio (it is compressed to AVIF). To remove the current cover image, check <Ui>Remove cover image</Ui>.
                    </li>
                    <li>
                        Click <Ui>Save</Ui> — “Post saved”. For a published post, the <Ui>View</Ui> button opens it the way readers see it.
                    </li>
                </Steps>
                <ul>
                    <li>
                        The post's address is generated from the title when it is created and never changes, so links to the post don't
                        “break” after editing.
                    </li>
                    <li>If you unpublish a post and publish it again, the original publication date is kept.</li>
                    <li>
                        <Ui>Delete</Ui> (for an existing post) — after the “Delete the post permanently?” confirmation, the post is deleted
                        forever together with its photos, comments and reactions. To just hide a post, uncheck “Published”.
                    </li>
                </ul>
                <p>
                    Possible errors: “Enter a title.”, “Enter a summary.”, “Write the text of the post.”, “Wait until the photos are
                    uploaded.”, “The image cannot be larger than 15 MB.”, “Choose a JPEG, PNG, WebP, AVIF or GIF photo.”
                </p>
            </Topic>
            <Topic id="news-formatting">
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
                                Select text, click <Ui>Link</Ui>, enter the address and click <Ui>Apply</Ui>. http(s), mailto:, tel: and
                                internal links are allowed; external links open in a new tab
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
                                Several photos as tiles; readers open them full screen and browse. If the cursor is in a gallery, new photos
                                are added to it
                            </td>
                        </tr>
                    </tbody>
                </Table>
                <p>
                    <strong>Photos.</strong> JPEG, PNG, WebP, AVIF and GIF up to 15 MB are supported. Photos can also be pasted from the
                    clipboard (<kbd>Ctrl</kbd>+<kbd>V</kbd>) or dragged into the text. Each photo is uploaded to the server right away and
                    optimized: reduced to 1600 pixels, compressed to AVIF within 10–100 KB, its metadata (including location) removed.
                    Galleries also get thumbnails. A photo removed from the text is deleted when the post is saved.
                </p>
                <p>
                    For security, the server keeps only this markup: scripts, embedded pages, images from other sites and arbitrary styles
                    are removed. Posts written earlier in Markdown are shown as before and are saved in the new format after the first edit.
                </p>
            </Topic>
        </Chapter>
    );
}
