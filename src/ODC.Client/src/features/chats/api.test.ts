import type { QueryClient } from "@tanstack/react-query";
import { beforeEach, describe, expect, it } from "vitest";
import { setAntiforgeryToken } from "@/shared/api/client";
import type { ChatDetails, ChatSummary } from "@/shared/api/schema";
import { createTestQueryClient } from "@/test/render";
import { json, mockApi, problem } from "@/test/server";
import {
    applyRead,
    catchUp,
    chatApi,
    chatKeys,
    chatQuery,
    chatsQuery,
    loadOlder,
    mergeMessages,
    peopleQuery,
    receiveMessage,
    threadQuery,
    touchList,
    type ChatMessage,
    type ChatThread,
} from "./api";
import { chatId, makeDetails, makeMember, makeMessage, makeMessagePage, makeSummary, maria, me, meRef, petro } from "./testing";

/**
 * A message still on its way from this page.
 * @param clientId The client identifier.
 * @param failed Whether sending failed.
 * @param createdAt The moment it was written.
 * @returns The message.
 */
function pendingMessage(clientId: string, failed = false, createdAt = "2026-11-08T09:00:00.000Z"): ChatMessage {
    return { ...makeMessage(0, { id: `local:${clientId}`, clientId, author: meRef, createdAt }), pending: { failed, files: [] } };
}

/**
 * The numbers of the messages of a thread (pending ones as their client id).
 * @param thread The thread.
 * @returns The numbers.
 */
function numbers(thread: ChatThread | undefined): (number | string)[] {
    return (thread?.messages ?? []).map((m) => (m.pending ? (m.clientId ?? "") : m.sequence));
}

/**
 * Reads the loaded thread of the test chat.
 * @param queryClient The query client.
 * @returns The thread.
 */
function threadOf(queryClient: QueryClient): ChatThread | undefined {
    return queryClient.getQueryData<ChatThread>(chatKeys.thread(chatId));
}

describe("mergeMessages", () => {
    it("adds new messages and orders them by number", () => {
        const thread: ChatThread = { messages: [makeMessage(3)], hasOlder: false };

        const merged = mergeMessages(thread, [makeMessage(5), makeMessage(1), makeMessage(4)]);

        expect(numbers(merged)).toEqual([1, 3, 4, 5]);
        expect(merged.hasOlder).toBe(false);
        expect(thread.messages).toHaveLength(1);
    });

    it("replaces a message with the same id (an edit)", () => {
        const thread: ChatThread = { messages: [makeMessage(1), makeMessage(2)], hasOlder: true };

        const merged = mergeMessages(thread, [makeMessage(2, { text: "Змінено", editedAt: "2026-11-08T09:00:00Z" })]);

        expect(merged.messages).toHaveLength(2);
        expect(merged.messages[1]?.text).toBe("Змінено");
        expect(merged.hasOlder).toBe(true);
    });

    it("keeps pending messages at the end, by the moment they were written", () => {
        const thread: ChatThread = {
            messages: [pendingMessage("b", false, "2026-11-08T09:00:02.000Z"), makeMessage(2)],
            hasOlder: false,
        };

        const merged = mergeMessages(thread, [pendingMessage("a", false, "2026-11-08T09:00:01.000Z"), makeMessage(1)]);

        expect(numbers(merged)).toEqual([1, 2, "a", "b"]);
    });

    it("replaces a pending message by the saved one with the same client id", () => {
        const thread: ChatThread = { messages: [makeMessage(1), pendingMessage("c-1")], hasOlder: false };

        const merged = mergeMessages(thread, [makeMessage(2, { clientId: "c-1", author: meRef })]);

        expect(numbers(merged)).toEqual([1, 2]);
        expect(merged.messages[1]?.pending).toBeUndefined();
    });

    it("marks a pending message as failed by its client id", () => {
        const thread: ChatThread = { messages: [pendingMessage("c-1")], hasOlder: false };

        const merged = mergeMessages(thread, [pendingMessage("c-1", true)]);

        expect(merged.messages).toHaveLength(1);
        expect(merged.messages[0]?.pending?.failed).toBe(true);
    });

    it("does not bring back the pending state with a late echo of a saved message", () => {
        const saved = makeMessage(2, { clientId: "c-1", author: meRef });
        const thread: ChatThread = { messages: [saved], hasOlder: false };

        const merged = mergeMessages(thread, [pendingMessage("c-1", true)]);

        expect(merged.messages).toEqual([saved]);
    });

    it("does not match two saved messages with the same client id (only pending ones match by it)", () => {
        const thread: ChatThread = { messages: [makeMessage(1, { clientId: "same" })], hasOlder: false };

        const merged = mergeMessages(thread, [makeMessage(2, { clientId: "same" })]);

        expect(numbers(merged)).toEqual([1, 2]);
    });

    it("keeps a message the user hid hidden when the others change it", () => {
        const hidden = makeMessage(1, { isHidden: true, body: "", text: "" });
        const thread: ChatThread = { messages: [hidden], hasOlder: false };

        const merged = mergeMessages(thread, [makeMessage(1, { text: "Змінено іншими" })]);

        expect(merged.messages[0]).toEqual(hidden);
    });

    it("hides a message when the hidden placeholder arrives", () => {
        const thread: ChatThread = { messages: [makeMessage(1)], hasOlder: false };

        const merged = mergeMessages(thread, [makeMessage(1, { isHidden: true, body: "", text: "" })]);

        expect(merged.messages[0]?.isHidden).toBe(true);
    });
});

