import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { emitLive } from "@/shared/realtime/fakeLive";
import { makeUserRef, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { makeArticle, makeComment, makeDiscussion } from "../testing";
import { ArticlePage } from "./ArticlePage";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/** The article id of the fixtures. */
const articleId = makeArticle().id;

/**
 * Renders an article with its discussion.
 * @param article The article.
 * @param discussion The discussion, or a responder.
 * @param state The history state (the discussion view).
 * @returns The rendered page and the discussion calls.
 */
function renderArticle(article = makeArticle(), discussion: Schemas["NewsDiscussion"] = makeDiscussion(), state?: unknown) {
    mockApi("get", "/api/v1/news/{slug}", article);
    const calls = mockApi("get", "/api/v1/news/{articleId}/discussion", discussion);
    return { ...renderPage(<ArticlePage />, { path: "/news/:slug", url: `/news/${article.slug}`, state }), calls };
}

/**
 * The query of the last discussion request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

/**
 * The article element of a comment by its author name.
 * @param name The author's full name.
 * @returns Queries inside the comment.
 */
function comment(name: string) {
    return within(screen.getByRole("article", { name: `Коментар: ${name}` }));
}

describe("ArticlePage", () => {
    it("shows the article with its category, author, views and body", async () => {
        renderArticle();

        expect(await screen.findByRole("heading", { level: 1, name: "Відкриття нового корпусу" })).toBeInTheDocument();
        expect(screen.getByText("Урочисте відкриття відбудеться у п'ятницю.")).toBeInTheDocument();
        expect(screen.getByText("Текст новини")).toBeInTheDocument();
        expect(screen.getByTitle("Переглядів")).toHaveTextContent("42");
        expect(screen.getByRole("link", { name: "Новини" })).toHaveAttribute("href", "/news");
        expect(screen.queryByRole("link", { name: "Редагувати" })).not.toBeInTheDocument();
    });

    it("marks a draft and offers editing to its managers", async () => {
        renderArticle(makeArticle({ publishedAt: null, canManage: true }));

        expect(await screen.findByText("Чернетка")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Редагувати" })).toHaveAttribute("href", `/news/manage/${articleId}`);
    });

    it("says when the article does not exist", async () => {
        mockApi("get", "/api/v1/news/{slug}", () => problem(404, "not_found", "Новину не знайдено або знято з публікації."));
        renderPage(<ArticlePage />, { path: "/news/:slug", url: "/news/missing" });

        expect(await screen.findByText("Новину не знайдено або знято з публікації.")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "До новин" })).toHaveAttribute("href", "/news");
    });
});

