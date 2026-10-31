import { Chapter, Table, Topic, Ui } from "../../components/Manual";

/**
 * The home page (English).
 * @returns The chapter.
 */
export function HomeChapter() {
    return (
        <Chapter id="home">
            <Topic id="home-overview">
                <p>
                    The home page opens right after you sign in, and also via the <Ui>Home</Ui> link or the “KOD” logo. It gathers in one
                    place everything that needs your attention.
                </p>
                <ul>
                    <li>
                        <strong>A greeting</strong> depending on the time of day (“Good morning”, “Good afternoon”…) with your name.
                    </li>
                    <li>
                        <strong>Quick actions</strong>: <Ui>New document</Ui> — opens document creation (requires the “Create documents”
                        permission); <Ui>Sign a file</Ui> — opens file signing (requires the “Signing and verifying files” permission).
                    </li>
                    <li>
                        <strong>Awaiting my signature</strong> — up to 5 documents where it is currently your turn to sign, shown as “No.
                        number · type · author”. Click a document to open it, or <Ui>All</Ui> to see the full list. If the queue is empty,
                        you will see “All signed” — “No new documents to sign.”
                    </li>
                    <li>
                        <strong>News</strong> — the three latest posts (pinned ones first), with category and date. <Ui>All</Ui> opens the
                        news feed. If there are no posts yet — “No news yet”.
                    </li>
                </ul>
                <p>Blocks you don't have permissions for are not shown.</p>
            </Topic>
            <Topic id="home-indicators">
                <p>Four cards with counters; each one is a link to the filtered document registry:</p>
                <Table>
                    <thead>
                        <tr>
                            <th>Card</th>
                            <th>What it counts</th>
                            <th>Where it leads</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>Awaiting my signature</td>
                            <td>
                                Documents where it is currently your turn. If there are more than zero, the card is highlighted in black.
                            </td>
                            <td>The “Awaiting my signature” tab</td>
                        </tr>
                        <tr>
                            <td>My drafts</td>
                            <td>Your documents with the “Draft” status.</td>
                            <td>“Created by me” + the “Draft” status</td>
                        </tr>
                        <tr>
                            <td>My documents in signing</td>
                            <td>Your documents that others are currently signing.</td>
                            <td>“Created by me” + “In signing”</td>
                        </tr>
                        <tr>
                            <td>Signed by me this month</td>
                            <td>Documents you have signed since the beginning of the current month.</td>
                            <td>“Signed by me”</td>
                        </tr>
                    </tbody>
                </Table>
            </Topic>
        </Chapter>
    );
}