describe("receiveMessage and catchUp", () => {
    let queryClient: QueryClient;

    beforeEach(() => {
        queryClient = createTestQueryClient();
    });

    it("ignores a message of a conversation that is not loaded", () => {
        receiveMessage(queryClient, makeMessage(1));

        expect(threadOf(queryClient)).toBeUndefined();
    });

    it("puts a message that follows the newest one into the thread without a request", () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });

        receiveMessage(queryClient, makeMessage(2));

        expect(numbers(threadOf(queryClient))).toEqual([1, 2]);
    });

    it("fetches the missing messages after a gap with ?after=", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1), makeMessage(2)], hasOlder: false });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([makeMessage(3), makeMessage(4), makeMessage(5)]));

        receiveMessage(queryClient, makeMessage(5));

        expect(numbers(threadOf(queryClient))).toEqual([1, 2, 5]);
        await expect.poll(() => numbers(threadOf(queryClient))).toEqual([1, 2, 3, 4, 5]);
        expect(calls).toHaveLength(1);
        expect(calls[0]?.path).toBe(`/api/v1/chats/${chatId}/messages`);
        expect(calls[0]?.query.get("after")).toBe("2");
        expect(calls[0]?.query.get("limit")).toBe("100");
    });

    it("swallows a failed catch-up of a gap", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", () => problem(500, "error"));

        receiveMessage(queryClient, makeMessage(3));

        await expect.poll(() => calls.length).toBe(1);
        expect(numbers(threadOf(queryClient))).toEqual([1, 3]);
    });

    it("does nothing when the conversation is not loaded", async () => {
        await catchUp(queryClient, chatId);

        expect(threadOf(queryClient)).toBeUndefined();
    });

    it("reloads a conversation without saved messages", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [pendingMessage("c")], hasOlder: false });
        // Nothing shows the thread here, so it is only marked stale (a shown conversation would load it again).
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([makeMessage(1)]));

        await catchUp(queryClient, chatId);

        expect(queryClient.getQueryCache().find({ queryKey: chatKeys.thread(chatId) })?.state.isInvalidated).toBe(true);
        expect(calls).toHaveLength(0);
    });

    it("pages through the newer messages until the newest one", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", ({ query }) =>
            json(
                query.get("after") === "1"
                    ? makeMessagePage([makeMessage(2), makeMessage(3)], true)
                    : makeMessagePage([makeMessage(4)], false),
            ),
        );

        await catchUp(queryClient, chatId);

        expect(calls.map((c) => c.query.get("after"))).toEqual(["1", "3"]);
        expect(numbers(threadOf(queryClient))).toEqual([1, 2, 3, 4]);
    });

    it("stops when a page says more but brings nothing", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([], true));

        await catchUp(queryClient, chatId);

        expect(calls).toHaveLength(1);
    });

    it("gives up after twenty pages", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", ({ query }) => {
            const after = Number(query.get("after"));
            return json(makeMessagePage([makeMessage(after + 1)], true));
        });

        await catchUp(queryClient, chatId);

        expect(calls).toHaveLength(20);
    });

    it("starts from the newest message without a gap, ignoring pending ones", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), {
            messages: [makeMessage(1), makeMessage(2), pendingMessage("c")],
            hasOlder: false,
        });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([]));

        await catchUp(queryClient, chatId);

        expect(calls[0]?.query.get("after")).toBe("2");
    });

    it("leaves a thread removed meanwhile alone", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        mockApi("get", "/api/v1/chats/{id}/messages", () => {
            queryClient.removeQueries({ queryKey: chatKeys.thread(chatId) });
            return json(makeMessagePage([makeMessage(2)]));
        });

        await catchUp(queryClient, chatId);

        expect(threadOf(queryClient)).toBeUndefined();
    });
});

