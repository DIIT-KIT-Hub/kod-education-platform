import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { chatId, makeDetails, makeMember, makePerson, makeSummary, maria, me, meRef, olena, petro } from "../testing";
import { ForwardDialog } from "./ForwardDialog";
import { MembersDialog } from "./MembersDialog";
import { NewChatDialog } from "./NewChatDialog";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * Renders a dialog on a page of its own, so navigations land elsewhere.
 * @param element The dialog.
 * @returns The rendered page.
 */
function renderDialog(element: React.ReactNode) {
    return renderPage(element, { path: "/dialog", user: me });
}

/**
 * Looks people up in the picker of the open dialog.
 * @param page The rendered page.
 * @param term The search text.
 */
async function search(page: ReturnType<typeof renderDialog>, term: string) {
    await page.user.type(screen.getByRole("searchbox"), `${term}{Enter}`);
}

/**
 * The open confirmation dialog with the given title.
 * @param title The title.
 * @returns Queries inside the dialog.
 */
async function confirmation(title: string) {
    return within((await screen.findByRole("heading", { name: title })).closest("dialog") as HTMLElement);
}

describe("NewChatDialog", () => {
    it("asks for two characters before searching", () => {
        renderDialog(<NewChatDialog open onClose={vi.fn()} />);

        expect(screen.getByText("Введіть щонайменше два символи.")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Почати чат" })).toBeDisabled();
    });

    it("opens a one-to-one chat with the chosen person", async () => {
        const people = mockApi("get", "/api/v1/chats/people", [makePerson(maria, { position: "Бухгалтер" })]);
        const direct = mockApi("post", "/api/v1/chats/direct", { id: "chat-9" });
        const onClose = vi.fn();
        const page = renderDialog(<NewChatDialog open onClose={onClose} />);

        await search(page, "Мар");
        await page.user.click(await screen.findByRole("button", { name: /Бухгалтер/ }));
        await page.user.click(screen.getByRole("button", { name: "Почати чат" }));

        expect(await navigatedTo()).toBe("/chats/chat-9");
        expect(Object.fromEntries(people[0]?.query ?? [])).toEqual({ term: "Мар" });
        expect(direct[0]?.body).toEqual({ userId: maria.id });
        expect(onClose).toHaveBeenCalled();
    });

    it("creates a group with a trimmed title when several people are chosen", async () => {
        mockApi("get", "/api/v1/chats/people", [makePerson(maria), makePerson(petro)]);
        const created = mockApi("post", "/api/v1/chats", { id: "chat-10" });
        const page = renderDialog(<NewChatDialog open onClose={vi.fn()} />);

        await search(page, "ко");
        await page.user.click(await screen.findByRole("button", { name: new RegExp(maria.fullName) }));
        await page.user.click(screen.getByRole("button", { name: new RegExp(`^${petro.fullName}`) }));
        expect(screen.getByRole("button", { name: new RegExp(`^${petro.fullName}`) })).toHaveAttribute("aria-pressed", "true");
        await page.user.type(screen.getByRole("textbox", { name: /Назва чату/ }), "  Проєкт  ");
        await page.user.click(screen.getByRole("button", { name: "Створити груповий чат" }));

        expect(await navigatedTo()).toBe("/chats/chat-10");
        expect(created[0]?.body).toEqual({ title: "Проєкт", memberIds: [maria.id, petro.id] });
    });

    it("removes a chosen person with the chip button", async () => {
        mockApi("get", "/api/v1/chats/people", [makePerson(maria)]);
        const page = renderDialog(<NewChatDialog open onClose={vi.fn()} />);

        await search(page, "Мар");
        await page.user.click(await screen.findByRole("button", { name: new RegExp(maria.fullName) }));
        await page.user.click(screen.getByRole("button", { name: `Прибрати ${maria.fullName}` }));

        expect(screen.getByRole("button", { name: "Почати чат" })).toBeDisabled();
    });

    it("says when nobody is found", async () => {
        mockApi("get", "/api/v1/chats/people", []);
        const page = renderDialog(<NewChatDialog open onClose={vi.fn()} />);

        await search(page, "zzz");

        expect(await screen.findByText("Нікого не знайдено.")).toBeInTheDocument();
    });

    it("reports a refused chat and stays open", async () => {
        mockApi("get", "/api/v1/chats/people", [makePerson(maria)]);
        mockApi("post", "/api/v1/chats/direct", () => problem(400, "validation", "Обліковий запис заблоковано."));
        const onClose = vi.fn();
        const page = renderDialog(<NewChatDialog open onClose={onClose} />);

        await search(page, "Мар");
        await page.user.click(await screen.findByRole("button", { name: new RegExp(maria.fullName) }));
        await page.user.click(screen.getByRole("button", { name: "Почати чат" }));

        expect(await screen.findByText("Обліковий запис заблоковано.")).toBeInTheDocument();
        expect(onClose).not.toHaveBeenCalled();
    });
});

describe("ForwardDialog", () => {
    it("forwards the messages into a chosen chat and opens it", async () => {
        mockApi("get", "/api/v1/chats", [
            makeSummary({ id: "chat-2", kind: "Group", title: "Бухгалтерія", members: [petro], memberCount: 4 }),
        ]);
        const forwarded = mockApi("post", "/api/v1/chats/{id}/forward", []);
        const page = renderDialog(<ForwardDialog open onClose={vi.fn()} chatId={chatId} messageIds={["m-1", "m-2"]} />);

        await page.user.click(await screen.findByRole("button", { name: /Бухгалтерія/ }));

        await waitFor(() => expect(forwarded[0]?.path).toBe("/api/v1/chats/chat-2/forward"));
        expect(forwarded[0]?.body).toEqual({ sourceChatId: chatId, messageIds: ["m-1", "m-2"] });
        expect(await screen.findByText("Повідомлення переслано (2).")).toBeInTheDocument();
    });

    it("opens a one-to-one chat with a person who has none yet", async () => {
        mockApi("get", "/api/v1/chats", [makeSummary({ members: [maria] })]);
        mockApi("get", "/api/v1/chats/people", [makePerson(maria), makePerson(olena)]);
        const direct = mockApi("post", "/api/v1/chats/direct", { id: "chat-olena" });
        const forwarded = mockApi("post", "/api/v1/chats/{id}/forward", []);
        const page = renderDialog(<ForwardDialog open onClose={vi.fn()} chatId={chatId} messageIds={["m-1"]} />);

        await search(page, "ен");
        await page.user.click(await screen.findByRole("button", { name: new RegExp(olena.fullName) }));

        await waitFor(() => expect(forwarded[0]?.path).toBe("/api/v1/chats/chat-olena/forward"));
        expect(direct[0]?.body).toEqual({ userId: olena.id });
        expect(await screen.findByText("Повідомлення переслано.")).toBeInTheDocument();
    });

    it("reports a failed forward", async () => {
        mockApi("get", "/api/v1/chats", [
            makeSummary({ id: "chat-2", kind: "Group", title: "Бухгалтерія", members: [petro], memberCount: 4 }),
        ]);
        mockApi("post", "/api/v1/chats/{id}/forward", () => problem(403, "forbidden", "Ви не учасник цього чату."));
        const page = renderDialog(<ForwardDialog open onClose={vi.fn()} chatId={chatId} messageIds={["m-1"]} />);

        await page.user.click(await screen.findByRole("button", { name: /Бухгалтерія/ }));

        expect(await screen.findByText("Ви не учасник цього чату.")).toBeInTheDocument();
    });
});

describe("MembersDialog", () => {
    /** A group the user owns. */
    const group = makeDetails({
        kind: "Group",
        title: "Проєкт",
        myRole: "Owner",
        members: [
            makeMember(meRef, { role: "Owner" }),
            makeMember(maria, { position: "Бухгалтер" }),
            makeMember(petro, { isActive: false }),
        ],
    });

    it("lists the members with the owner, the user and blocked accounts", () => {
        renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        expect(screen.getByRole("heading", { name: "Учасники чату" })).toBeInTheDocument();
        expect(screen.getByText("Власник")).toBeInTheDocument();
        expect(screen.getByText("Ви")).toBeInTheDocument();
        expect(screen.getByText("Обліковий запис заблоковано")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: `Вилучити ${maria.fullName}` })).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: `Вилучити ${me.fullName}` })).not.toBeInTheDocument();
    });

    it("lets the owner rename the group, remove someone and add people", async () => {
        const renamed = mockApi("put", "/api/v1/chats/{id}/title");
        const removed = mockApi("delete", "/api/v1/chats/{id}/members/{userId}");
        const added = mockApi("post", "/api/v1/chats/{id}/members");
        mockApi("get", "/api/v1/chats/people", [makePerson(olena), makePerson(maria)]);
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        const title = screen.getByRole("textbox", { name: "Назва чату" });
        await page.user.clear(title);
        await page.user.type(title, "Проєкт 2026");
        await page.user.click(screen.getByRole("button", { name: "Зберегти" }));
        await waitFor(() => expect(renamed[0]?.body).toEqual({ title: "Проєкт 2026" }));

        await page.user.click(screen.getByRole("button", { name: `Вилучити ${maria.fullName}` }));
        await waitFor(() => expect(removed[0]?.path).toBe(`/api/v1/chats/${chatId}/members/${maria.id}`));

        await search(page, "Оле");
        expect(screen.queryByRole("button", { name: new RegExp(`^${maria.fullName}`) })).not.toBeInTheDocument();
        await page.user.click(await screen.findByRole("button", { name: new RegExp(olena.fullName) }));
        await page.user.click(screen.getByRole("button", { name: "Додати" }));
        await waitFor(() => expect(added[0]?.body).toEqual({ userIds: [olena.id] }));
    });

    it("hides removing and deleting for everyone from a member who is not the owner", () => {
        renderDialog(<MembersDialog open onClose={vi.fn()} chat={{ ...group, myRole: "Member" }} me={me.id} />);

        expect(screen.queryByRole("button", { name: `Вилучити ${maria.fullName}` })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Видалити чат для всіх" })).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Очистити історію" })).toBeInTheDocument();
    });

    it("leaves the group after confirmation and returns to the chat list", async () => {
        const left = mockApi("delete", "/api/v1/chats/{id}/members/{userId}");
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        await page.user.click(screen.getByRole("button", { name: "Вийти з чату" }));
        await page.user.click((await confirmation("Вийти з чату?")).getByRole("button", { name: "Вийти" }));

        expect(await navigatedTo()).toBe("/chats");
        expect(left[0]?.path).toBe(`/api/v1/chats/${chatId}/members/${me.id}`);
    });

    it("clears the history only for the user", async () => {
        const cleared = mockApi("post", "/api/v1/chats/{id}/clear");
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        await page.user.click(screen.getByRole("button", { name: "Очистити історію" }));
        await page.user.click((await confirmation("Очистити історію?")).getByRole("button", { name: "Очистити" }));

        await waitFor(() => expect(cleared).toHaveLength(1));
    });

    it("lets the owner delete the group for everyone", async () => {
        const deleted = mockApi("delete", "/api/v1/chats/{id}");
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        await page.user.click(screen.getByRole("button", { name: "Видалити чат для всіх" }));
        await page.user.click(
            (await confirmation("Видалити чат для всіх?")).getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement,
        );

        await waitFor(() => expect(deleted).toHaveLength(1));
    });

    it("only clears a one-to-one chat for the user unless the option is ticked", async () => {
        const deleted = mockApi("delete", "/api/v1/chats/{id}");
        const cleared = mockApi("post", "/api/v1/chats/{id}/clear");
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={makeDetails()} me={me.id} />);

        expect(screen.getByRole("heading", { name: "Про чат" })).toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Видалити чат" }));
        const dialog = await confirmation("Видалити чат?");
        expect(dialog.getByRole("checkbox", { name: /Також видалити для/ })).not.toBeChecked();
        await page.user.click(dialog.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        await waitFor(() => expect(cleared).toHaveLength(1));
        expect(deleted).toHaveLength(0);
    });

    it("deletes a one-to-one chat for both people when the option is ticked", async () => {
        const deleted = mockApi("delete", "/api/v1/chats/{id}");
        const cleared = mockApi("post", "/api/v1/chats/{id}/clear");
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={makeDetails()} me={me.id} />);

        await page.user.click(screen.getByRole("button", { name: "Видалити чат" }));
        const dialog = await confirmation("Видалити чат?");
        await page.user.click(dialog.getByRole("checkbox", { name: /Також видалити для/ }));
        await page.user.click(dialog.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);

        await waitFor(() => expect(deleted).toHaveLength(1));
        expect(cleared).toHaveLength(0);
    });

    it("reports a failed change", async () => {
        mockApi("delete", "/api/v1/chats/{id}/members/{userId}", () => problem(403, "forbidden", "Вилучати може лише власник."));
        const page = renderDialog(<MembersDialog open onClose={vi.fn()} chat={group} me={me.id} />);

        await page.user.click(screen.getByRole("button", { name: `Вилучити ${maria.fullName}` }));

        expect(await screen.findByText("Вилучати може лише власник.")).toBeInTheDocument();
    });
});
