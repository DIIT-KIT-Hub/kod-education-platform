import { screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Permissions } from "@/shared/session/session";
import { makeUser, type Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { HomePage } from "./HomePage";

/**
 * The dashboard as the server returns it.
 * @param overrides Changed fields.
 * @returns The summary.
 */
function makeSummary(overrides: Partial<Schemas["DashboardSummary"]> = {}): Schemas["DashboardSummary"] {
    return {
        awaitingMyDecision: 2,
        myDrafts: 1,
        myInSigning: 3,
        signedThisMonth: 12,
        awaiting: [
            {
                organizationId: "00000000-0000-7000-8000-0000000000f1",
                organizationCode: "ORG",
                document: {
                    id: "doc-1",
                    registrationNumber: "2026/0007",
                    title: "Наказ про відпустки",
                    kind: "Order",
                    status: "InSigning",
                    authorName: "Коваленко Марія",
                    unitName: "Відділ кадрів",
                    createdAt: "2026-11-01T09:00:00Z",
                    signedCount: 0,
                    signersCount: 2,
                    awaitingMe: true,
                    authorPhotoId: null,
                },
            },
        ],
        latestNews: [
            {
                id: "news-1",
                slug: "novyi-grafik",
                title: "Новий графік роботи",
                summary: "З понеділка",
                category: "Announcement",
                coverImageUrl: null,
                isPinned: true,
                publishedAt: "2026-11-05T10:00:00Z",
                authorName: "Прес-служба",
                authorPhotoId: null,
                updatedAt: null,
                organization: null,
                commentCount: 0,
                reactionCount: 0,
                viewCount: 0,
            },
        ],
        ...overrides,
    };
}

afterEach(() => vi.useRealTimers());

describe("HomePage", () => {
    it("greets the user by first name and shows the counters, the documents to sign and the news", async () => {
        mockApi("get", "/api/v1/dashboard", makeSummary());
        renderPage(<HomePage />, { path: "/" });

        expect(screen.getByRole("heading", { level: 1, name: "Тарас" })).toBeInTheDocument();
        const stats = await screen.findByRole("region", { name: "Показники" });
        expect(within(stats).getByRole("link", { name: /2\s*Очікують мого підпису/ })).toHaveAttribute("href", "/documents");
        expect(within(stats).getByText("12")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Наказ про відпустки/ })).toHaveAttribute("href", "/documents/doc-1");
        expect(screen.getByText(/№ 2026\/0007 · Наказ · Коваленко Марія/)).toBeInTheDocument();
        expect(screen.getByRole("link", { name: /Новий графік роботи/ })).toHaveAttribute("href", "/news/novyi-grafik");
    });

    it.each([
        ["2026-11-09T00:30:00Z", "Доброї ночі,"],
        ["2026-11-09T06:00:00Z", "Доброго ранку,"],
        ["2026-11-09T12:00:00Z", "Доброго дня,"],
        ["2026-11-09T18:00:00Z", "Доброго вечора,"],
    ])("greets by the hour in Kyiv at %s", async (moment, greeting) => {
        vi.useFakeTimers({ toFake: ["Date"] });
        vi.setSystemTime(new Date(moment));
        mockApi("get", "/api/v1/dashboard", makeSummary());
        renderPage(<HomePage />, { path: "/" });

        expect(screen.getByText(greeting)).toBeInTheDocument();
    });

    it("says when nothing awaits a signature and there is no news", async () => {
        mockApi("get", "/api/v1/dashboard", makeSummary({ awaiting: [], latestNews: [], awaitingMyDecision: 0 }));
        renderPage(<HomePage />, { path: "/" });

        expect(await screen.findByText("Усе підписано")).toBeInTheDocument();
        expect(screen.getByText("Новин поки немає")).toBeInTheDocument();
    });

    it("shows only what the user may use", async () => {
        mockApi("get", "/api/v1/dashboard", makeSummary());
        renderPage(<HomePage />, { path: "/", user: makeUser({ permissions: [Permissions.newsView] }) });

        expect(await screen.findByRole("link", { name: /Новий графік роботи/ })).toBeInTheDocument();
        expect(screen.queryByRole("region", { name: "Показники" })).not.toBeInTheDocument();
        expect(screen.queryByText("Наказ про відпустки")).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Новий документ" })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Підписати файл" })).not.toBeInTheDocument();
    });

    it("offers a new document and file signing to those allowed", async () => {
        mockApi("get", "/api/v1/dashboard", makeSummary());
        renderPage(<HomePage />, { path: "/" });

        expect(screen.getByRole("link", { name: "Новий документ" })).toHaveAttribute("href", "/documents/new");
        expect(screen.getByRole("link", { name: "Підписати файл" })).toHaveAttribute("href", "/signing");
        await screen.findByRole("region", { name: "Показники" });
    });

    it("says when the summary cannot be loaded", async () => {
        mockApi("get", "/api/v1/dashboard", () => problem(500, "error", "Сервер недоступний."));
        renderPage(<HomePage />, { path: "/" });

        expect(await screen.findByText("Сервер недоступний.")).toBeInTheDocument();
    });
});
