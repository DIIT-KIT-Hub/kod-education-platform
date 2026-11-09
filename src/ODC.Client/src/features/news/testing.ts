import { makeUserRef, type Schemas } from "@/test/factories";

/*
 * Fixtures of the news tests (articles, comments, discussions). Only the tests of this feature import them.
 */

/**
 * An article of a list.
 * @param overrides Changed fields.
 * @returns The list item.
 */
export function makeNewsItem(overrides: Partial<Schemas["NewsListItem"]> = {}): Schemas["NewsListItem"] {
    return {
        id: "10000000-0000-7000-8000-000000000001",
        slug: "new-building",
        title: "Відкриття нового корпусу",
        summary: "Урочисте відкриття відбудеться у п'ятницю.",
        category: "News",
        coverImageUrl: null,
        coverThumbnailUrl: null,
        isPinned: false,
        publishedAt: "2026-11-05T09:00:00Z",
        authorName: "Коваленко Марія Андріївна",
        authorPhotoId: null,
        updatedAt: "2026-11-05T09:00:00Z",
        organization: null,
        commentCount: 3,
        reactionCount: 7,
        viewCount: 42,
        ...overrides,
    };
}

/**
 * An article as the article page and the editor load it.
 * @param overrides Changed fields.
 * @returns The article.
 */
export function makeArticle(overrides: Partial<Schemas["NewsArticleDto"]> = {}): Schemas["NewsArticleDto"] {
    return {
        id: "10000000-0000-7000-8000-000000000001",
        slug: "new-building",
        title: "Відкриття нового корпусу",
        summary: "Урочисте відкриття відбудеться у п'ятницю.",
        body: "<p>Текст новини</p>",
        bodyFormat: "Html",
        bodyHtml: "<p>Текст новини</p>",
        category: "Announcement",
        coverImageUrl: null,
        isPinned: false,
        publishedAt: "2026-11-05T09:00:00Z",
        author: makeUserRef(),
        updatedAt: "2026-11-05T09:00:00Z",
        organization: null,
        allowComments: true,
        viewCount: 42,
        canManage: false,
        ...overrides,
    };
}

/**
 * No reactions yet.
 * @param overrides Changed fields.
 * @returns The summary.
 */
export function makeReactions(overrides: Partial<Schemas["ReactionSummary"]> = {}): Schemas["ReactionSummary"] {
    return { counts: [], mine: null, ...overrides };
}

/** The sequence of generated comment ids. */
let commentSequence = 0;

/**
 * A comment.
 * @param overrides Changed fields.
 * @returns The comment.
 */
export function makeComment(overrides: Partial<Schemas["NewsCommentDto"]> = {}): Schemas["NewsCommentDto"] {
    commentSequence++;
    const replies = overrides.replies ?? [];
    return {
        id: `20000000-0000-7000-8000-${String(commentSequence).padStart(12, "0")}`,
        parentId: null,
        replyTo: null,
        depth: 0,
        author: makeUserRef(),
        body: `Коментар ${commentSequence}`,
        createdAt: "2026-11-08T09:00:00Z",
        editedAt: null,
        isDeleted: false,
        reactions: makeReactions(),
        canEdit: false,
        canDelete: false,
        replyCount: replies.length,
        replies,
        ...overrides,
    };
}

/**
 * A discussion of an article.
 * @param overrides Changed fields.
 * @returns The discussion.
 */
export function makeDiscussion(overrides: Partial<Schemas["NewsDiscussion"]> = {}): Schemas["NewsDiscussion"] {
    const comments = overrides.comments ?? [];
    return {
        reactions: makeReactions(),
        comments,
        commentCount: comments.length,
        threadCount: comments.length,
        page: 1,
        pageSize: 10,
        sort: "Best",
        canReact: true,
        canComment: true,
        ...overrides,
    };
}