describe("Discussion", () => {
    it("invites the first comment and posts it with the CSRF header", async () => {
        const posted = mockApi("post", "/api/v1/news/{articleId}/comments", makeComment());
        const { user, calls } = renderArticle();

        expect(await screen.findByText("Коментарів ще немає. Будьте першими!")).toBeInTheDocument();
        expect(lastQuery(calls)).toEqual({ page: "1", pageSize: "10", sort: "Best" });
        const box = screen.getByRole("textbox", { name: "Новий коментар" });
        await user.type(box, "  Чудова новина  ");
        await user.click(screen.getByRole("button", { name: "Надіслати" }));

        await waitFor(() => expect(posted[0]?.body).toEqual({ body: "Чудова новина", parentId: null }));
        expect(posted[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        await waitFor(() => expect(box).toHaveValue(""));
    });

    it("does not send an empty comment", async () => {
        const { user } = renderArticle();

        await user.type(await screen.findByRole("textbox", { name: "Новий коментар" }), "   ");

        expect(screen.getByRole("button", { name: "Надіслати" })).toBeDisabled();
    });

    it("keeps a refused comment in the box and reports it", async () => {
        mockApi("post", "/api/v1/news/{articleId}/comments", () => problem(400, "validation", "Коментарі вимкнено."));
        const { user } = renderArticle();

        const box = await screen.findByRole("textbox", { name: "Новий коментар" });
        await user.type(box, "Текст");
        await user.click(screen.getByRole("button", { name: "Надіслати" }));

        expect(await screen.findByText("Коментарі вимкнено.")).toBeInTheDocument();
        expect(box).toHaveValue("Текст");
    });

    it("answers a comment and names whom a reply answers", async () => {
        const author = makeUserRef({ id: "u-2", fullName: "Бондар Олег" });
        const top = makeComment({
            author,
            replies: [makeComment({ depth: 1, author: makeUserRef({ id: "u-3", fullName: "Коваль Анна" }), replyTo: author })],
        });
        const posted = mockApi("post", "/api/v1/news/{articleId}/comments", makeComment());
        const { user } = renderArticle(makeArticle(), makeDiscussion({ comments: [top], commentCount: 2 }));

        await screen.findByRole("article", { name: "Коментар: Бондар Олег" });
        expect(comment("Коваль Анна").getByText(/у відповідь/)).toBeInTheDocument();
        await user.click(comment("Бондар Олег").getAllByRole("button", { name: "Відповісти" })[0] as HTMLElement);
        await user.type(screen.getByRole("textbox", { name: "Відповідь на коментар" }), "Згоден");
        await user.click(
            within(screen.getByRole("textbox", { name: "Відповідь на коментар" }).closest("form") as HTMLElement).getByRole("button", {
                name: "Відповісти",
            }),
        );

        await waitFor(() => expect(posted[0]?.body).toEqual({ body: "Згоден", parentId: top.id }));
    });

    it("lets the author edit and delete their comment, others only react", async () => {
        const mine = makeComment({ author: makeUserRef({ id: "u-me", fullName: "Шевченко Тарас" }), canEdit: true, canDelete: true });
        const other = makeComment({ author: makeUserRef({ id: "u-2", fullName: "Бондар Олег" }) });
        const edited = mockApi("put", "/api/v1/news/{articleId}/comments/{commentId}", mine);
        const deleted = mockApi("delete", "/api/v1/news/{articleId}/comments/{commentId}");
        const { user } = renderArticle(makeArticle(), makeDiscussion({ comments: [mine, other] }));

        await screen.findByRole("article", { name: "Коментар: Бондар Олег" });
        expect(comment("Бондар Олег").queryByRole("button", { name: "Змінити" })).not.toBeInTheDocument();
        expect(comment("Бондар Олег").queryByRole("button", { name: "Видалити" })).not.toBeInTheDocument();

        await user.click(comment("Шевченко Тарас").getByRole("button", { name: "Змінити" }));
        const text = screen.getByRole("textbox", { name: "Текст коментаря" });
        expect(text).toHaveValue(mine.body);
        await user.clear(text);
        await user.type(text, "Виправлений текст");
        await user.click(screen.getByRole("button", { name: "Зберегти" }));
        await waitFor(() => expect(edited[0]?.body).toEqual({ body: "Виправлений текст" }));

        await user.click(comment("Шевченко Тарас").getByRole("button", { name: "Видалити" }));
        const dialog = (await screen.findByRole("heading", { name: "Видалити коментар?" })).closest("dialog") as HTMLElement;
        await user.click(within(dialog).getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);
        await waitFor(() => expect(deleted[0]?.path).toBe(`/api/v1/news/${articleId}/comments/${mine.id}`));
        expect(await screen.findByText("Коментар видалено.")).toBeInTheDocument();
    });

    it("shows a deleted comment as a placeholder nobody can answer", async () => {
        const removed = makeComment({ isDeleted: true, body: "", replies: [makeComment({ depth: 1 })] });
        renderArticle(makeArticle(), makeDiscussion({ comments: [removed], commentCount: 2 }));

        const placeholder = await screen.findByRole("article", { name: "Коментар видалено." });
        expect(within(placeholder).getAllByRole("button", { name: "Відповісти" })).toHaveLength(1);
    });

    it("shows five replies and the rest on request", async () => {
        const replies = Array.from({ length: 7 }, (_, index) => makeComment({ depth: 1, body: `Відповідь ${index + 1}` }));
        const { user } = renderArticle(makeArticle(), makeDiscussion({ comments: [makeComment({ replies })], commentCount: 8 }));

        expect(await screen.findByText("Відповідь 5")).toBeInTheDocument();
        expect(screen.queryByText("Відповідь 6")).not.toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Ще відповідей: 2" }));

        expect(screen.getByText("Відповідь 7")).toBeInTheDocument();
    });

    it("sorts the threads and pages them", async () => {
        const comments = [makeComment(), makeComment()];
        const { user, calls } = renderArticle(makeArticle(), makeDiscussion({ comments, threadCount: 25, commentCount: 30 }));

        const order = await screen.findByRole("combobox", { name: "Порядок коментарів" });
        await user.click(order);
        const listbox = document.getElementById(order.getAttribute("aria-controls") ?? "") as HTMLElement;
        await user.click(within(listbox).getByRole("option", { name: "Нові" }));
        await waitFor(() => expect(lastQuery(calls).sort).toBe("Newest"));

        await user.click(screen.getByRole("link", { name: "Наступна сторінка" }));
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ page: "2", sort: "Newest" }));
    });

    it("explains why a draft or an article with comments off takes no comments", async () => {
        const page = renderArticle(makeArticle(), makeDiscussion({ canComment: false, canReact: false }));
        expect(await screen.findByText("Реакції та коментарі з'являться після публікації.")).toBeInTheDocument();
        page.unmount();

        renderArticle(makeArticle(), makeDiscussion({ canComment: false, canReact: true }));
        expect(await screen.findByText("Коментарі до цієї новини вимкнено.")).toBeInTheDocument();
        expect(screen.queryByRole("textbox", { name: "Новий коментар" })).not.toBeInTheDocument();
    });

    it("reloads the discussion when someone else changes it", async () => {
        const { calls } = renderArticle();
        await screen.findByText("Коментарів ще немає. Будьте першими!");
        const before = calls.length;

        emitLive("NewsDiscussionChanged", articleId);

        await waitFor(() => expect(calls.length).toBeGreaterThan(before));
    });

    it("shows the server's message when the comments cannot be loaded", async () => {
        mockApi("get", "/api/v1/news/{slug}", makeArticle());
        mockApi("get", "/api/v1/news/{articleId}/discussion", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<ArticlePage />, { path: "/news/:slug", url: "/news/new-building" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});
