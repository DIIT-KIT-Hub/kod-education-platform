import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makeUser, makeUserRef, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { TicketPage } from "./TicketPage";

/** The signed-in handler. */
const me = makeUser();

/** The author of the request. */
const author = makeUserRef({ id: "author-1", fullName: "Коваленко Марія" });

/**
 * A request as the server returns it.
 * @param overrides Changed fields.
 * @returns The request.
 */
function makeTicket(overrides: Partial<Schemas["TicketDetails"]> = {}): Schemas["TicketDetails"] {
    return {
        id: "ticket-1",
        number: 17,
        subject: "Не відкривається документ",
        category: "Bug",
        priority: "High",
        status: "New",
        author,
        authorPosition: "Бухгалтер",
        authorEmail: "maria@example.com",
        assignee: null,
        createdAt: "2026-11-02T08:00:00Z",
        lastActivityAt: "2026-11-02T09:00:00Z",
        messages: [
            {
                id: "m-1",
                kind: "Comment",
                author,
                fromAuthor: true,
                body: "Після натискання нічого не відбувається.",
                status: null,
                createdAt: "2026-11-02T08:00:00Z",
                attachments: [],
            },
            {
                id: "m-2",
                kind: "StatusChanged",
                author: makeUserRef({ id: me.id, fullName: me.fullName }),
                fromAuthor: false,
                body: "",
                status: "InProgress",
                createdAt: "2026-11-02T08:30:00Z",
                attachments: [],
            },
            {
                id: "m-3",
                kind: "Assigned",
                author: makeUserRef({ id: me.id, fullName: me.fullName }),
                fromAuthor: false,
                body: "",
                status: null,
                createdAt: "2026-11-02T08:31:00Z",
                attachments: [],
            },
        ],
        isMine: false,
        canManage: true,
        canReply: true,
        canClose: true,
        ...overrides,
    };
}

/**
 * Renders the card of a request.
 * @param ticket The request.
 * @returns The rendered page.
 */
function renderTicket(ticket: Schemas["TicketDetails"] = makeTicket()) {
    mockApi("get", "/api/v1/tickets/{id}", ticket);
    mockApi("get", "/api/v1/tickets/handlers", [
        makeUserRef({ id: me.id, fullName: me.fullName }),
        makeUserRef({ id: "h-2", fullName: "Бондар Олег" }),
    ]);
    return renderPage(<TicketPage />, { path: "/requests/:id", url: `/requests/${ticket.id}`, user: me });
}

/**
 * Chooses an option of the custom drop-down list.
 * @param page The rendered page.
 * @param name The list label.
 * @param option The option text.
 */
async function choose(page: ReturnType<typeof renderTicket>, name: string, option: string) {
    const combobox = screen.getByRole("combobox", { name });
    await page.user.click(combobox);
    const listbox = document.getElementById(combobox.getAttribute("aria-controls") ?? "");
    await page.user.click(within(listbox as HTMLElement).getByRole("option", { name: option }));
}

