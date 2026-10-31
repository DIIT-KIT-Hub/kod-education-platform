import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, CornerDownRight, MessageSquare, Minus, Pencil, Plus, Reply, Send, Trash2 } from "lucide-react";
import { useRef, useState, type ReactNode } from "react";
import { useSearchParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { CommentSort, NewsCommentDto, NewsDiscussion, ReactionKind } from "@/shared/api/schema";
import { formatAgo, formatDateTime } from "@/shared/i18n/format";
import { useLiveGroup } from "@/shared/realtime/live";
import { useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Avatar } from "@/shared/ui/Avatar";
import { Button } from "@/shared/ui/Button";
import { confirm } from "@/shared/ui/ConfirmHost";
import { Pagination } from "@/shared/ui/Pagination";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { toast } from "@/shared/ui/Toast";
import { discussionQuery, newsApi, newsKeys } from "../api";
import styles from "./Discussion.module.css";
import { ReactionBar } from "./ReactionBar";

/** The replies shown under a comment before "more replies". */
const visibleReplies = 5;

/** The deepest level drawn in place; deeper branches open on their own ("continue this thread"). */
const maxVisibleDepth = 6;

/** The longest comment (matches NewsComment.BodyMaxLength). */
const bodyMaxLength = 2000;

/** The orders of the threads. */
const sorts: readonly CommentSort[] = ["Best", "Newest", "Oldest"];

/**
 * What the discussion does with comments, shared by every comment of the tree.
 */
interface DiscussionActions {
    readonly discussion: NewsDiscussion;
    readonly replyTo: string | null;
    readonly editing: string | null;
    readonly setReplyTo: (id: string | null) => void;
    readonly setEditing: (id: string | null) => void;
    readonly react: (commentId: string | null, kind: ReactionKind) => Promise<void>;
    readonly comment: (parentId: string | null, body: string) => Promise<boolean>;
    readonly edit: (commentId: string, body: string) => Promise<boolean>;
    readonly remove: (commentId: string) => Promise<void>;
    readonly focus: (comment: NewsCommentDto) => void;
}

/**
 * The reactions and the comments of an article, like a forum thread: replies go under the very comment they answer,
 * branches are drawn with lines that fold them, long branches show the first replies and deep ones continue on their
 * own. Threads come in pages; open pages update live over the application's SignalR connection.
 * @param props The article.
 * @returns The discussion.
 */
export function Discussion({ articleId }: { readonly articleId: string }) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const [params, setParams] = useSearchParams();
    const page = Math.max(1, Number(params.get("comments")) || 1);
    const sort = sorts.find((item) => item === params.get("sort")) ?? "Best";
    const discussion = useQuery(discussionQuery(articleId, page, sort));
    const [replyTo, setReplyTo] = useState<string | null>(null);
    const [editing, setEditing] = useState<string | null>(null);
    const [focused, setFocused] = useState<NewsCommentDto | null>(null);
    const [error, setError] = useState<string | null>(null);
    const section = useRef<HTMLElement>(null);

    const reload = () => queryClient.invalidateQueries({ queryKey: newsKeys.discussion(articleId) });

    // Comments and reactions of other readers appear at once.
    useLiveGroup(
        `news:${articleId}`,
        (hub) => hub.invoke("JoinNewsDiscussion", articleId),
        (hub) => hub.invoke("LeaveNewsDiscussion", articleId),
        "NewsDiscussionChanged",
        () => void reload(),
    );

    /**
     * Runs a change and loads the discussion again.
     * @param change The call.
     * @returns Whether it succeeded.
     */
    async function run(change: () => Promise<unknown>): Promise<boolean> {
        setError(null);
        try {
            await change();
            await reload();
            return true;
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
            return false;
        }
    }

    if (discussion.isPending) {
        return <Spinner block />;
    }

    if (discussion.isError) {
        return <Alert tone="danger">{describeError(discussion.error, t`Не вдалося завантажити коментарі.`)}</Alert>;
    }

    const data = discussion.data;
    const actions: DiscussionActions = {
        discussion: data,
        replyTo,
        editing,
        setReplyTo: (id) => {
            setEditing(null);
            setReplyTo(id);
        },
        setEditing: (id) => {
            setReplyTo(null);
            setEditing(id);
        },
        react: async (commentId, kind) => {
            await run(() => newsApi.react(articleId, commentId, kind));
        },
        comment: async (parentId, body) => {
            const done = await run(() => newsApi.comment(articleId, parentId, body));
            if (done && parentId) {
                setReplyTo(null);
            }

            return done;
        },
        edit: async (commentId, body) => {
            const done = await run(() => newsApi.editComment(articleId, commentId, body));
            if (done) {
                setEditing(null);
            }

            return done;
        },
        remove: async (commentId) => {
            if (await confirm({ title: t`Видалити коментар?`, confirmText: t`Видалити`, danger: true })) {
                if (await run(() => newsApi.deleteComment(articleId, commentId))) {
                    toast(t`Коментар видалено.`);
                }
            }
        },
        focus: (comment) => {
            setFocused(comment);
            section.current?.scrollIntoView({ behavior: "smooth", block: "start" });
        },
    };

    // The focused branch follows the reloads (its replies may change), or closes when it is gone.
    const branch = focused ? findComment(data.comments, focused.id) : null;
    const commentCount = data.commentCount;

    return (
        <section ref={section} className={styles.discussion} aria-labelledby="discussion-title">
            <ReactionBar summary={data.reactions} canReact={data.canReact} onReact={(kind) => actions.react(null, kind)} />

            <header className={styles.header}>
                <h2 id="discussion-title" className={styles.title}>
                    {t`Коментарі`} <span className={styles.count}>{commentCount}</span>
                </h2>
                {data.threadCount > 1 && (
                    <Select
                        compact
                        aria-label={t`Порядок коментарів`}
                        value={sort}
                        onChange={(value) => {
                            const next = new URLSearchParams(params);
                            next.set("sort", value);
                            next.delete("comments");
                            setParams(next, { replace: true, preventScrollReset: true });
                        }}
                        options={[
                            { value: "Best", label: t`Найкращі` },
                            { value: "Newest", label: t`Нові` },
                            { value: "Oldest", label: t`Старі` },
                        ]}
                    />
                )}
            </header>

            {error && <Alert tone="danger">{error}</Alert>}

            {data.canComment ? (
                <CommentForm
                    placeholder={t`Напишіть коментар…`}
                    label={t`Новий коментар`}
                    submitText={t`Надіслати`}
                    onSubmit={(body) => actions.comment(null, body)}
                />
            ) : (
                <p className={[layout.muted, layout.textSm].join(" ")}>
                    {data.canReact ? t`Коментарі до цієї новини вимкнено.` : t`Реакції та коментарі з'являться після публікації.`}
                </p>
            )}

            {branch ? (
                <>
                    <Button variant="ghost" size="sm" icon={ArrowLeft} onClick={() => setFocused(null)}>
                        {t`До всіх коментарів`}
                    </Button>
                    <ol className={styles.thread}>
                        <CommentNode comment={branch} actions={actions} depthOffset={branch.depth} />
                    </ol>
                </>
            ) : (
                <>
                    {data.comments.length === 0 && data.canComment && (
                        <p className={[layout.muted, layout.textSm].join(" ")}>{t`Коментарів ще немає. Будьте першими!`}</p>
                    )}
                    <ol className={styles.thread}>
                        {data.comments.map((comment) => (
                            <CommentNode key={comment.id} comment={comment} actions={actions} depthOffset={0} />
                        ))}
                    </ol>
                    {data.threadCount > data.pageSize && (
                        <Pagination
                            page={data.page}
                            pageSize={data.pageSize}
                            totalCount={data.threadCount}
                            pageParam="comments"
                            pageSizes={null}
                        />
                    )}
                </>
            )}
        </section>
    );
}

