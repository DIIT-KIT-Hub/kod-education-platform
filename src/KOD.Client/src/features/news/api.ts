import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { CommentSort, MediaDto, NewsCategory, ReactionKind } from "@/shared/api/schema";
import { shrinkImage } from "@/shared/lib/image";

/**
 * The filters and paging of a news list (from the address).
 */
export interface NewsFilters {
    readonly category?: NewsCategory | undefined;
    readonly search?: string | undefined;
    readonly page: number;
    readonly pageSize: number;
    /** Drafts too (the management list). */
    readonly includeDrafts?: boolean;
    readonly sortBy?: string | undefined;
    readonly descending?: boolean;
}

/**
 * The keys of the news queries (invalidated after changes).
 */
export const newsKeys = {
    all: ["news"] as const,
    discussion: (articleId: string) => ["news", "discussion", articleId] as const,
};

/**
 * A page of articles.
 * @param filters The filters.
 * @returns The query.
 */
export function newsListQuery(filters: NewsFilters) {
    return queryOptions({
        queryKey: ["news", "list", filters],
        queryFn: () =>
            unwrap(
                api.GET("/api/v1/news", {
                    params: {
                        query: {
                            Category: filters.category,
                            Search: filters.search,
                            Page: filters.page,
                            PageSize: filters.pageSize,
                            IncludeDrafts: filters.includeDrafts,
                            SortBy: filters.sortBy,
                            Descending: filters.descending,
                        },
                    },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * An article by its address.
 * @param slug The URL identifier.
 * @returns The query.
 */
export function articleQuery(slug: string) {
    return queryOptions({
        queryKey: ["news", "article", slug],
        queryFn: () => unwrap(api.GET("/api/v1/news/{slug}", { params: { path: { slug } } })),
    });
}

/**
 * An article for editing.
 * @param id The article.
 * @returns The query.
 */
export function articleForEditQuery(id: string) {
    return queryOptions({
        queryKey: ["news", "edit", id],
        queryFn: () => unwrap(api.GET("/api/v1/news/{id}", { params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * The organizations the user publishes news for.
 */
export const newsOrganizationsQuery = queryOptions({
    queryKey: ["news", "organizations"],
    queryFn: () => unwrap(api.GET("/api/v1/news/organizations")),
    staleTime: 5 * 60_000,
});

/**
 * The reactions and one page of comment threads of an article.
 * @param articleId The article.
 * @param page The page of threads.
 * @param sort The order of the threads.
 * @returns The query.
 */
export function discussionQuery(articleId: string, page: number, sort: CommentSort) {
    return queryOptions({
        queryKey: [...newsKeys.discussion(articleId), page, sort],
        queryFn: () =>
            unwrap(
                api.GET("/api/v1/news/{articleId}/discussion", { params: { path: { articleId }, query: { page, pageSize: 10, sort } } }),
            ),
        placeholderData: (previous) => previous,
        staleTime: 0,
    });
}

/**
 * The fields of an article.
 */
export interface ArticleForm {
    readonly title: string;
    readonly summary: string;
    readonly body: string;
    readonly category: NewsCategory;
    readonly isPinned: boolean;
    readonly publish: boolean;
    readonly removeCover: boolean;
    readonly organizationId: string | null;
    readonly allowComments: boolean;
}

/**
 * The changes of articles and their discussion.
 */
export const newsApi = {
    create: async (form: ArticleForm): Promise<string> =>
        ((await unwrap(api.POST("/api/v1/news", { body: form }))) as unknown as { id: string }).id,
    update: (id: string, form: ArticleForm) => unwrap(api.PUT("/api/v1/news/{id}", { params: { path: { id } }, body: form })),
    cover: (id: string, file: File) => {
        const data = new FormData();
        data.append("file", file);
        return unwrap(api.PUT("/api/v1/news/{id}/cover", { params: { path: { id } }, body: {} as never, bodySerializer: () => data }));
    },
    remove: (id: string) => unwrap(api.DELETE("/api/v1/news/{id}", { params: { path: { id } } })),
    react: (articleId: string, commentId: string | null, kind: ReactionKind) =>
        commentId
            ? unwrap(
                  api.POST("/api/v1/news/{articleId}/comments/{commentId}/reactions/{kind}", {
                      params: { path: { articleId, commentId, kind } },
                  }),
              )
            : unwrap(api.POST("/api/v1/news/{articleId}/reactions/{kind}", { params: { path: { articleId, kind } } })),
    comment: (articleId: string, parentId: string | null, body: string) =>
        unwrap(api.POST("/api/v1/news/{articleId}/comments", { params: { path: { articleId } }, body: { body, parentId } })),
    editComment: (articleId: string, commentId: string, body: string) =>
        unwrap(api.PUT("/api/v1/news/{articleId}/comments/{commentId}", { params: { path: { articleId, commentId } }, body: { body } })),
    deleteComment: (articleId: string, commentId: string) =>
        unwrap(api.DELETE("/api/v1/news/{articleId}/comments/{commentId}", { params: { path: { articleId, commentId } } })),
};

/**
 * Uploads an image of an article text (a large photo is shrunk first; the server stores AVIF).
 * @param file The image.
 * @returns The stored image.
 */
export async function uploadImage(file: File): Promise<MediaDto> {
    const data = new FormData();
    data.append("file", await shrinkImage(file, 2400));
    return unwrap(api.POST("/api/v1/media", { body: {} as never, bodySerializer: () => data }));
}