describe("TicketPage", () => {
    it("shows the thread, the details and the history notes of a request", async () => {
        renderTicket();

        expect(await screen.findByRole("heading", { name: "Не відкривається документ" })).toBeInTheDocument();
        expect(screen.getByText("Після натискання нічого не відбувається.")).toBeInTheDocument();
        expect(screen.getByText("Опис звернення")).toBeInTheDocument();
        expect(screen.getByText("Стан змінено на «В роботі»")).toBeInTheDocument();
        expect(screen.getByText("Виконавця знято")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "maria@example.com" })).toHaveAttribute("href", "mailto:maria@example.com");
        expect(screen.getByText("Не призначено", { selector: "dd *, dd" })).toBeInTheDocument();
    });

    it("leads a handler back to the administration list", async () => {
        renderTicket();

        expect(await screen.findByRole("link", { name: "Адміністрування" })).toHaveAttribute("href", "/admin");
    });

    it("leads the author back to their own requests and offers to close it", async () => {
        renderTicket(makeTicket({ isMine: true, canManage: false }));

        expect(await screen.findByRole("link", { name: "Звернення" })).toHaveAttribute("href", "/requests");
        expect(screen.queryByText("Опрацювання")).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Закрити звернення" })).toBeInTheDocument();
    });

    it("sends a trimmed reply with the CSRF header and clears the box", async () => {
        const calls = mockApi("post", "/api/v1/tickets/{id}/messages", { id: "m-4" });
        const page = renderTicket();

        const box = await screen.findByRole("textbox", { name: "Повідомлення" });
        expect(screen.getByRole("button", { name: "Надіслати" })).toBeDisabled();
        await page.user.type(box, "  Перевіряємо  ");
        await page.user.click(screen.getByRole("button", { name: "Надіслати" }));

        await waitFor(() => expect(calls).toHaveLength(1));
        expect((calls[0]?.body as FormData).get("Body")).toBe("Перевіряємо");
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        await waitFor(() => expect(box).toHaveValue(""));
    });

    it("sends a reply with Ctrl+Enter", async () => {
        const calls = mockApi("post", "/api/v1/tickets/{id}/messages", { id: "m-4" });
        const page = renderTicket();

        await page.user.type(await screen.findByRole("textbox", { name: "Повідомлення" }), "Готово{Control>}{Enter}{/Control}");

        await waitFor(() => expect(calls).toHaveLength(1));
    });

    it("keeps the reply when it could not be sent", async () => {
        mockApi("post", "/api/v1/tickets/{id}/messages", () => problem(400, "validation", "Звернення закрито."));
        const page = renderTicket();

        const box = await screen.findByRole("textbox", { name: "Повідомлення" });
        await page.user.type(box, "Ще питання");
        await page.user.click(screen.getByRole("button", { name: "Надіслати" }));

        expect(await screen.findByText("Звернення закрито.")).toBeInTheDocument();
        expect(box).toHaveValue("Ще питання");
    });

    it("explains that a closed request takes no replies", async () => {
        renderTicket(makeTicket({ status: "Closed", canReply: false }));

        expect(await screen.findByText(/Звернення закрито: відповісти на нього не можна/)).toBeInTheDocument();
        expect(screen.queryByRole("textbox", { name: "Повідомлення" })).not.toBeInTheDocument();
    });

    it("lets a handler change the state, the assignee, the category and the priority", async () => {
        const status = mockApi("put", "/api/v1/tickets/{id}/status");
        const assignee = mockApi("put", "/api/v1/tickets/{id}/assignee");
        const classification = mockApi("put", "/api/v1/tickets/{id}/classification");
        const page = renderTicket();
        await screen.findByText("Опрацювання");

        await choose(page, "Стан", "Вирішено");
        await waitFor(() => expect(status[0]?.body).toEqual({ status: "Resolved" }));

        await choose(page, "Виконавець", "Бондар Олег");
        await waitFor(() => expect(assignee[0]?.body).toEqual({ assigneeId: "h-2" }));

        await choose(page, "Категорія", "Доступ і права");
        await waitFor(() => expect(classification[0]?.body).toEqual({ category: "Access", priority: "High" }));

        await choose(page, "Пріоритет", "Критичний");
        await waitFor(() => expect(classification[1]?.body).toEqual({ category: "Bug", priority: "Critical" }));
    });

    it("lets a handler take a request and hides the button once it is theirs", async () => {
        const assignee = mockApi("put", "/api/v1/tickets/{id}/assignee");
        const page = renderTicket();

        await page.user.click(await screen.findByRole("button", { name: "Взяти собі" }));
        await waitFor(() => expect(assignee[0]?.body).toEqual({ assigneeId: me.id }));

        page.unmount();
        renderTicket(makeTicket({ assignee: makeUserRef({ id: me.id, fullName: me.fullName }) }));
        await screen.findByText("Опрацювання");
        expect(screen.queryByRole("button", { name: "Взяти собі" })).not.toBeInTheDocument();
    });

    it("reports a refused change", async () => {
        mockApi("put", "/api/v1/tickets/{id}/status", () => problem(400, "validation", "Неможливий перехід стану."));
        const page = renderTicket();
        await screen.findByText("Опрацювання");

        await choose(page, "Стан", "Закрито");

        expect(await screen.findByText("Неможливий перехід стану.")).toBeInTheDocument();
    });

    it("closes the author's own request after confirmation", async () => {
        const status = mockApi("put", "/api/v1/tickets/{id}/status");
        const page = renderTicket(makeTicket({ isMine: true, canManage: false }));

        await page.user.click(await screen.findByRole("button", { name: "Закрити звернення" }));
        const heading = await screen.findByRole("heading", { name: "Закрити звернення?" });
        // The dialog also has a close button with the same name; the action comes last.
        await page.user.click(
            within(heading.closest("dialog") as HTMLElement)
                .getAllByRole("button", { name: "Закрити" })
                .at(-1) as HTMLElement,
        );

        await waitFor(() => expect(status[0]?.body).toEqual({ status: "Closed" }));
        expect(await screen.findByText("Звернення закрито")).toBeInTheDocument();
    });

    it("does not close the request when the confirmation is cancelled", async () => {
        const status = mockApi("put", "/api/v1/tickets/{id}/status");
        const page = renderTicket(makeTicket({ isMine: true, canManage: false }));

        await page.user.click(await screen.findByRole("button", { name: "Закрити звернення" }));
        const heading = await screen.findByRole("heading", { name: "Закрити звернення?" });
        await page.user.click(within(heading.closest("dialog") as HTMLElement).getByRole("button", { name: "Скасувати" }));

        expect(status).toHaveLength(0);
    });

    it("shows the server's message when the request cannot be loaded", async () => {
        mockApi("get", "/api/v1/tickets/{id}", () => problem(404, "not_found", "Звернення не знайдено."));
        renderPage(<TicketPage />, { path: "/requests/:id", url: "/requests/missing" });

        expect(await screen.findByText("Звернення не знайдено.")).toBeInTheDocument();
    });
});