/**
 * Finds a comment anywhere in the threads.
 * @param comments The threads.
 * @param id The comment.
 * @returns The comment, or null.
 */
function findComment(comments: readonly NewsCommentDto[], id: string): NewsCommentDto | null {
    for (const comment of comments) {
        if (comment.id === id) {
            return comment;
        }

        const found = findComment(comment.replies, id);
        if (found) {
            return found;
        }
    }

    return null;
}

/**
 * Props of {@link CommentNode}.
 */
interface CommentNodeProps {
    readonly comment: NewsCommentDto;
    readonly actions: DiscussionActions;
    /** The depth of the branch root when a branch is opened on its own. */
    readonly depthOffset: number;
}

/**
 * A comment with its branch: the avatar, a line down the branch (a click folds it), the text, the actions and the
 * replies.
 * @param props The props.
 * @returns The comment.
 */
function CommentNode({ comment, actions, depthOffset }: CommentNodeProps) {
    const { t } = useLingui();
    const [collapsed, setCollapsed] = useState(false);
    const [showAll, setShowAll] = useState(false);
    const depth = comment.depth - depthOffset;
    const replyCount = comment.replyCount;
    const authorName = comment.author.fullName;
    const parentName = comment.replyTo?.fullName ?? "";
    const hidden = comment.replies.length - visibleReplies;
    const replies = showAll ? comment.replies : comment.replies.slice(0, visibleReplies);
    const tooDeep = depth >= maxVisibleDepth && comment.replies.length > 0;
    const canAnswer = actions.discussion.canComment && !comment.isDeleted;

    if (collapsed) {
        return (
            <li className={styles.node}>
                <button type="button" className={styles.collapsed} onClick={() => setCollapsed(false)} aria-expanded={false}>
                    <span className={styles.toggle}>
                        <Plus aria-hidden="true" />
                    </span>
                    <span className={layout.strong}>{comment.isDeleted ? t`Коментар видалено.` : authorName}</span>
                    <span className={styles.meta}>· {formatAgo(comment.createdAt)}</span>
                    {replyCount > 0 && <span className={styles.meta}>· {t`відповідей: ${replyCount}`}</span>}
                </button>
            </li>
        );
    }

    return (
        <li className={styles.node}>
            <article className={styles.comment} aria-label={comment.isDeleted ? t`Коментар видалено.` : t`Коментар: ${authorName}`}>
                <div className={styles.rail}>
                    <Avatar name={authorName} photoId={comment.author.photoId} size={depth === 0 ? "md" : "sm"} decorative />
                    {(comment.replies.length > 0 || actions.replyTo === comment.id) && (
                        <button
                            type="button"
                            className={styles.line}
                            title={t`Згорнути гілку`}
                            aria-label={t`Згорнути гілку`}
                            aria-expanded
                            onClick={() => setCollapsed(true)}
                        >
                            <span className={styles.lineMark} aria-hidden="true">
                                <Minus />
                            </span>
                        </button>
                    )}
                </div>

                <div className={styles.body}>
                    <header className={styles.commentHeader}>
                        <span className={layout.strong}>{comment.isDeleted ? t`[видалено]` : authorName}</span>
                        <time className={styles.meta} dateTime={comment.createdAt} title={formatDateTime(comment.createdAt)}>
                            {formatAgo(comment.createdAt)}
                        </time>
                        {comment.editedAt && (
                            <span className={styles.meta} title={formatDateTime(comment.editedAt)}>
                                {t`(змінено)`}
                            </span>
                        )}
                    </header>
                    {comment.replyTo && (
                        <span className={styles.replyTo}>
                            <CornerDownRight aria-hidden="true" />
                            {t`у відповідь ${parentName}`}
                        </span>
                    )}

                    {comment.isDeleted ? (
                        <p className={[styles.text, layout.muted].join(" ")}>{t`Коментар видалено.`}</p>
                    ) : actions.editing === comment.id ? (
                        <CommentForm
                            initial={comment.body}
                            label={t`Текст коментаря`}
                            submitText={t`Зберегти`}
                            onSubmit={(body) => actions.edit(comment.id, body)}
                            onCancel={() => actions.setEditing(null)}
                        />
                    ) : (
                        <>
                            <p className={styles.text}>{comment.body}</p>
                            <div className={styles.actions}>
                                <ReactionBar
                                    compact
                                    summary={comment.reactions}
                                    canReact={actions.discussion.canReact}
                                    onReact={(kind) => actions.react(comment.id, kind)}
                                />
                                {canAnswer && (
                                    <CommentAction
                                        icon={<Reply />}
                                        onClick={() => actions.setReplyTo(actions.replyTo === comment.id ? null : comment.id)}
                                    >
                                        {t`Відповісти`}
                                    </CommentAction>
                                )}
                                {comment.canEdit && (
                                    <CommentAction icon={<Pencil />} onClick={() => actions.setEditing(comment.id)}>
                                        {t`Змінити`}
                                    </CommentAction>
                                )}
                                {comment.canDelete && (
                                    <CommentAction icon={<Trash2 />} onClick={() => void actions.remove(comment.id)}>
                                        {t`Видалити`}
                                    </CommentAction>
                                )}
                            </div>
                        </>
                    )}

                    {actions.replyTo === comment.id && (
                        <CommentForm
                            label={t`Відповідь на коментар`}
                            placeholder={t`Ваша відповідь для ${authorName}…`}
                            submitText={t`Відповісти`}
                            autoFocus
                            onSubmit={(body) => actions.comment(comment.id, body)}
                            onCancel={() => actions.setReplyTo(null)}
                        />
                    )}

                    {tooDeep ? (
                        <button type="button" className={styles.more} onClick={() => actions.focus(comment)}>
                            <MessageSquare aria-hidden="true" />
                            {t`Продовжити гілку (${replyCount}) →`}
                        </button>
                    ) : (
                        comment.replies.length > 0 && (
                            <ol className={styles.replies}>
                                {replies.map((reply) => (
                                    <CommentNode key={reply.id} comment={reply} actions={actions} depthOffset={depthOffset} />
                                ))}
                                {!showAll && hidden > 0 && (
                                    <li>
                                        <button type="button" className={styles.more} onClick={() => setShowAll(true)}>
                                            <Plus aria-hidden="true" />
                                            {t`Ще відповідей: ${hidden}`}
                                        </button>
                                    </li>
                                )}
                            </ol>
                        )
                    )}
                </div>
            </article>
        </li>
    );
}

