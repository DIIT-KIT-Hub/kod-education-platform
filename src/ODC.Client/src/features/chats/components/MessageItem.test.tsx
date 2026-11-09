import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ChatMessage } from "@/features/chats/api";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { chatId, maria, makeMessage, makeMessagePage, me, meRef } from "../testing";
import { MessageItem } from "./MessageItem";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * Renders one message of a chat whose thread holds it.
 * @param message The message.
 * @param props Changed props.
 * @returns The rendered page and the editing callback.
 */
function renderMessage(message: ChatMessage, props: { seen?: boolean; editing?: boolean } = {}) {
    const onEditing = vi.fn();
    const page = renderPage(
        <ul>
            <MessageItem
                message={message}
                me={me.id}
                compact={false}
                showAuthor
                seen={props.seen ?? false}
                editing={props.editing ?? false}
                onEditing={onEditing}
                everyoneLabel="Також видалити для Марії"
            />
        </ul>,
        { path: "/chats/:chatId", url: `/chats/${chatId}`, user: me },
    );
    page.queryClient.setQueryData(["chats", "thread", chatId], makeMessagePage([message]));
    return { ...page, onEditing };
}

/**
 * The open confirmation dialog with the given title.
 * @param title The title.
 * @returns Queries inside the dialog.
 */
async function dialog(title: string) {
    return within((await screen.findByRole("heading", { name: title })).closest("dialog") as HTMLElement);
}

