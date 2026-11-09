import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Permissions } from "@/shared/session/session";
import type { Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { captureDownloads } from "@/test/downloads";
import { file, mockApi, problem } from "@/test/server";
import { clearToastsAfterEach, findToast, makeRoleOptions, userWith } from "../testing";
import { UsersImportPage } from "./UsersImportPage";

clearToastsAfterEach();

/**
 * An import report as the server returns it.
 * @param overrides Changed fields.
 * @returns The report.
 */
function makeReport(overrides: Partial<Schemas["UserImportReport"]> = {}): Schemas["UserImportReport"] {
    return { totalRows: 12, usersToCreate: 3, usersToUpdate: 5, applied: false, errors: [], invitationsSent: 0, ...overrides };
}

/**
 * Renders the import page.
 * @param user The signed-in user.
 * @returns The rendered page.
 */
function renderImport(user = userWith(Permissions.usersImport, Permissions.usersExport)) {
    mockApi("get", "/api/v1/user-imports/roles", makeRoleOptions());
    return renderPage(<UsersImportPage />, { path: "/admin/users/import", user });
}

/**
 * Picks a file in the drop zone.
 * @param file The file.
 */
function pick(file: File) {
    fireEvent.change(document.querySelector('input[type="file"]') as HTMLInputElement, { target: { files: [file] } });
}

/**
 * A card of the page by its title.
 * @param title The title.
 * @returns The card.
 */
function card(title: string): HTMLElement {
    return screen.getByRole("heading", { name: title }).closest("section, article, div[class]") as HTMLElement;
}

/** A small import file. */
const sheet = new File(["email;lastName"], "users.csv", { type: "text/csv" });

describe("UsersImportPage", () => {
    it("checks the file first and imports only a file without errors", async () => {
        const calls = mockApi(
            "post",
            "/api/v1/user-imports",
            ({ query }) =>
                new Response(JSON.stringify(makeReport({ applied: query.get("dryRun") === "false", invitationsSent: 3 })), {
                    headers: { "Content-Type": "application/json" },
                }),
        );
        const page = renderImport();

        expect(screen.getByRole("button", { name: "Перевірити" })).toBeDisabled();
        pick(sheet);
        expect(await screen.findByText("users.csv")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Імпортувати" })).toBeDisabled();

        await page.user.click(screen.getByRole("button", { name: "Перевірити" }));
        await findToast("Перевірку файлу розпочато.");
        await findToast("Перевірку завершено: помилок не знайдено.");
        expect(await screen.findByRole("heading", { name: "2. Результат перевірки" })).toBeInTheDocument();
        expect(screen.getByText("Помилок не знайдено. Натисніть «Імпортувати», щоб зберегти зміни.")).toBeInTheDocument();
        expect(calls[0]?.query.get("dryRun")).toBe("true");
        expect((calls[0]?.body as FormData).get("file")).toBeInstanceOf(File);

        await page.user.click(screen.getByRole("button", { name: "Імпортувати" }));
        await findToast("Імпорт користувачів розпочато. Це може тривати кілька хвилин.");
        await findToast("Імпорт завершено. Створено: 3, оновлено: 5.");
        expect(await screen.findByRole("heading", { name: "2. Імпорт виконано" })).toBeInTheDocument();
        expect(calls[1]?.query.get("dryRun")).toBe("false");
        expect(
            screen.getByText(/Запрошення надіслано: 3\. Користувачі активують облікові записи самі — паролі ніхто не передає\./),
        ).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Імпортувати" })).toBeDisabled();
    });

    it("lists the errors of the file and does not let it be imported", async () => {
        mockApi(
            "post",
            "/api/v1/user-imports",
            makeReport({
                errors: [
                    { row: 4, email: "bad@", message: "Некоректна адреса." },
                    { row: 9, email: "x@odc.local", message: "Невідома роль «boss»." },
                ],
            }),
        );
        const page = renderImport();

        pick(sheet);
        await page.user.click(await screen.findByRole("button", { name: "Перевірити" }));

        await findToast("Перевірку завершено. Знайдено помилок: 2.");
        expect(await screen.findByText("Знайдено помилок: 2")).toBeInTheDocument();
        expect(screen.getByText("Некоректна адреса.")).toBeInTheDocument();
        expect(screen.getByText("Невідома роль «boss».")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Імпортувати" })).toBeDisabled();
    });

    it("says the import was not done when the file got errors since the check", async () => {
        mockApi("post", "/api/v1/user-imports", ({ query }) =>
            Response.json(
                query.get("dryRun") === "true"
                    ? makeReport()
                    : makeReport({ errors: [{ row: 2, email: "maria@odc.local", message: "Обліковий запис уже існує." }] }),
            ),
        );
        const page = renderImport();

        pick(sheet);
        await page.user.click(await screen.findByRole("button", { name: "Перевірити" }));
        await findToast("Перевірку завершено: помилок не знайдено.");
        await page.user.click(screen.getByRole("button", { name: "Імпортувати" }));

        await findToast("Імпорт не виконано. Знайдено помилок: 1.");
        expect(screen.queryByText(/Імпорт завершено/)).not.toBeInTheDocument();
        expect(screen.getByText("Обліковий запис уже існує.")).toBeInTheDocument();
    });

    it("tells to warn the users when e-mail is not configured", async () => {
        mockApi("post", "/api/v1/user-imports", makeReport({ applied: true, invitationsSent: 0 }));
        const page = renderImport();

        pick(sheet);
        await page.user.click(await screen.findByRole("button", { name: "Перевірити" }));

        expect(await screen.findByText(/Надсилання листів не налаштовано: повідомте користувачів/)).toBeInTheDocument();
    });

    it("refuses a file over 5 MB", async () => {
        renderImport();
        const big = new File(["x"], "big.xlsx");
        Object.defineProperty(big, "size", { value: 6 * 1024 * 1024 });

        pick(big);

        await findToast("Файл імпорту не може бути більшим за 5 МБ.");
        expect(screen.getByRole("button", { name: "Перевірити" })).toBeDisabled();
    });

    it("reports a file the server cannot read", async () => {
        mockApi("post", "/api/v1/user-imports", () => problem(400, "validation", "Файл пошкоджено."));
        const page = renderImport();

        pick(sheet);
        await page.user.click(await screen.findByRole("button", { name: "Перевірити" }));

        await findToast("Перевірку файлу розпочато.");
        await findToast("Файл пошкоджено.");
        expect(screen.queryByText(/Перевірку завершено/)).not.toBeInTheDocument();
    });

    it("offers the templates, the role codes and the export", async () => {
        renderImport();

        expect(await screen.findByText("Коди ролей")).toBeInTheDocument();
        expect(screen.getByText("employee")).toBeInTheDocument();
        expect(
            within(card("Шаблон"))
                .getAllByRole("button")
                .map((button) => button.textContent),
        ).toEqual(["XLSX", "CSV"]);
        expect(
            within(card("Експорт"))
                .getAllByRole("button")
                .map((button) => button.textContent),
        ).toEqual(["XLSX", "CSV"]);
    });

    it("downloads the template in the chosen format with toasts", async () => {
        const saved = captureDownloads();
        const templates = mockApi("get", "/api/v1/user-imports/template", () => file("users-import-template.csv"));
        const page = renderImport();

        await page.user.click(within(card("Шаблон")).getByRole("button", { name: "CSV" }));

        await findToast("Шаблон готується до завантаження.");
        await findToast("Шаблон users-import-template.csv завантажено.");
        expect(templates[0]?.query.get("format")).toBe("Csv");
        expect(saved.map(({ name }) => name)).toEqual(["users-import-template.csv"]);
    });

    it("says when the template cannot be downloaded", async () => {
        captureDownloads();
        mockApi("get", "/api/v1/user-imports/template", 500);
        const page = renderImport();

        await page.user.click(within(card("Шаблон")).getByRole("button", { name: "XLSX" }));

        await findToast("Не вдалося завантажити шаблон. Спробуйте ще раз.");
    });

    it("exports every user with toasts for the start and the end", async () => {
        const saved = captureDownloads();
        const exports = mockApi("get", "/api/v1/user-exports", () => file("users-20261109-1000.xlsx"));
        const page = renderImport();

        await page.user.click(within(card("Експорт")).getByRole("button", { name: "XLSX" }));

        await findToast("Експорт користувачів розпочато. Файл завантажиться, щойно буде готовий.");
        await findToast("Експорт завершено: файл users-20261109-1000.xlsx завантажено.");
        expect(Object.fromEntries(exports[0]?.query ?? [])).toEqual({ format: "Xlsx" });
        expect(saved.map(({ name }) => name)).toEqual(["users-20261109-1000.xlsx"]);
    });

    it("hides the export from someone without the export permission", async () => {
        renderImport(userWith(Permissions.usersImport));

        await waitFor(() => expect(screen.getByText("Коди ролей")).toBeInTheDocument());
        expect(screen.queryByRole("heading", { name: "Експорт" })).not.toBeInTheDocument();
    });
});
