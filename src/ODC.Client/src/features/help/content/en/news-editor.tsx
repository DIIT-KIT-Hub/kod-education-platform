import newsEditorImage from "../../images/en/news-editor.webp";
import { Figure, Ref, Steps, Ui } from "../../components/Manual";

/**
 * The manual page "news-editor" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <Steps>
                <li>
                    Click <Ui>New post</Ui> (in the feed or on the management page) or <Ui>Edit</Ui> next to an existing one.
                </li>
                <li>
                    Fill in the <Ui>Title</Ui> (up to 200 characters) and <Ui>Summary</Ui> (up to 500 characters, 1–2 sentences for the
                    card).
                </li>
                <li>
                    Write the <Ui>Text</Ui> in the visual editor (up to 50,000 characters) — headings, lists, tables, photos and galleries,
                    see “<Ref to="news-formatting">Text formatting, photos and galleries</Ref>”.
                </li>
                <li>
                    In the “Publication” card, choose the <Ui>Organization</Ui> the post belongs to: the communications specialists of that
                    organization will be able to edit it. The <Ui>Entire cabinet (all organizations)</Ui> option is available only to an
                    administrator of all organizations. Published news is read by employees of all organizations, and the post's card shows
                    its organization.
                </li>
                <li>
                    Choose a <Ui>Category</Ui>. The <Ui>Published</Ui> checkbox makes the post visible to everyone (it is checked for a new
                    post; uncheck it to save a draft). <Ui>Pin to top</Ui> — the post will always be first. <Ui>Allow comments</Ui> (on) —
                    readers can comment; reactions are always available.
                </li>
                <li>
                    Optionally add a cover image: <Ui>Choose an image</Ui> — JPEG, PNG, WebP, AVIF or GIF up to 15 MB, preferably with a
                    16:9 aspect ratio (the cabinet makes the file smaller itself). To remove the current cover image, check{" "}
                    <Ui>Remove cover image</Ui>.
                </li>
                <li>
                    Click <Ui>Save</Ui> — “Post saved”. For a published post, the <Ui>View</Ui> button opens it the way readers see it.
                </li>
            </Steps>
            <Figure
                src={newsEditorImage}
                alt="The news editor"
                caption="The news editor: the title, the summary, the formatted text and the cover."
            />
            <ul>
                <li>
                    The post's address is generated from the title when it is created and never changes, so links to the post don't “break”
                    after editing.
                </li>
                <li>If you unpublish a post and publish it again, the original publication date is kept.</li>
                <li>
                    <Ui>Delete</Ui> (for an existing post) — after the “Delete the post permanently?” confirmation, the post is deleted
                    forever together with its photos, comments and reactions. To just hide a post, uncheck “Published”.
                </li>
            </ul>
            <p>
                Possible errors: “Enter a title.”, “Enter a summary.”, “Write the text of the post.”, “Wait until the photos are uploaded.”,
                “The image cannot be larger than 15 MB.”, “Choose a JPEG, PNG, WebP, AVIF or GIF photo.”
            </p>
        </>
    );
}
