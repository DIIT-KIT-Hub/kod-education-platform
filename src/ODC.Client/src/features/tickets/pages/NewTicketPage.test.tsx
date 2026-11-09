import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { NewTicketPage } from "./NewTicketPage";

/**
 * Renders the form of a new request.
 * @returns The rendered page.
 */
function renderForm() {
    return renderPage(<NewTicketPage />, { path: "/requests/new" });
}

/**
 * Chooses an option of the custom drop-down list.
 * @param page The rendered page.
 * @param name The list label.
 * @param option The option text.
 */
async function choose(page: ReturnType<typeof renderForm>, name: string, option: string) {
    const combobox = screen.getByRole("combobox", { name });
    await page.user.click(combobox);
    const listbox = document.getElementById(combobox.getAttribute("aria-controls") ?? "");
    await page.user.click(within(listbox as HTMLElement).getByRole("option", { name: option }));
}

/**
 * The hidden file input of the attachment picker.
 * @returns The input.
 */
function fileInput(): HTMLInputElement {
    return document.querySelector('input[type="file"]') as HTMLInputElement;
}

describe("NewTicketPage", () => {
    it("sends the chosen category, priority, texts and files and opens the new request", async () => {
        const calls = mockApi("post", "/api/v1/tickets", { id: "ticket-7" });
        const page = renderForm();

        await page.user.click(screen.getByRole("radio", { name: "Доступ і права" }));
        await page.user.type(screen.getByRole("textbox", { name: /Тема/ }), "  Немає доступу до реєстру  ");
        await page.user.type(screen.getByRole("textbox", { name: /Опис/ }), "Після входу розділ «Документи» порожній.");
        await choose(page, "Пріоритет", "Високий");
        await page.user.upload(fileInput(), new File(["png"], "screen.png", { type: "image/png" }));
        expect(screen.getByText("screen.png")).toBeInTheDocument();
        await page.user.click(screen.getByRole("button", { name: "Надіслати звернення" }));

        expect(await screen.findByText("Звернення надіслано")).toBeInTheDocument();
        expect(await navigatedTo()).toBe("/requests/ticket-7");
        const form = calls[0]?.body as FormData;
        expect(form.get("Category")).toBe("Access");
        expect(form.get("Priority")).toBe("High");
        expect(form.get("Subject")).toBe("Немає доступу до реєстру");
        expect(form.get("Description")).toBe("Після входу розділ «Документи» порожній.");
        expect((form.getAll("Files") as File[]).map((file) => file.name)).toEqual(["screen.png"]);
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("starts with a bug of normal priority", () => {
        renderForm();

        expect(screen.getByRole("radio", { name: "Помилка" })).toBeChecked();
        expect(screen.getByRole("combobox", { name: "Пріоритет" })).toHaveTextContent("Звичайний");
    });

    it("asks for a subject and a longer description without sending anything", async () => {
        const calls = mockApi("post", "/api/v1/tickets", { id: "never" });
        const page = renderForm();

        await page.user.type(screen.getByRole("textbox", { name: /Опис/ }), "Коротко");
        await page.user.click(screen.getByRole("button", { name: "Надіслати звернення" }));

        expect(await screen.findByText("Вкажіть тему.")).toBeInTheDocument();
        expect(screen.getByText("Опишіть звернення докладніше.")).toBeInTheDocument();
        expect(screen.getByRole("textbox", { name: /Тема/ })).toHaveAttribute("aria-invalid", "true");
        expect(calls).toHaveLength(0);
    });

    it("does not count spaces as a subject", async () => {
        const page = renderForm();

        await page.user.type(screen.getByRole("textbox", { name: /Тема/ }), "     ");
        await page.user.click(screen.getByRole("button", { name: "Надіслати звернення" }));

        expect(await screen.findByText("Вкажіть тему.")).toBeInTheDocument();
    });

    it("refuses a file of a forbidden type with a warning", async () => {
        renderForm();
        fireEvent.change(fileInput(), { target: { files: [new File(["MZ"], "setup.exe", { type: "application/octet-stream" })] } });

        expect(await screen.findByText("Файл «setup.exe» має недозволений тип.")).toBeInTheDocument();
        expect(screen.queryByText("setup.exe")).not.toBeInTheDocument();
    });

    it("refuses more than ten files", async () => {
        const page = renderForm();
        const files = Array.from({ length: 11 }, (_, index) => new File(["x"], `note-${index}.txt`, { type: "text/plain" }));

        await page.user.upload(fileInput(), files);

        expect(await screen.findByText("Можна додати не більше 10 файлів.")).toBeInTheDocument();
        expect(screen.queryByText("note-0.txt")).not.toBeInTheDocument();
    });

    it("removes an attached file", async () => {
        const page = renderForm();

        await page.user.upload(fileInput(), new File(["x"], "log.txt", { type: "text/plain" }));
        await page.user.click(screen.getByRole("button", { name: "Прибрати файл log.txt" }));

        expect(screen.queryByText("log.txt")).not.toBeInTheDocument();
    });

    it("keeps the form and reports the server's refusal", async () => {
        mockApi("post", "/api/v1/tickets", () => problem(400, "validation", "Файл «a.zip» більший за 25 МБ."));
        const page = renderForm();

        await page.user.type(screen.getByRole("textbox", { name: /Тема/ }), "Тема");
        await page.user.type(screen.getByRole("textbox", { name: /Опис/ }), "Достатньо довгий опис.");
        await page.user.click(screen.getByRole("button", { name: "Надіслати звернення" }));

        expect(await screen.findByText("Файл «a.zip» більший за 25 МБ.")).toBeInTheDocument();
        await waitFor(() => expect(screen.getByRole("button", { name: "Надіслати звернення" })).toBeEnabled());
        expect(screen.getByRole("textbox", { name: /Тема/ })).toHaveValue("Тема");
    });
});