describe("loadOlder", () => {
    let queryClient: QueryClient;

    beforeEach(() => {
        queryClient = createTestQueryClient();
    });

    it("loads the page before the oldest message", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(51), makeMessage(52)], hasOlder: true });
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([makeMessage(49), makeMessage(50)], false));

        await loadOlder(queryClient, chatId);

        expect(calls[0]?.query.get("before")).toBe("51");
        expect(calls[0]?.query.get("limit")).toBe("50");
        expect(numbers(threadOf(queryClient))).toEqual([49, 50, 51, 52]);
        expect(threadOf(queryClient)?.hasOlder).toBe(false);
    });

    it("does nothing when there are no older messages or no saved ones", async () => {
        const calls = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([]));
        await loadOlder(queryClient, chatId);

        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
        await loadOlder(queryClient, chatId);

        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [pendingMessage("c")], hasOlder: true });
        await loadOlder(queryClient, chatId);

        expect(calls).toHaveLength(0);
    });

    it("throws when the page cannot be loaded", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(2)], hasOlder: true });
        mockApi("get", "/api/v1/chats/{id}/messages", () => problem(500, "error", "Збій."));

        await expect(loadOlder(queryClient, chatId)).rejects.toMatchObject({ status: 500, detail: "Збій." });
    });

    it("leaves a thread removed meanwhile alone", async () => {
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(2)], hasOlder: true });
        mockApi("get", "/api/v1/chats/{id}/messages", () => {
            queryClient.removeQueries({ queryKey: chatKeys.thread(chatId) });
            return json(makeMessagePage([makeMessage(1)]));
        });

        await loadOlder(queryClient, chatId);

        expect(threadOf(queryClient)).toBeUndefined();
    });
});

describe("touchList", () => {
    let queryClient: QueryClient;
    const other = makeSummary({ id: "10000000-0000-7000-8000-000000000002", lastSequence: 9 });

    beforeEach(() => {
        queryClient = createTestQueryClient();
    });

    it("says the chat is unknown when the list is not loaded", () => {
        expect(touchList(queryClient, makeMessage(1), me.id)).toBe(false);
    });

    it("says the chat is unknown when it is not in the list", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [other]);

        expect(touchList(queryClient, makeMessage(1), me.id)).toBe(false);
    });

    it("moves the chat to the top with the preview and one more unread message", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [other, makeSummary({ lastSequence: 4, lastReadSequence: 3, unreadCount: 1 })]);
        const message = makeMessage(5, {
            attachments: [
                {
                    id: "a",
                    kind: "File",
                    fileName: "a.pdf",
                    contentType: "application/pdf",
                    size: 1,
                    width: null,
                    height: null,
                    url: "/a",
                    thumbnailUrl: null,
                },
            ],
        });

        expect(touchList(queryClient, message, me.id)).toBe(true);

        const [first, second] = queryClient.getQueryData<ChatSummary[]>(chatKeys.list) ?? [];
        expect(second?.id).toBe(other.id);
        expect(first).toMatchObject({
            id: chatId,
            lastSequence: 5,
            lastActivityAt: message.createdAt,
            lastReadSequence: 3,
            unreadCount: 2,
            lastMessage: { author: maria, kind: "Text", text: "Повідомлення 5", isDeleted: false, attachmentCount: 1 },
        });
    });

    it("does not count a system note as unread", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [makeSummary({ lastSequence: 4, unreadCount: 1 })]);

        touchList(queryClient, makeMessage(5, { kind: "System", event: "MemberLeft" }), me.id);

        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)?.[0]?.unreadCount).toBe(1);
    });

    it("marks the chat read up to an own message", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [makeSummary({ lastSequence: 4, lastReadSequence: 2, unreadCount: 2 })]);

        touchList(queryClient, makeMessage(5, { author: meRef }), me.id);

        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)?.[0]).toMatchObject({ lastReadSequence: 5, unreadCount: 0 });
    });

    it("ignores a message the list already knows (an edit or an echo)", () => {
        const known = makeSummary({ lastSequence: 5, unreadCount: 1 });
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [other, known]);

        expect(touchList(queryClient, makeMessage(5), me.id)).toBe(true);

        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)).toEqual([other, known]);
    });
});

