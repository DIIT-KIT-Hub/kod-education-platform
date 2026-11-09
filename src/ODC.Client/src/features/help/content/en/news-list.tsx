import newsManageImage from "../../images/en/news-manage.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "news-list" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                In the news feed, click <Ui>Manage</Ui> (or <Ui>Administration</Ui> → <Ui>News</Ui>). Here you see the posts of the
                organizations whose news you manage (an administrator of all organizations sees all of them), including drafts — only
                editors can see them.
            </p>
            <Figure src={newsManageImage} alt="Managing the news" caption="Managing the news: drafts, published and pinned articles." />
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
        </>
    );
}
