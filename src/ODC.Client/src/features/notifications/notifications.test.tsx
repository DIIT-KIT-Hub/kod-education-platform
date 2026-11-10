import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { i18n } from "@/shared/i18n/i18n";
import { emitLive, reconnectLive } from "@/shared/realtime/fakeLive";
import { authContextQuery } from "@/shared/session/session";
import { makeAuthContext, makeOrganization, makePage, makeUserRef, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { notificationKeys } from "./api";
import { NotificationSync } from "./components/NotificationSync";
import { notificationTitle } from "./display";
import { NotificationsPage } from "./pages/NotificationsPage";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * A notification as the server returns it.
 * @param overrides Changed fields.
 * @returns The notification.
 */
function makeNotification(overrides: Partial<Schemas["NotificationDto"]> = {}): Schemas["NotificationDto"] {
    return {
        id: "n-1",
        kind: "DocumentToSign",
        actor: makeUserRef({ fullName: "Коваленко Марія" }),
        entityId: "doc-1",
        subject: "Наказ № 12",
        preview: null,
        code: null,
        link: "/documents/doc-1",
        count: 1,
        createdAt: "2026-11-09T08:00:00Z",
        readAt: null,
        organizationId: "00000000-0000-7000-8000-0000000000f1",
        organizationCode: "ORG",
        ...overrides,
    };
}

/**
 * The query of the last list request.
 * @param calls The recorded calls.
 * @returns The query values.
 */
function lastQuery(calls: RecordedRequest[]): Record<string, string> {
    return Object.fromEntries(calls.at(-1)?.query ?? []);
}

describe("notificationTitle", () => {
    it.each([
        ["NewsReply", "Відповідь на Ваш коментар"],
        ["DocumentToSign", "Документ очікує Вашого підпису"],
        ["DocumentSigned", "Документ підписали всі підписанти"],
        ["DocumentRejected", "Документ відхилено"],
        ["TicketCreated", "Нове звернення"],
        ["TicketReply", "Нова відповідь у зверненні"],
        ["TicketAssigned", "Вам призначено звернення"],
    ] as const)("names a %s notification", (kind, title) => {
        expect(notificationTitle(makeNotification({ kind }), i18n)).toBe(title);
    });

    it("counts folded chat messages", () => {
        expect(notificationTitle(makeNotification({ kind: "ChatMessage", count: 1 }), i18n)).toBe("Нове повідомлення");
        expect(notificationTitle(makeNotification({ kind: "ChatMessage", count: 4 }), i18n)).toBe("Нові повідомлення: 4");
    });

    it("names the new state of a request and falls back for an unknown one", () => {
        expect(notificationTitle(makeNotification({ kind: "TicketStatusChanged", code: "Resolved" }), i18n)).toBe(
            "Стан звернення: Вирішено",
        );
        expect(notificationTitle(makeNotification({ kind: "TicketStatusChanged", code: "constructor" }), i18n)).toBe(
            "Стан звернення змінено",
        );
        expect(notificationTitle(makeNotification({ kind: "TicketStatusChanged", code: null }), i18n)).toBe("Стан звернення змінено");
    });
});

describe("NotificationsPage", () => {
    /**
     * Renders the page with a list and an unread count.
     * @param items The notifications.
     * @param unread The unread count.
     * @returns The rendered page and the list calls.
     */
    function renderList(items: Schemas["NotificationDto"][], unread = 1) {
        const calls = mockApi("get", "/api/v1/notifications", makePage(items));
        mockApi("get", "/api/v1/notifications/unread-count", { count: unread });
        const page = renderPage(<NotificationsPage />, {
            path: "/notifications",
            routes: [{ path: "/documents/:id", element: <p>document</p> }],
        });
        return { ...page, calls };
    }

    it("lists the notifications with their title, subject, author and unread mark", async () => {
        const { calls } = renderList([makeNotification({ preview: "Підпишіть до п'ятниці" })]);

        expect(await screen.findByText("Документ очікує Вашого підпису")).toBeInTheDocument();
        expect(screen.getByText("Наказ № 12")).toBeInTheDocument();
        expect(screen.getByText("Підпишіть до п'ятниці")).toBeInTheDocument();
        expect(screen.getByLabelText("Непрочитане")).toBeInTheDocument();
        expect(lastQuery(calls)).toEqual({ Page: "1", PageSize: "20", UnreadOnly: "false" });
    });

    it("opens the item it is about and marks it read", async () => {
        const read = mockApi("put", "/api/v1/notifications/{id}/read");
        const { user } = renderList([makeNotification()]);

        await user.click(await screen.findByRole("button", { name: /Документ очікує Вашого підпису/ }));

        expect(await screen.findByText("document")).toBeInTheDocument();
        expect(read[0]?.path).toBe("/api/v1/notifications/n-1/read");
    });

    it("names the organization of a notification from another one and opens it there", async () => {
        const assign = vi.spyOn(window.location, "assign").mockImplementation(() => undefined);
        const read = mockApi("put", "/o/FUND/api/v1/notifications/{id}/read" as never);
        const { user, queryClient } = renderList([makeNotification({ organizationId: "f", organizationCode: "FUND" })]);
        queryClient.setQueryData(authContextQuery.queryKey, {
            ...makeAuthContext(),
            organizations: [
                makeOrganization(),
                makeOrganization({ id: "f", code: "FUND", name: "Благодійний фонд", shortName: "БФ", access: "Member" }),
            ],
        });

        expect(await screen.findByText("БФ · Наказ № 12")).toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: /Документ очікує Вашого підпису/ }));

        await waitFor(() => expect(assign).toHaveBeenCalledWith("/o/FUND/documents/doc-1"));
        expect(read[0]?.path).toBe("/o/FUND/api/v1/notifications/n-1/read");
        assign.mockRestore();
    });

    it("does not mark a read notification again when it is opened", async () => {
        const read = mockApi("put", "/api/v1/notifications/{id}/read");
        const { user } = renderList([makeNotification({ readAt: "2026-11-09T09:00:00Z" })], 0);

        await user.click(await screen.findByRole("button", { name: /Документ очікує Вашого підпису/ }));

        expect(await screen.findByText("document")).toBeInTheDocument();
        expect(read).toHaveLength(0);
    });

    it("marks one notification read and deletes another with the row buttons", async () => {
        const read = mockApi("put", "/api/v1/notifications/{id}/read");
        const removed = mockApi("delete", "/api/v1/notifications/{id}");
        const { user } = renderList([
            makeNotification(),
            makeNotification({ id: "n-2", readAt: "2026-11-09T09:00:00Z", subject: "Наказ № 13" }),
        ]);

        const first = (await screen.findByText("Наказ № 12")).closest("li") as HTMLElement;
        const second = screen.getByText("Наказ № 13").closest("li") as HTMLElement;
        expect(within(second).queryByRole("button", { name: "Позначити прочитаним" })).not.toBeInTheDocument();
        await user.click(within(first).getByRole("button", { name: "Позначити прочитаним" }));
        await user.click(within(second).getByRole("button", { name: "Видалити сповіщення" }));

        await waitFor(() => expect(removed[0]?.path).toBe("/api/v1/notifications/n-2"));
        expect(read[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("marks every notification read, and the button is off with nothing unread", async () => {
        const all = mockApi("put", "/api/v1/notifications/read");
        const { user } = renderList([makeNotification()], 2);

        const button = await screen.findByRole("button", { name: "Прочитати всі" });
        await waitFor(() => expect(button).toBeEnabled());
        await user.click(button);

        await waitFor(() => expect(all).toHaveLength(1));
    });

    it("keeps the mark-all button off when nothing is unread", async () => {
        renderList([makeNotification({ readAt: "2026-11-09T09:00:00Z" })], 0);

        await screen.findByText("Наказ № 12");
        expect(screen.getByRole("button", { name: "Прочитати всі" })).toBeDisabled();
    });

    it("deletes the read notifications only after confirmation", async () => {
        const removed = mockApi("delete", "/api/v1/notifications/read");
        const { user } = renderList([makeNotification()]);

        await user.click(await screen.findByRole("button", { name: "Видалити прочитані" }));
        const dialog = (await screen.findByRole("heading", { name: "Видалити прочитані сповіщення?" })).closest("dialog") as HTMLElement;
        await user.click(within(dialog).getByRole("button", { name: "Видалити" }));

        await waitFor(() => expect(removed).toHaveLength(1));
    });

    it("reports a failed change", async () => {
        mockApi("put", "/api/v1/notifications/read", () => problem(500, "error", "Сервер недоступний."));
        const { user } = renderList([makeNotification()], 1);

        const button = await screen.findByRole("button", { name: "Прочитати всі" });
        await waitFor(() => expect(button).toBeEnabled());
        await user.click(button);

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });

    it("shows only the unread ones on request and says when there are none", async () => {
        const { calls, user } = renderList([], 3);

        expect(await screen.findByText("Сповіщень немає")).toBeInTheDocument();
        const filter = screen.getByRole("combobox", { name: "Показати" });
        await user.click(filter);
        const listbox = document.getElementById(filter.getAttribute("aria-controls") ?? "") as HTMLElement;
        await user.click(within(listbox).getByRole("option", { name: "Непрочитані (3)" }));

        await waitFor(() => expect(lastQuery(calls).UnreadOnly).toBe("true"));
        expect(await screen.findByText("Непрочитаних сповіщень немає")).toBeInTheDocument();
    });

    it("shows the server's message when the list fails", async () => {
        mockApi("get", "/api/v1/notifications", () => problem(500, "error", "Сервер недоступний."));
        mockApi("get", "/api/v1/notifications/unread-count", { count: 0 });
        renderPage(<NotificationsPage />, { path: "/notifications" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});

describe("NotificationSync", () => {
    it("reads the unread count of every organization again, refreshes the requests and shows a toast for a new notification", async () => {
        const { queryClient } = renderPage(<NotificationSync />, { path: "/" });
        queryClient.setQueryData(notificationKeys.unread, 6);
        queryClient.setQueryData(["tickets", "mine"], "stale");

        // The count of the event is that of its own organization: the badge counts all of them, so it is read again.
        emitLive("NotificationReceived", makeNotification({ kind: "TicketReply", subject: "Запит № 5", link: "/requests/t-5" }), 7);

        expect(queryClient.getQueryState(notificationKeys.unread)?.isInvalidated).toBe(true);
        expect(queryClient.getQueryState(["tickets", "mine"])?.isInvalidated).toBe(true);
        expect(await screen.findByText("Нова відповідь у зверненні: Запит № 5")).toBeInTheDocument();
    });

    it("refreshes the documents and the dashboard for a document notification", () => {
        const { queryClient } = renderPage(<NotificationSync />, { path: "/" });
        queryClient.setQueryData(["documents", "list"], "stale");
        queryClient.setQueryData(["dashboard"], "stale");

        emitLive("NotificationReceived", makeNotification(), 1);

        expect(queryClient.getQueryState(["documents", "list"])?.isInvalidated).toBe(true);
        expect(queryClient.getQueryState(["dashboard"])?.isInvalidated).toBe(true);
    });

    it("stays quiet for chat messages and for the page the user is already on", () => {
        renderPage(<NotificationSync />, { path: "/documents/:id", url: "/documents/doc-1" });

        emitLive("NotificationReceived", makeNotification({ kind: "ChatMessage", link: "/chats/c-1" }), 1);
        emitLive("NotificationReceived", makeNotification({ link: "/documents/doc-1#route" }), 2);

        expect(screen.queryByText(/Наказ № 12/)).not.toBeInTheDocument();
    });

    it("applies a changed unread count and reloads everything after a reconnect", () => {
        const { queryClient } = renderPage(<NotificationSync />, { path: "/" });
        queryClient.setQueryData(notificationKeys.list({ page: 1, pageSize: 20, unreadOnly: false }), "stale");

        emitLive("NotificationsChanged", 0);
        expect(queryClient.getQueryData(notificationKeys.unread)).toBe(0);

        queryClient.setQueryData(notificationKeys.unread, 5);
        reconnectLive();
        expect(queryClient.getQueryState(notificationKeys.unread)?.isInvalidated).toBe(true);
    });
});