describe("applyRead", () => {
    let queryClient: QueryClient;

    beforeEach(() => {
        queryClient = createTestQueryClient();
    });

    it("moves the read mark of a member forward only", () => {
        queryClient.setQueryData<ChatDetails>(
            chatKeys.details(chatId),
            makeDetails({ members: [makeMember(meRef, { lastReadSequence: 5 }), makeMember(maria, { lastReadSequence: 3 })] }),
        );

        applyRead(queryClient, chatId, maria.id, 4, me.id);
        applyRead(queryClient, chatId, maria.id, 2, me.id);

        const members = queryClient.getQueryData<ChatDetails>(chatKeys.details(chatId))?.members;
        expect(members?.map((m) => m.lastReadSequence)).toEqual([5, 4]);
    });

    it("does not touch the list for another member", () => {
        const list = [makeSummary({ lastSequence: 5, unreadCount: 2 })];
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, list);

        applyRead(queryClient, chatId, maria.id, 5, me.id);

        expect(queryClient.getQueryData(chatKeys.list)).toBe(list);
    });

    it("clears the unread count when the user read to the end", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [makeSummary({ lastSequence: 5, lastReadSequence: 2, unreadCount: 3 })]);

        applyRead(queryClient, chatId, me.id, 5, me.id);

        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)?.[0]).toMatchObject({ lastReadSequence: 5, unreadCount: 0 });
    });

    it("reloads the list when the user read only part of the chat", () => {
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [makeSummary({ lastSequence: 9, lastReadSequence: 2, unreadCount: 7 })]);

        applyRead(queryClient, chatId, me.id, 5, me.id);

        expect(queryClient.getQueryCache().find({ queryKey: chatKeys.list })?.state.isInvalidated).toBe(true);
        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)?.[0]?.unreadCount).toBe(7);
    });

    it("ignores an older read mark and chats that are not listed", () => {
        const list = [makeSummary({ lastSequence: 9, lastReadSequence: 6, unreadCount: 3 })];
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, list);

        applyRead(queryClient, chatId, me.id, 5, me.id);
        applyRead(queryClient, "other", me.id, 50, me.id);

        expect(queryClient.getQueryData(chatKeys.list)).toBe(list);
        expect(queryClient.getQueryCache().find({ queryKey: chatKeys.list })?.state.isInvalidated).toBe(false);
    });

    it("works without a loaded chat or list", () => {
        applyRead(queryClient, chatId, me.id, 5, me.id);

        expect(queryClient.getQueryData(chatKeys.details(chatId))).toBeUndefined();
        expect(queryClient.getQueryData(chatKeys.list)).toBeUndefined();
    });
});

describe("chat queries", () => {
    it("loads the list, a chat and the newest page of messages", async () => {
        const queryClient = createTestQueryClient();
        mockApi("get", "/api/v1/chats", [makeSummary()]);
        mockApi("get", "/api/v1/chats/{id}", makeDetails());
        const messages = mockApi("get", "/api/v1/chats/{id}/messages", makeMessagePage([makeMessage(1)], true));

        expect(await queryClient.fetchQuery(chatsQuery)).toEqual([makeSummary()]);
        expect(await queryClient.fetchQuery(chatQuery(chatId))).toEqual(makeDetails());
        expect(await queryClient.fetchQuery(threadQuery(chatId))).toEqual({ messages: [makeMessage(1)], hasOlder: true });
        expect(messages[0]?.query.get("limit")).toBe("50");
    });

    it("searches people only from two characters", async () => {
        const queryClient = createTestQueryClient();
        const calls = mockApi("get", "/api/v1/chats/people", []);

        expect(peopleQuery("К").enabled).toBe(false);
        expect(peopleQuery(" К ").enabled).toBe(false);
        expect(peopleQuery("Ко").enabled).toBe(true);
        await queryClient.fetchQuery(peopleQuery("Ко"));

        expect(calls[0]?.query.get("term")).toBe("Ко");
    });
});