/**
 * A small text button under a comment.
 * @param props The icon, the label and the action.
 * @returns The button.
 */
function CommentAction({
    icon,
    children,
    onClick,
}: {
    readonly icon: ReactNode;
    readonly children: ReactNode;
    readonly onClick: () => void;
}) {
    return (
        <button type="button" className={styles.action} onClick={onClick}>
            {icon}
            {children}
        </button>
    );
}

/**
 * Props of {@link CommentForm}.
 */
interface CommentFormProps {
    readonly label: string;
    readonly submitText: string;
    readonly onSubmit: (body: string) => Promise<boolean>;
    readonly onCancel?: () => void;
    readonly initial?: string;
    readonly placeholder?: string;
    readonly autoFocus?: boolean;
}

/**
 * The text of a new comment, a reply or an edit; Ctrl+Enter sends it.
 * @param props The props.
 * @returns The form.
 */
function CommentForm({ label, submitText, onSubmit, onCancel, initial = "", placeholder, autoFocus }: CommentFormProps) {
    const { t } = useLingui();
    const user = useCurrentUser();
    const [text, setText] = useState(initial);
    const [busy, setBusy] = useState(false);

    /**
     * Sends the text and clears the form when it was accepted.
     */
    async function send(): Promise<void> {
        if (!text.trim() || busy) {
            return;
        }

        setBusy(true);
        if (await onSubmit(text.trim())) {
            setText("");
        }

        setBusy(false);
    }

    return (
        <form
            className={styles.form}
            onSubmit={(event) => {
                event.preventDefault();
                void send();
            }}
        >
            {!onCancel && <Avatar name={user.fullName} photoId={user.photoId} decorative />}
            <div className={styles.formBody}>
                <Textarea
                    className={styles.input}
                    rows={2}
                    maxLength={bodyMaxLength}
                    value={text}
                    placeholder={placeholder}
                    aria-label={label}
                    autoFocus={autoFocus}
                    onChange={(event) => setText(event.target.value)}
                    onKeyDown={(event) => {
                        if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
                            event.preventDefault();
                            void send();
                        } else if (event.key === "Escape" && onCancel) {
                            onCancel();
                        }
                    }}
                />
                <div className={styles.formActions}>
                    {onCancel && (
                        <Button variant="ghost" size="sm" onClick={onCancel}>
                            {t`Скасувати`}
                        </Button>
                    )}
                    <Button type="submit" size="sm" icon={Send} busy={busy} disabled={!text.trim()}>
                        {submitText}
                    </Button>
                </div>
            </div>
        </form>
    );
}
