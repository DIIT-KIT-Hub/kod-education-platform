import { act, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { noteTyping } from "@/features/chats/typing";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { chatId, maria, makeDetails, makeMember, makeMessage, makeMessagePage, makeSummary, me, meRef, olena, petro } from "../testing";
import { ChatsPage } from "./ChatsPage";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * Renders the chats page at an address.
 * @param url The opened address.
 * @returns The rendered page.
 */
function renderChats(url = "/chats") {
    return renderPage(<ChatsPage />, { path: url === "/chats" ? "/chats" : "/chats/:chatId", url, user: me });
}

/**
 * Mocks an open one-to-one conversation with Maria.
 * @param messages The messages of the thread.
 */
function mockConversation(messages = [makeMessage(1), makeMessage(2, { author: meRef, body: "<p>Добре</p>", text: "Добре" })]) {
    mockApi("get", "/api/v1/chats", [makeSummary({ lastSequence: messages.length })]);
    mockApi("get", "/api/v1/chats/{id}", makeDetails({ lastSequence: messages.length }));
    mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage(messages));
    mockApi("put", "/api/v1/chats/{id}/read");
}

describe("ChatsPage", () => {
    it("lists the chats with their names, last messages and unread counts", async () => {
        mockApi("get", "/api/v1/chats", [
            makeSummary({
                unreadCount: 3,
                lastMessage: {
                    author: maria,
                    kind: "Text",
                    event: null,
                    text: "Перевірте акт",
                    isDeleted: false,
                    attachmentCount: 0,
                    createdAt: "2026-11-08T08:00:00Z",
                },
            }),
            makeSummary({ id: "chat-2", kind: "Group", title: "Бухгалтерія", members: [petro, olena], memberCount: 3, unreadCount: 150 }),
            makeSummary({
                id: "chat-3",
                members: [petro],
                lastMessage: {
                    author: meRef,
                    kind: "Text",
                    event: null,
                    text: "",
                    isDeleted: true,
                    attachmentCount: 0,
                    createdAt: "2026-11-08T08:00:00Z",
                },
            }),
        ]);
        renderChats();

        const list = await screen.findByRole("navigation", { name: "Список чатів" });
        const first = within(list).getByRole("link", { name: /Ковальчук|Марія|Перевірте акт/ });
        expect(first).toHaveAttribute("href", `/chats/${chatId}`);
        expect(within(first).getByText("Перевірте акт")).toBeInTheDocument();
        expect(within(first).getByLabelText("Непрочитаних: 3")).toHaveTextContent("3");
        expect(within(list).getByLabelText("Непрочитаних: 150")).toHaveTextContent("99+");
        expect(within(list).getByText("Бухгалтерія")).toBeInTheDocument();
        expect(within(list).getByText("Ви: Повідомлення видалено")).toBeInTheDocument();
        expect(screen.getByText("Оберіть чат")).toBeInTheDocument();
    });

    it("filters the list by name and says when nothing matches", async () => {
        mockApi("get", "/api/v1/chats", [
            makeSummary(),
            makeSummary({ id: "chat-2", kind: "Group", title: "Бухгалтерія", members: [petro], memberCount: 2 }),
        ]);
        const page = renderChats();
        await screen.findByText("Бухгалтерія");

        await page.user.type(screen.getByRole("searchbox", { name: /Пошук чатів/ }), "бухг{Enter}");
        await waitFor(() => expect(screen.queryByText(maria.fullName)).not.toBeInTheDocument());
        expect(screen.getByText("Бухгалтерія")).toBeInTheDocument();

        await page.user.clear(screen.getByRole("searchbox", { name: /Пошук чатів/ }));
        await page.user.type(screen.getByRole("searchbox", { name: /Пошук чатів/ }), "zzz{Enter}");
        expect(await screen.findByText("Нічого не знайдено.")).toBeInTheDocument();
    });

    it("invites to start the first conversation and opens the new chat dialog", async () => {
        mockApi("get", "/api/v1/chats", []);
        mockApi("get", "/api/v1/chats/people", []);
        const page = renderChats();

        expect(await screen.findByText("Чатів ще немає")).toBeInTheDocument();
        await page.user.click(screen.getAllByRole("button", { name: "Новий чат" })[0] as HTMLElement);

        expect(await screen.findByRole("heading", { name: "Новий чат" })).toBeInTheDocument();
    });

    it("says when the chats cannot be loaded", async () => {
        mockApi("get", "/api/v1/chats", () => problem(500, "error"));
        renderChats();

        expect(await screen.findByText("Не вдалося завантажити чати.")).toBeInTheDocument();
    });

    it("opens a conversation with its title, the colleague's position and the messages", async () => {
        mockConversation();
        renderChats(`/chats/${chatId}`);

        expect(await screen.findByRole("heading", { level: 1, name: maria.fullName })).toBeInTheDocument();
        expect(screen.getByText("Бухгалтер, Фінансовий відділ")).toBeInTheDocument();
        expect(await screen.findByText("Повідомлення 1")).toBeInTheDocument();
        expect(screen.getByText("Добре")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Про чат" })).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "До чатів" })).toHaveAttribute("href", "/chats");
    });

    it("names a group by its members count and offers the member list", async () => {
        mockApi("get", "/api/v1/chats", [makeSummary({ kind: "Group", title: "Проєкт", members: [maria, petro], memberCount: 3 })]);
        mockApi(
            "get",
            "/api/v1/chats/{id}",
            makeDetails({
                kind: "Group",
                title: "Проєкт",
                members: [makeMember(meRef, { role: "Owner" }), makeMember(maria), makeMember(petro)],
            }),
        );
        mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([]));
        renderChats(`/chats/${chatId}`);

        expect(await screen.findByRole("heading", { level: 1, name: "Проєкт" })).toBeInTheDocument();
        expect(screen.getByText("Учасників: 3")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Учасники" })).toBeInTheDocument();
    });

    it("shows who is typing in the conversation, but never the user", async () => {
        mockConversation();
        renderChats(`/chats/${chatId}`);
        await screen.findByText("Повідомлення 1");

        act(() => noteTyping(chatId, maria));
        act(() => noteTyping(chatId, meRef));

        expect(await screen.findByText(/пише…$/)).toBeInTheDocument();
        expect(screen.queryByText(/пишуть…$/)).not.toBeInTheDocument();
    });

    it("says that a chat the user left or that was deleted is not found", async () => {
        mockApi("get", "/api/v1/chats", []);
        mockApi("get", "/api/v1/chats/{id}", () => problem(404, "not_found"));
        mockApi("get", "/api/v1/chats/{id}/messages", () => problem(404, "not_found"));
        renderChats(`/chats/${chatId}`);

        expect(await screen.findByText("Чат не знайдено")).toBeInTheDocument();
    });

    it("tells the server how far the user has read", async () => {
        mockApi("get", "/api/v1/chats", [makeSummary({ lastSequence: 2, unreadCount: 2 })]);
        mockApi("get", "/api/v1/chats/{id}", makeDetails({ lastSequence: 2 }));
        mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([makeMessage(1), makeMessage(2)]));
        const read = mockApi("put", "/api/v1/chats/{id}/read");
        renderChats(`/chats/${chatId}`);

        await screen.findByText("Повідомлення 2");

        await waitFor(() => expect(read.at(-1)?.body).toEqual({ sequence: 2 }));
        expect(read.at(-1)?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });
});