describe("chatApi", () => {
    beforeEach(() => {
        setAntiforgeryToken("csrf-test-token");
    });

    it("opens a one-to-one chat", async () => {
        const calls = mockApi("post", "/api/v1/chats/direct", { id: chatId });

        expect(await chatApi.openDirect(maria.id)).toEqual({ id: chatId });

        expect(calls[0]?.body).toEqual({ userId: maria.id });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("creates a group with a trimmed title or none", async () => {
        const calls = mockApi("post", "/api/v1/chats", { id: chatId });

        await chatApi.createGroup("  Відділ ", [maria.id, petro.id]);
        await chatApi.createGroup("   ", [maria.id]);

        expect(calls.map((c) => c.body)).toEqual([
            { title: "Відділ", memberIds: [maria.id, petro.id] },
            { title: null, memberIds: [maria.id] },
        ]);
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("renames, adds and removes members, clears and deletes", async () => {
        const rename = mockApi("put", "/api/v1/chats/{id}/title");
        const add = mockApi("post", "/api/v1/chats/{id}/members");
        const remove = mockApi("delete", "/api/v1/chats/{id}/members/{userId}");
        const clear = mockApi("post", "/api/v1/chats/{id}/clear");
        const drop = mockApi("delete", "/api/v1/chats/{id}");

        await chatApi.rename(chatId, " Нова ");
        await chatApi.rename(chatId, "");
        await chatApi.addMembers(chatId, [petro.id]);
        await chatApi.removeMember(chatId, petro.id);
        await chatApi.clearHistory(chatId);
        await chatApi.deleteChat(chatId);

        expect(rename.map((c) => c.body)).toEqual([{ title: "Нова" }, { title: null }]);
        expect(add[0]?.body).toEqual({ userIds: [petro.id] });
        expect(remove[0]?.path).toBe(`/api/v1/chats/${chatId}/members/${petro.id}`);
        expect(clear[0]?.path).toBe(`/api/v1/chats/${chatId}/clear`);
        expect(drop[0]?.path).toBe(`/api/v1/chats/${chatId}`);
        for (const call of [...rename, ...add, ...remove, ...clear, ...drop]) {
            expect(call.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        }
    });

    it("sends a message as a form with the client id, the body and the files", async () => {
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(1));
        const file = new File(["data"], "report.pdf", { type: "application/pdf" });

        await chatApi.send(chatId, "client-1", "<p>Привіт</p>", [file]);

        const form = calls[0]?.body as FormData;
        expect(form.get("ClientId")).toBe("client-1");
        expect(form.get("Body")).toBe("<p>Привіт</p>");
        expect((form.getAll("Files")[0] as File).name).toBe("report.pdf");
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("edits, forwards, deletes, hides, reacts and marks read", async () => {
        const edit = mockApi("put", "/api/v1/chats/{id}/messages/{messageId}", makeMessage(1));
        const forward = mockApi("post", "/api/v1/chats/{id}/forward", [makeMessage(2)]);
        const remove = mockApi("delete", "/api/v1/chats/{id}/messages/{messageId}");
        const hide = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/hide", makeMessage(1, { isHidden: true }));
        const react = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/reactions/{kind}", makeMessage(1));
        const read = mockApi("put", "/api/v1/chats/{id}/read");
        const id = makeMessage(1).id;

        await chatApi.edit(chatId, id, "<p>Нове</p>");
        await chatApi.forward("target", chatId, [id]);
        await chatApi.remove(chatId, id);
        expect(await chatApi.hide(chatId, id)).toMatchObject({ isHidden: true });
        await chatApi.react(chatId, id, "Love");
        await chatApi.markRead(chatId, 7);

        expect(edit[0]?.body).toEqual({ body: "<p>Нове</p>" });
        expect(forward[0]?.path).toBe("/api/v1/chats/target/forward");
        expect(forward[0]?.body).toEqual({ sourceChatId: chatId, messageIds: [id] });
        expect(remove[0]?.path).toBe(`/api/v1/chats/${chatId}/messages/${id}`);
        expect(hide[0]?.path).toBe(`/api/v1/chats/${chatId}/messages/${id}/hide`);
        expect(react[0]?.path).toBe(`/api/v1/chats/${chatId}/messages/${id}/reactions/Love`);
        expect(read[0]?.body).toEqual({ sequence: 7 });
        for (const call of [...edit, ...forward, ...remove, ...hide, ...react, ...read]) {
            expect(call.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        }
    });

    it("turns a failure into an error with the server's message", async () => {
        mockApi("post", "/api/v1/chats/direct", () => problem(404, "not_found", "Користувача не знайдено."));

        await expect(chatApi.openDirect(maria.id)).rejects.toMatchObject({ status: 404, code: "not_found", detail: "Користувача не знайдено." });
    });
});