describe("MessageItem", () => {
    it("shows a colleague's message with reactions, forwarding and deleting but no editing", () => {
        renderMessage(makeMessage(1));

        expect(screen.getByText("Повідомлення 1")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Реакція" })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Переслати" })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Видалити" })).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Змінити" })).not.toBeInTheDocument();
    });

    it("lets the author edit their own message", async () => {
        const { user, onEditing } = renderMessage(makeMessage(1, { author: meRef }));

        await user.click(screen.getByRole("button", { name: "Змінити" }));

        expect(onEditing).toHaveBeenCalledWith(true);
    });

    it("does not let the author edit a forwarded message and names where it came from", () => {
        renderMessage(makeMessage(1, { author: meRef, forwardedFrom: maria }));

        expect(screen.getByText(/^Переслано від/)).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Змінити" })).not.toBeInTheDocument();
    });

    it("shows a deleted message as a placeholder without reactions or forwarding", () => {
        renderMessage(makeMessage(1, { isDeleted: true, body: "", text: "" }));

        expect(screen.getByText("Повідомлення видалено")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Реакція" })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Переслати" })).not.toBeInTheDocument();
    });

    it("marks an edited message and one the colleague has seen", () => {
        renderMessage(makeMessage(1, { author: meRef, editedAt: "2026-11-08T09:00:00Z" }), { seen: true });

        expect(screen.getByText("змінено")).toBeInTheDocument();
        expect(screen.getByText("Переглянуто")).toBeInTheDocument();
    });

    it("sets a reaction from the picker", async () => {
        const calls = mockApi(
            "post",
            "/api/v1/chats/{id}/messages/{messageId}/reactions/{kind}",
            makeMessage(1, { reactions: [{ kind: "Like", userIds: [me.id] }] }),
        );
        const { user } = renderMessage(makeMessage(1));

        await user.click(screen.getByRole("button", { name: "Реакція" }));
        // A click without pointer moves: happy-dom fires a stray mouseleave that closes the picker.
        fireEvent.click(within(screen.getByRole("group", { name: "Реакції" })).getByRole("button", { name: "Подобається" }));

        await waitFor(() => expect(calls[0]?.path).toBe(`/api/v1/chats/${chatId}/messages/${makeMessage(1).id}/reactions/Like`));
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("reports a failed reaction", async () => {
        mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/reactions/{kind}", () =>
            problem(400, "validation", "Повідомлення видалено."),
        );
        const { user } = renderMessage(makeMessage(1));

        await user.click(screen.getByRole("button", { name: "Реакція" }));
        // A click without pointer moves: happy-dom fires a stray mouseleave that closes the picker.
        fireEvent.click(within(screen.getByRole("group", { name: "Реакції" })).getByRole("button", { name: "Сумно" }));

        expect(await screen.findByText("Повідомлення видалено.")).toBeInTheDocument();
    });

    it("deletes the author's own message for everyone by default", async () => {
        const deleted = mockApi("delete", "/api/v1/chats/{id}/messages/{messageId}", makeMessage(1, { author: meRef, isDeleted: true }));
        const hidden = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/hide", makeMessage(1, { author: meRef, isHidden: true }));
        const { user } = renderMessage(makeMessage(1, { author: meRef }));

        await user.click(screen.getByRole("button", { name: "Видалити" }));
        const confirmation = await dialog("Видалити повідомлення?");
        expect(confirmation.getByRole("checkbox", { name: /Також видалити для Марії/ })).toBeChecked();
        await user.click(confirmation.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        await waitFor(() => expect(deleted).toHaveLength(1));
        expect(hidden).toHaveLength(0);
    });

    it("hides the author's own message only for them when the option is cleared", async () => {
        const deleted = mockApi("delete", "/api/v1/chats/{id}/messages/{messageId}", makeMessage(1, { author: meRef, isDeleted: true }));
        const hidden = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/hide", makeMessage(1, { author: meRef, isHidden: true }));
        const { user } = renderMessage(makeMessage(1, { author: meRef }));

        await user.click(screen.getByRole("button", { name: "Видалити" }));
        const confirmation = await dialog("Видалити повідомлення?");
        await user.click(confirmation.getByRole("checkbox", { name: /Також видалити для Марії/ }));
        await user.click(confirmation.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        await waitFor(() => expect(hidden).toHaveLength(1));
        expect(deleted).toHaveLength(0);
    });

    it("hides a colleague's message only for the user after confirmation", async () => {
        const hidden = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/hide", makeMessage(1, { isHidden: true }));
        const { user } = renderMessage(makeMessage(1));

        await user.click(screen.getByRole("button", { name: "Видалити" }));
        const confirmation = await dialog("Видалити повідомлення?");
        expect(confirmation.getByText(/зникне лише у Вас/)).toBeInTheDocument();
        await user.click(confirmation.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        await waitFor(() => expect(hidden).toHaveLength(1));
    });

    it("keeps the message when deleting is cancelled", async () => {
        const hidden = mockApi("post", "/api/v1/chats/{id}/messages/{messageId}/hide", makeMessage(1, { isHidden: true }));
        const { user } = renderMessage(makeMessage(1));

        await user.click(screen.getByRole("button", { name: "Видалити" }));
        await user.click((await dialog("Видалити повідомлення?")).getByRole("button", { name: "Скасувати" }));

        expect(hidden).toHaveLength(0);
    });

    it("shows a message being sent without actions", () => {
        renderMessage({ ...makeMessage(1, { author: meRef, clientId: "c-1" }), pending: { failed: false, files: [] } });

        expect(screen.getByText("Надсилання…")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Видалити" })).not.toBeInTheDocument();
    });

    it("sends a failed message again with the same client id", async () => {
        const calls = mockApi("post", "/api/v1/chats/{id}/messages", makeMessage(1, { author: meRef, clientId: "c-1" }));
        const { user } = renderMessage({ ...makeMessage(1, { author: meRef, clientId: "c-1" }), pending: { failed: true, files: [] } });

        expect(screen.getByText("Не надіслано")).toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: /Надіслати ще раз/ }));

        await waitFor(() => expect((calls[0]?.body as FormData).get("ClientId")).toBe("c-1"));
    });

    it("drops a failed message on request", async () => {
        const { user, queryClient } = renderMessage({
            ...makeMessage(1, { author: meRef, clientId: "c-1" }),
            pending: { failed: true, files: [] },
        });

        await user.click(screen.getByRole("button", { name: /Прибрати/ }));

        expect(queryClient.getQueryData<ReturnType<typeof makeMessagePage>>(["chats", "thread", chatId])?.messages).toHaveLength(0);
    });

    it("describes a system event instead of a message", () => {
        renderMessage(makeMessage(1, { kind: "System", event: "MembersAdded", text: "Петренко Петро Іванович", body: "" }));

        expect(screen.queryByRole("button", { name: "Реакція" })).not.toBeInTheDocument();
        expect(screen.getByRole("listitem")).toHaveTextContent(/Петренко/);
    });
});
