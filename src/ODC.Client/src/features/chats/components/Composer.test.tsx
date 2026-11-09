import { fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { sendLive } from "@/shared/realtime/fakeLive";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { chatId, makeFile, makeMessage, makeMessagePage, me } from "../testing";
import { Composer } from "./Composer";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * Renders the composer of a chat whose thread is already loaded.
 * @returns The rendered page.
 */
function renderComposer() {
    const page = renderPage(<Composer chatId={chatId} />, { path: "/chats/:chatId", url: `/chats/${chatId}`, user: me });
    page.queryClient.setQueryData(["chats", "thread", chatId], { messages: [], hasMore: false, lastSequence: 0 });
    return page;
}

/**
 * The editor of the message.
 * @returns The editable element.
 */
function editor(): HTMLElement {
    return screen.getByRole("textbox", { name: "Повідомлення" });
}

/**
 * The hidden file input of the composer.
 * @returns The input.
 */
function fileInput(): HTMLInputElement {
    return document.querySelector('input[type="file"]') as HTMLInputElement;
}

describe("Composer", () => {
    it("is empty at first and cannot send nothing", () => {
        renderComposer();

        expect(screen.getByRole("button", { name: "Надіслати" })).toBeDisabled();
    });

    it("sends the typed text with a client id and the CSRF header, then clears the editor", async () => {
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(1));
        const page = renderComposer();

        await page.user.click(editor());
        await page.user.keyboard("Привіт, колего");
        await page.user.click(screen.getByRole("button", { name: "Надіслати" }));

        await waitFor(() => expect(calls).toHaveLength(1));
        const form = calls[0]?.body as FormData;
        // A simulated click in happy-dom sometimes lands before the paragraph and leaves an empty one first.
        expect(form.get("Body")).toMatch(/^(<p><\/p>)?<p>Привіт, колего<\/p>$/);
        expect(form.get("ClientId")).toMatch(/^[0-9a-f-]{36}$/);
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        await waitFor(() => expect(editor()).toHaveTextContent(""));
    });

    it("tells the others the user is typing", async () => {
        sendLive.mockClear();
        const page = renderComposer();

        await page.user.click(editor());
        await page.user.keyboard("П");

        expect(sendLive).toHaveBeenCalledWith("Typing", chatId);
    });

    it("sends a file without text", async () => {
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(1));
        const page = renderComposer();

        fireEvent.change(fileInput(), { target: { files: [makeFile("act.pdf")] } });
        expect(await screen.findByText("act.pdf")).toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Надіслати" }));

        await waitFor(() => expect(calls).toHaveLength(1));
        const form = calls[0]?.body as FormData;
        // The empty body may be dropped by the test multipart parser; either way there is no text.
        expect(form.get("Body") ?? "").toBe("");
        // An ASCII name: the test multipart parser reads non-ASCII file names as Latin-1.
        expect((form.getAll("Files") as File[]).map((file) => file.name)).toEqual(["act.pdf"]);
    });

    it("removes a picked file", async () => {
        const page = renderComposer();

        fireEvent.change(fileInput(), { target: { files: [makeFile("акт.pdf")] } });
        await page.user.click(await screen.findByRole("button", { name: "Прибрати акт.pdf" }));

        expect(screen.queryByText("акт.pdf")).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Надіслати" })).toBeDisabled();
    });

    it("refuses forbidden types, oversized files and too many files", async () => {
        renderComposer();

        fireEvent.change(fileInput(), { target: { files: [makeFile("run.exe")] } });
        expect(await screen.findByText("Файли такого типу не можна надсилати в чат: run.exe")).toBeInTheDocument();

        fireEvent.change(fileInput(), { target: { files: [makeFile("big.pdf", 26 * 1024 * 1024)] } });
        expect(await screen.findByText("Файл більший за 25 МБ: big.pdf")).toBeInTheDocument();

        fireEvent.change(fileInput(), { target: { files: Array.from({ length: 11 }, (_, index) => makeFile(`f${index}.pdf`)) } });
        expect(await screen.findByText("До повідомлення можна додати не більше 10 файлів.")).toBeInTheDocument();

        fireEvent.change(fileInput(), {
            target: {
                files: [makeFile("a.pdf", 24 * 1024 * 1024), makeFile("b.pdf", 24 * 1024 * 1024), makeFile("c.pdf", 24 * 1024 * 1024)],
            },
        });
        expect(await screen.findByText("Файли одного повідомлення не можуть бути більшими за 50 МБ разом.")).toBeInTheDocument();
        expect(screen.queryByText("a.pdf")).not.toBeInTheDocument();
    });

    it("keeps a failed message in the thread and reports it", async () => {
        mockApi("post", "/api/v1/chats/{id}/messages", () => problem(400, "validation", "Чат закрито."));
        const page = renderComposer();

        await page.user.click(editor());
        await page.user.keyboard("Не дійде");
        await page.user.click(screen.getByRole("button", { name: "Надіслати" }));

        expect(await screen.findByText("Чат закрито.")).toBeInTheDocument();
        const thread = page.queryClient.getQueryData<ReturnType<typeof makeMessagePage>>(["chats", "thread", chatId]);
        expect(thread?.messages.map((message) => message.text)).toContain("Не дійде");
    });
});
