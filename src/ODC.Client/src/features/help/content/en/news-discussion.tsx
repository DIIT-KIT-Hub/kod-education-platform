import { Ui } from "../../components/Manual";

/**
 * The manual page "news-discussion" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Reactions and comments are below the text of a post. New comments and reactions of other readers appear without reloading
                the page.
            </p>
            <ul>
                <li>
                    <strong>Reactions.</strong> Click <Ui>React</Ui> (the smiley with a plus) and choose an emotion: 👍 Like, 👎 Dislike, ❤️
                    Love, 👏 Applause, 🎉 Congratulations, 😄 Funny, 😮 Wow, 🤔 Doubtful, 😢 Sad, 😡 Outrageous. Each reader has one
                    reaction per post: another emotion replaces the previous one, clicking your own again removes it. Comments can be
                    reacted to the same way.
                </li>
                <li>
                    <strong>Comment.</strong> Type in the “Write a comment…” field (up to 2,000 characters) and click <Ui>Send</Ui> (or
                    press Ctrl+Enter). Comments are shown as threads, 10 per page; choose the order in <Ui>Comment order</Ui>: <Ui>Best</Ui>{" "}
                    (more reactions and replies), <Ui>Newest</Ui> or <Ui>Oldest</Ui>.
                </li>
                <li>
                    <strong>Reply.</strong> <Ui>Reply</Ui> under any comment opens a reply field — the reply goes under that very comment,
                    so a thread can have several levels. Above a reply you see whom it answers (“in reply to …”). The line to the left of a
                    thread collapses it (<Ui>Collapse the thread</Ui>). The first 5 replies are shown under a comment, <Ui>More replies</Ui>{" "}
                    opens the rest; very deep threads open on their own with <Ui>Continue this thread</Ui>, and{" "}
                    <Ui>Back to all comments</Ui> returns to the discussion.
                </li>
                <li>
                    <strong>Your comment</strong> can be edited with <Ui>Edit</Ui> (it gets an “(edited)” mark) or deleted with{" "}
                    <Ui>Delete</Ui>. If someone has already replied, “The comment was deleted.” stays in its place so the thread holds
                    together; nobody can reply to it any more, and it goes away with its last reply.
                </li>
                <li>
                    <strong>Moderation.</strong> News editors of the organization the post belongs to can delete any comment under it; such
                    deletions are recorded in the audit log (“Comment deleted”).
                </li>
            </ul>
            <p>
                Only published posts can be commented on. If the editor has turned comments off, the post shows “Comments on this post are
                turned off.” — reactions still work.
            </p>
        </>
    );
}
