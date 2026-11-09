import { screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { renderPage } from "@/test/render";
import { userWith } from "../testing";
import { AdminPage } from "./AdminPage";

/** Every section of the hub with its address, in the order of the page. */
const allSections = [
    ["Користувачі", "/admin/users"],
    ["Імпорт і експорт", "/admin/users/import"],
    ["Ролі та дозволи", "/admin/roles"],
    ["Структура", "/admin/units"],
    ["Типи підрозділів", "/admin/unit-types"],
    ["Новини", "/news/manage"],
    ["Звернення", "/admin/requests"],
    ["Журнал аудиту", "/admin/audit"],
] as const;

/**
 * The titles of the section links shown on the page.
 * @returns The titles in order.
 */
function shownSections(): string[] {
    return screen
        .queryAllByRole("link")
        .filter((link) => link.closest("nav") === null)
        .map((link) => link.querySelector("span:nth-child(2)")?.textContent ?? "");
}

describe("AdminPage", () => {
    it("shows every section with its address and description to a user with every permission", async () => {
        renderPage(<AdminPage />, { path: "/admin" });

        expect(await screen.findByRole("heading", { name: "Адміністрування" })).toBeInTheDocument();
        expect(shownSections()).toEqual(allSections.map(([title]) => title));
        for (const [title, address] of allSections) {
            const link = screen.getAllByRole("link").find((item) => within(item).queryByText(title) !== null);
            expect(link).toHaveAttribute("href", address);
        }

        expect(screen.getByText("Масове додавання користувачів з XLSX/CSV та вивантаження.")).toBeInTheDocument();
        expect(screen.getByText("Входи, зміни прав, підписання документів.")).toBeInTheDocument();
    });

    it.each([
        [["users.view"], ["Користувачі"]],
        [["users.import"], ["Імпорт і експорт"]],
        [["users.export"], ["Імпорт і експорт"]],
        [["roles.manage"], ["Ролі та дозволи"]],
        [["structure.manage"], ["Структура", "Типи підрозділів"]],
        [["news.manage"], ["Новини"]],
        [["tickets.manage"], ["Звернення"]],
        [["audit.view"], ["Журнал аудиту"]],
        [["users.manage", "users.security"], []],
        [["administration.all"], []],
        [
            ["users.view", "audit.view"],
            ["Користувачі", "Журнал аудиту"],
        ],
    ])("with %j shows only %j", async (permissions, expected) => {
        renderPage(<AdminPage />, { path: "/admin", user: userWith(...permissions) });

        await screen.findByRole("heading", { name: "Адміністрування" });
        expect(shownSections()).toEqual(expected);
    });

    it("does not repeat the import section for a user who may both import and export", async () => {
        renderPage(<AdminPage />, { path: "/admin", user: userWith("users.import", "users.export") });

        await screen.findByRole("heading", { name: "Адміністрування" });
        expect(shownSections()).toEqual(["Імпорт і експорт"]);
    });
});
