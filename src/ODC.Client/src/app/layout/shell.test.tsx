import { i18n } from "@lingui/core";
import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { lastPage } from "@/shared/lib/lastPage";
import { Permissions } from "@/shared/session/session";
import { makeAuthContext, makeUser } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { AppShell } from "./AppShell";
import { isCurrent, navigationFor } from "./navigation";
import { UserMenu } from "./UserMenu";

vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"));

/**
 * The addresses of the menu items a user sees.
 * @param permissions The user's permissions.
 * @returns The addresses.
 */
function menuOf(...permissions: string[]): string[] {
    return navigationFor(makeUser({ permissions })).items.map((item) => item.to);
}

describe("navigationFor", () => {
    it("gives everyone the home page, requests, profile and the manual", () => {
        expect(menuOf()).toEqual(["/", "/requests", "/profile", "/help"]);
    });

    it("adds the sections the user's permissions open", () => {
        expect(menuOf(Permissions.documentsView, Permissions.chatsUse, Permissions.newsView, Permissions.structureView)).toEqual([
            "/",
            "/documents",
            "/chats",
            "/news",
            "/requests",
            "/structure",
            "/profile",
            "/help",
        ]);
    });

    it.each([Permissions.usersView, Permissions.rolesManage, Permissions.auditView, Permissions.ticketsManage, Permissions.newsManage])(
        "opens the administration with %s",
        (permission) => {
            expect(menuOf(permission)).toContain("/admin");
        },
    );

    it("drops a section with no items for the user", () => {
        const groups = navigationFor(makeUser({ permissions: [] })).groups.map((group) => group.id);

        expect(groups).not.toContain("work");
        expect(groups).toContain("personal");
    });

    it("keeps only some items for the phone tab bar", () => {
        const tabs = navigationFor(makeUser())
            .items.filter((item) => item.tabBar)
            .map((item) => item.to);

        expect(tabs).toEqual(["/", "/documents", "/chats", "/news", "/profile"]);
    });

    it.each([
        ["/", "/", true],
        ["/", "/documents", false],
        ["/documents", "/documents", true],
        ["/documents", "/documents/42", true],
        ["/documents", "/documents-old", false],
        ["/news", "/news/manage", true],
    ])("treats %s as current on %s: %s", (to, pathname, expected) => {
        expect(isCurrent(to, pathname)).toBe(expected);
    });

    it("names every item in the current language", () => {
        expect(navigationFor(makeUser()).items.map((item) => i18n._(item.text))).toContain("Документи");
    });
});

describe("AppShell", () => {
    /**
     * Renders the shell at an address for a user.
     * @param url The address.
     * @param permissions The user's permissions.
     * @returns The rendered page.
     */
    function renderShell(url: string, permissions?: string[]) {
        mockApi("get", "/api/v1/notifications/unread-count", { count: 2 });
        mockApi("get", "/api/v1/chats", [
            {
                id: "c",
                kind: "Direct",
                title: null,
                members: [],
                memberCount: 2,
                lastMessage: null,
                lastActivityAt: "2026-11-08T08:00:00Z",
                lastSequence: 3,
                lastReadSequence: 0,
                unreadCount: 3,
            },
        ]);
        return renderPage(<AppShell />, { path: "*", url, user: permissions ? makeUser({ permissions }) : makeUser() });
    }

    it("shows only the menu items the user may open", async () => {
        renderShell("/", [Permissions.newsView]);

        const menu = await screen.findByRole("complementary", { name: "Головне меню" });
        expect(within(menu).getByRole("link", { name: /Новини/ })).toBeInTheDocument();
        expect(within(menu).queryByRole("link", { name: /Документи/ })).not.toBeInTheDocument();
        expect(within(menu).queryByRole("link", { name: /Адміністрування/ })).not.toBeInTheDocument();
    });

    it("remembers the page the user is on, for the manual's way back", async () => {
        renderShell("/documents?x=1#route");

        await screen.findByRole("complementary", { name: "Головне меню" });
        await waitFor(() => expect(lastPage().path).toBe("/documents?x=1#route"));
    });

    it("offers a link that skips to the content", async () => {
        renderShell("/");

        expect(await screen.findByRole("link", { name: "Перейти до змісту" })).toBeInTheDocument();
    });
});

describe("UserMenu", () => {
    it("links to the profile and signs out after confirmation", async () => {
        const logout = mockApi("post", "/api/v1/auth/logout", 204);
        mockApi("get", "/api/v1/auth/context", makeAuthContext(null));
        const { user } = renderPage(<UserMenu />, { path: "/" });

        expect(screen.getByRole("link", { name: /Мій профіль/ })).toHaveAttribute("href", "/profile");
        await user.click(screen.getByRole("button", { name: "Вийти" }));
        const dialog = within((await screen.findByRole("heading", { name: "Вийти з кабінету?" })).closest("dialog") as HTMLElement);
        await user.click(dialog.getAllByRole("button", { name: "Вийти" }).at(-1) as HTMLElement);

        expect(await navigatedTo()).toBe("/account/login");
        expect(logout[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("stays signed in when the question is cancelled", async () => {
        const logout = mockApi("post", "/api/v1/auth/logout", 204);
        const { user } = renderPage(<UserMenu />, { path: "/" });

        await user.click(screen.getByRole("button", { name: "Вийти" }));
        const dialog = within((await screen.findByRole("heading", { name: "Вийти з кабінету?" })).closest("dialog") as HTMLElement);
        await user.click(dialog.getByRole("button", { name: "Скасувати" }));

        expect(logout).toHaveLength(0);
    });

    it("reports a failed sign-out", async () => {
        mockApi("post", "/api/v1/auth/logout", () => problem(500, "error", "Сервер недоступний."));
        const { user } = renderPage(<UserMenu />, { path: "/" });

        await user.click(screen.getByRole("button", { name: "Вийти" }));
        const dialog = within((await screen.findByRole("heading", { name: "Вийти з кабінету?" })).closest("dialog") as HTMLElement);
        await user.click(dialog.getAllByRole("button", { name: "Вийти" }).at(-1) as HTMLElement);

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});
