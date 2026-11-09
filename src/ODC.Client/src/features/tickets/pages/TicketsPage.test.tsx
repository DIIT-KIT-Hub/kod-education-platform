import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { makePage, makeUserRef, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem, type RecordedRequest } from "@/test/server";
import { TicketsAdminPage } from "./TicketsAdminPage";
import { TicketsPage } from "./TicketsPage";

/**
 * A row of a request list.
 * @param overrides Changed fields.
 * @returns The row.
 */
function makeRow(overrides: Partial<Schemas["TicketListItem"]> = {}): Schemas["TicketListItem"] {
    return {
        id: "ticket-1",
        number: 17,
        subject: "Не відкривається документ",
        category: "Bug",
        priority: "Critical",
        status: "New",
        author: makeUserRef({ id: "author-1", fullName: "Коваленко Марія" }),
        assignee: null,
        createdAt: "2026-11-02T08:00:00Z",
        lastActivityAt: "2026-11-02T09:00:00Z",
        commentCount: 1,
        awaitingAnswer: true,
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

/**
 * Chooses an option of the custom drop-down list.
 * @param page The rendered page.
 * @param name The list label.
 * @param option The option text.
 */
async function choose(page: ReturnType<typeof renderPage>, name: string, option: string) {
    const combobox = screen.getByRole("combobox", { name });
    await page.user.click(combobox);
    const listbox = document.getElementById(combobox.getAttribute("aria-controls") ?? "");
    await page.user.click(within(listbox as HTMLElement).getByRole("option", { name: option }));
}

describe("TicketsPage", () => {
    it("lists the user's own requests with links to them", async () => {
        const calls = mockApi("get", "/api/v1/tickets/mine", makePage([makeRow()]));
        renderPage(<TicketsPage />, { path: "/requests" });

        expect(await screen.findByRole("link", { name: /№ 17\s*Не відкривається документ/ })).toHaveAttribute("href", "/requests/ticket-1");
        expect(screen.getByRole("link", { name: "Нове звернення" })).toHaveAttribute("href", "/requests/new");
        expect(lastQuery(calls)).toMatchObject({ Page: "1", PageSize: "20", OpenOnly: "false", AssignedToMe: "false" });
    });

    it("invites to create the first request when there are none", async () => {
        mockApi("get", "/api/v1/tickets/mine", makePage([]));
        renderPage(<TicketsPage />, { path: "/requests" });

        expect(await screen.findByText("Звернень ще немає")).toBeInTheDocument();
    });

    it("filters by state and searches", async () => {
        const calls = mockApi("get", "/api/v1/tickets/mine", makePage([makeRow()]));
        const page = renderPage(<TicketsPage />, { path: "/requests" });
        await screen.findByRole("link", { name: /Не відкривається документ/ });

        await choose(page, "Стан", "Вирішено");
        await waitFor(() => expect(lastQuery(calls).Status).toBe("Resolved"));

        await page.user.type(screen.getByRole("searchbox"), "документ{Enter}");
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ Search: "документ", Status: "Resolved", Page: "1" }));
    });

    it("ignores unknown states kept in the view", async () => {
        const calls = mockApi("get", "/api/v1/tickets/mine", makePage([]));
        renderPage(<TicketsPage />, { path: "/requests", state: { view: "status=constructor&category=toString&page=-3" } });

        await screen.findByText("Звернень ще немає");
        expect(lastQuery(calls).Status).toBeUndefined();
        expect(lastQuery(calls).Category).toBeUndefined();
        expect(lastQuery(calls).Page).toBe("1");
    });

    it("shows the server's message when the list fails", async () => {
        mockApi("get", "/api/v1/tickets/mine", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<TicketsPage />, { path: "/requests" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});

describe("TicketsAdminPage", () => {
    it("shows every request with its author, priority, assignee and an awaiting-answer mark", async () => {
        mockApi(
            "get",
            "/api/v1/tickets",
            makePage([
                makeRow(),
                makeRow({
                    id: "ticket-2",
                    number: 18,
                    status: "Resolved",
                    awaitingAnswer: true,
                    assignee: makeUserRef({ fullName: "Бондар Олег" }),
                }),
            ]),
        );
        renderPage(<TicketsAdminPage />, { path: "/admin/requests" });

        const first = (await screen.findByRole("link", { name: /№ 17/ })).closest("tr") as HTMLElement;
        expect(within(first).getByText("Очікує відповіді")).toBeInTheDocument();
        expect(within(first).getByText("Коваленко Марія")).toBeInTheDocument();
        expect(within(first).getByText("Критичний")).toBeInTheDocument();
        const second = screen.getByRole("link", { name: /№ 18/ }).closest("tr") as HTMLElement;
        expect(within(second).queryByText("Очікує відповіді")).not.toBeInTheDocument();
        expect(within(second).getByText("Бондар Олег")).toBeInTheDocument();
    });

    it("narrows the list to open requests, those assigned to me and a category", async () => {
        const calls = mockApi("get", "/api/v1/tickets", makePage([makeRow()]));
        const page = renderPage(<TicketsAdminPage />, { path: "/admin/requests" });
        await screen.findByRole("link", { name: /№ 17/ });

        await choose(page, "Показати", "Призначені мені");
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ AssignedToMe: "true", OpenOnly: "false" }));

        await choose(page, "Показати", "Відкриті");
        await waitFor(() => expect(lastQuery(calls)).toMatchObject({ AssignedToMe: "false", OpenOnly: "true" }));

        await choose(page, "Категорія", "Пропозиція");
        await waitFor(() => expect(lastQuery(calls).Category).toBe("Suggestion"));
    });

    it("says when nothing matches", async () => {
        mockApi("get", "/api/v1/tickets", makePage([]));
        renderPage(<TicketsAdminPage />, { path: "/admin/requests" });

        expect(await screen.findByText("Звернень не знайдено")).toBeInTheDocument();
    });
});
