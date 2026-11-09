import newsListImage from "../../images/en/news-list.webp";
import newsArticleImage from "../../images/en/news-article.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "news-read" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>News</Ui> in the menu.
            </p>
            <Figure src={newsListImage} alt="The news feed" caption="The news feed with categories; pinned news come first." />
            <Figure src={newsArticleImage} alt="A news article" caption="A news article with reactions and the discussion." />
            <ul>
                <li>
                    Category tabs: <Ui>All</Ui>, “News”, “Announcement”, “Event”, “Training and development”, “Projects and achievements”.
                </li>
                <li>
                    The <Ui>Search news</Ui> field searches titles and summaries.
                </li>
                <li>
                    Cards show the cover image, category, the <Ui>Pinned</Ui> badge, title, summary, date, the author with a photo and the
                    numbers of views, reactions and comments. Pinned posts always come first, then from newest to oldest.
                </li>
                <li>There are 9 cards per page; the number can be changed (9, 18, 36, 72).</li>
            </ul>
            <p>
                Click a card to open the post: category, date, number of views, cover image, author, summary and full text. Photos in the
                text and in galleries open full screen: browse with the arrow keys, the buttons or a swipe, close with <kbd>Esc</kbd>. The{" "}
                <Ui>All news</Ui> button returns to the feed. The latest news is also shown on the home page.
            </p>
            <p>A view is counted once per reader. If a post has been deleted or unpublished — “News post not found”.</p>
        </>
    );
}
