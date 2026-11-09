import type { QueryClient } from "@tanstack/react-query";
import { beforeEach, describe, expect, it } from "vitest";
import { setAntiforgeryToken } from "@/shared/api/client";
import type { ChatSummary } from "@/shared/api/schema";
import * as attachments from "@/shared/lib/attachments";
import { createTestQueryClient } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { chatKeys, type ChatThread } from "./api";
import { discardDraft, fileAccept, isAllowedFile, maxFiles, maxFileSize, maxTotalSize, sendDraft, type Draft } from "./send";
import { chatId, makeFile, makeMessage, makeSummary, meRef } from "./testing";

/**
 * A message to send.
 * @param overrides Changed fields.
 * @returns The draft.
 */
function draft(overrides: Partial<Draft> = {}): Draft {
    return { clientId: "client-1", body: "<p>Привіт</p>", text: "Привіт", files: [], ...overrides };
}

/**
 * Reads the loaded thread of the test chat.
 * @param queryClient The query client.
 * @returns The thread.
 */
function threadOf(queryClient: QueryClient): ChatThread | undefined {
    return queryClient.getQueryData<ChatThread>(chatKeys.thread(chatId));
}

describe("sendDraft", () => {
    let queryClient: QueryClient;

    beforeEach(() => {
        setAntiforgeryToken("csrf-test-token");
        queryClient = createTestQueryClient();
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [makeMessage(1)], hasOlder: false });
    });

    it("shows the message as pending at once and replaces it with the saved one", async () => {
        let seenWhileSending: ChatThread | undefined;
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", () => {
            seenWhileSending = threadOf(queryClient);
            return Response.json(makeMessage(2, { clientId: "client-1", author: meRef, text: "Привіт" }));
        });
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, [makeSummary({ lastSequence: 1 })]);

        await expect(sendDraft(queryClient, chatId, meRef, draft())).resolves.toBe(true);

        const pending = seenWhileSending?.messages.at(-1);
        expect(pending).toMatchObject({
            id: "local:client-1",
            clientId: "client-1",
            author: meRef,
            text: "Привіт",
            pending: { failed: false, files: [] },
        });
        expect(threadOf(queryClient)?.messages.map((m) => [m.sequence, Boolean(m.pending)])).toEqual([
            [1, false],
            [2, false],
        ]);
        expect(queryClient.getQueryData<ChatSummary[]>(chatKeys.list)?.[0]).toMatchObject({ lastSequence: 2, unreadCount: 0 });
        expect((calls[0]?.body as FormData).get("ClientId")).toBe("client-1");
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("reloads the chat list when the chat is not in it", async () => {
        mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(2, { clientId: "client-1", author: meRef }));
        queryClient.setQueryData<ChatSummary[]>(chatKeys.list, []);

        await sendDraft(queryClient, chatId, meRef, draft());

        expect(queryClient.getQueryCache().find({ queryKey: chatKeys.list })?.state.isInvalidated).toBe(true);
    });

    it("keeps a failed message with its files and rethrows", async () => {
        mockApi("post", "/api/v1/chats/{id}/messages", () => problem(400, "invalid", "Порожнє повідомлення."));
        const file = makeFile("a.pdf");

        await expect(sendDraft(queryClient, chatId, meRef, draft({ files: [file] }))).rejects.toMatchObject({
            detail: "Порожнє повідомлення.",
        });

        expect(threadOf(queryClient)?.messages.at(-1)?.pending).toEqual({ failed: true, files: [file] });
    });

    it("sends again with the same client id and lands once", async () => {
        let fail = true;
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", () =>
            fail ? problem(503, "unavailable") : Response.json(makeMessage(2, { clientId: "client-1", author: meRef })),
        );

        await expect(sendDraft(queryClient, chatId, meRef, draft())).rejects.toBeDefined();
        fail = false;
        await sendDraft(queryClient, chatId, meRef, draft());

        expect(calls.map((c) => (c.body as FormData).get("ClientId"))).toEqual(["client-1", "client-1"]);
        expect(threadOf(queryClient)?.messages.map((m) => m.sequence)).toEqual([1, 2]);
    });

    it("works when the conversation is not loaded", async () => {
        queryClient.removeQueries({ queryKey: chatKeys.thread(chatId) });
        mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(2, { clientId: "client-1", author: meRef }));

        await expect(sendDraft(queryClient, chatId, meRef, draft())).resolves.toBe(true);

        expect(threadOf(queryClient)).toBeUndefined();
    });
});

describe("discardDraft", () => {
    it("removes only the pending message with the client id", () => {
        const queryClient = createTestQueryClient();
        const saved = makeMessage(1, { clientId: "client-1" });
        const pending = { ...makeMessage(0, { id: "local:client-1", clientId: "client-1" }), pending: { failed: true, files: [] } };
        const other = { ...makeMessage(0, { id: "local:client-2", clientId: "client-2" }), pending: { failed: true, files: [] } };
        queryClient.setQueryData<ChatThread>(chatKeys.thread(chatId), { messages: [saved, pending, other], hasOlder: false });

        discardDraft(queryClient, chatId, "client-1");

        expect(threadOf(queryClient)?.messages).toEqual([saved, other]);
    });

    it("does nothing without a loaded conversation", () => {
        const queryClient = createTestQueryClient();

        discardDraft(queryClient, chatId, "client-1");

        expect(threadOf(queryClient)).toBeUndefined();
    });
});

describe("attachment rules", () => {
    it("re-exports the shared allow-list and limits", () => {
        expect(fileAccept).toBe(attachments.fileAccept);
        expect(isAllowedFile).toBe(attachments.isAllowedFile);
        expect([maxFiles, maxFileSize, maxTotalSize]).toEqual([10, 25 * 1024 * 1024, 50 * 1024 * 1024]);
    });

    it.each(["звіт.pdf", "Фото.JPG", "архів.7z", "дані.p7s", "пакет.asice", "голос.m4a"])("allows %s", (name) => {
        expect(isAllowedFile(makeFile(name))).toBe(true);
    });

    it.each(["setup.exe", "script.js", "page.html", "image.svg", "README", "pdf", "archive.tar.gz"])("refuses %s", (name) => {
        expect(isAllowedFile(makeFile(name))).toBe(false);
    });

    it("lists the extensions for the file input", () => {
        expect(fileAccept.split(",")).toContain(".pdf");
        expect(fileAccept.split(",")).not.toContain(".exe");
    });
});
