import homeImage from "../../images/en/home.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "home-overview" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The home page opens right after you sign in, and also via the <Ui>Home</Ui> link or the “ODC” logo. It gathers in one place
                everything that needs your attention.
            </p>
            <Figure src={homeImage} alt="The home page" caption="The home page after signing in." />
            <ul>
                <li>
                    <strong>A greeting</strong> depending on the time of day (“Good morning”, “Good afternoon”…) with your name.
                </li>
                <li>
                    <strong>Quick actions</strong>: <Ui>New document</Ui> — opens document creation (requires the “Create documents”
                    permission); <Ui>Sign a file</Ui> — opens file signing (requires the “Signing and verifying files” permission).
                </li>
                <li>
                    <strong>Awaiting my signature</strong> — up to 5 documents where it is currently your turn to sign, shown as “No. number
                    · type · author”. Click a document to open it, or <Ui>All</Ui> to see the full list. If the queue is empty, you will see
                    “All signed” — “No new documents to sign.”
                </li>
                <li>
                    <strong>News</strong> — the three latest posts (pinned ones first), with category and date. <Ui>All</Ui> opens the news
                    feed. If there are no posts yet — “No news yet”.
                </li>
            </ul>
            <p>Blocks you don't have permissions for are not shown.</p>
        </>
    );
}
