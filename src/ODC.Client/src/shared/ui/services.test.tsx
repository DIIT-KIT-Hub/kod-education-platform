import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { Upload } from "lucide-react";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderPage } from "@/test/render";
import { confirm, confirmWithOption } from "./ConfirmHost";
import { CopyButton } from "./CopyButton";
import { FileDrop } from "./FileDrop";
import { PasswordInput } from "./PasswordInput";
import { toast } from "./Toast";
import { Wizard, type WizardStep } from "./Wizard";

afterEach(() => vi.useRealTimers());

/**
 * The open confirmation dialog.
 * @returns Queries inside it.
 */
async function dialog() {
    return within(await waitFor(() => document.querySelector("dialog[open]") as HTMLElement));
}

describe("toast", () => {
    it("shows a message with its tone and closes it with the button", async () => {
        const { user } = renderPage(<p>page</p>);

        act(() => toast("Збережено"));
        act(() => toast("Помилка збереження", { tone: "error" }));

        const saved = await screen.findByText("Збережено");
        expect(saved.closest("[data-tone]")).toHaveAttribute("data-tone", "success");
        expect(screen.getByText("Помилка збереження").closest("[data-tone]")).toHaveAttribute("data-tone", "error");
        await user.click(within(saved.closest("[data-tone]") as HTMLElement).getByRole("button", { name: "Закрити" }));
        await waitFor(() => expect(screen.queryByText("Збережено")).not.toBeInTheDocument());
        expect(screen.getByText("Помилка збереження")).toBeInTheDocument();
    });

    it("goes away on its own, errors staying longer", async () => {
        vi.useFakeTimers();
        renderPage(<p>page</p>);

        act(() => toast("Збережено"));
        act(() => toast("Помилка", { tone: "error" }));
        act(() => vi.advanceTimersByTime(6000));

        expect(screen.queryByText("Збережено")).not.toBeInTheDocument();
        expect(screen.getByText("Помилка")).toBeInTheDocument();
        act(() => vi.advanceTimersByTime(3000));
        expect(screen.queryByText("Помилка")).not.toBeInTheDocument();
    });

    it("does not stack a message that is already shown, but shows it again once it has gone", async () => {
        vi.useFakeTimers();
        renderPage(<p>page</p>);

        act(() => toast("Немає з'єднання", { tone: "error" }));
        act(() => toast("Немає з'єднання", { tone: "error" }));
        act(() => toast("Немає з'єднання", { tone: "warning" }));
        expect(screen.getAllByText("Немає з'єднання")).toHaveLength(2);

        act(() => vi.advanceTimersByTime(8000));
        expect(screen.queryByText("Немає з'єднання")).not.toBeInTheDocument();
        act(() => toast("Немає з'єднання", { tone: "error" }));
        expect(screen.getAllByText("Немає з'єднання")).toHaveLength(1);
    });
});

describe("confirm", () => {
    it("resolves true when confirmed and false when cancelled", async () => {
        const { user } = renderPage(<p>page</p>);

        let answer: Promise<boolean> = Promise.resolve(false);
        act(() => {
            answer = confirm({ title: "Видалити документ?", text: "Цю дію не можна скасувати.", confirmText: "Видалити", danger: true });
        });
        const first = await dialog();
        expect(first.getByText("Цю дію не можна скасувати.")).toBeInTheDocument();
        await user.click(first.getByRole("button", { name: "Видалити" }));
        await expect(answer).resolves.toBe(true);

        act(() => {
            answer = confirm({ title: "Вийти?" });
        });
        await user.click((await dialog()).getByRole("button", { name: "Скасувати" }));
        await expect(answer).resolves.toBe(false);
    });

    it("cancels the previous question when a new one comes", async () => {
        renderPage(<p>page</p>);

        let first: Promise<boolean> = Promise.resolve(true);
        act(() => {
            first = confirm({ title: "Перше?" });
        });
        act(() => void confirm({ title: "Друге?" }));

        await expect(first).resolves.toBe(false);
        expect(await screen.findByRole("heading", { name: "Друге?" })).toBeInTheDocument();
    });

    it("returns the option, or null when cancelled", async () => {
        const { user } = renderPage(<p>page</p>);

        let answer: Promise<boolean | null> = Promise.resolve(null);
        act(() => {
            answer = confirmWithOption({
                title: "Видалити чат?",
                confirmText: "Видалити",
                option: { label: "Також для Марії", checked: true },
            });
        });
        const box = await dialog();
        await user.click(box.getByRole("checkbox", { name: /Також для Марії/ }));
        await user.click(box.getAllByRole("button", { name: "Видалити" }).at(-1) as HTMLElement);
        await expect(answer).resolves.toBe(false);

        act(() => {
            answer = confirmWithOption({ title: "Видалити чат?", option: { label: "Також для Марії" } });
        });
        await user.click((await dialog()).getByRole("button", { name: "Скасувати" }));
        await expect(answer).resolves.toBeNull();
    });
});

describe("CopyButton", () => {
    it("copies the value and says so", async () => {
        const { user } = renderPage(<CopyButton value="AAAA-1111">Скопіювати</CopyButton>);
        // After the render: user-event puts its own clipboard stub in place when it is set up.
        const writeText = vi.fn(() => Promise.resolve());
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });

        await user.click(screen.getByRole("button", { name: "Скопіювати" }));

        expect(writeText).toHaveBeenCalledWith("AAAA-1111");
        expect(await screen.findByText("Скопійовано")).toBeInTheDocument();
    });

    it("reports a refused clipboard", async () => {
        const { user } = renderPage(<CopyButton value="x">Скопіювати</CopyButton>);
        Object.defineProperty(navigator, "clipboard", {
            value: { writeText: () => Promise.reject(new Error("denied")) },
            configurable: true,
        });

        await user.click(screen.getByRole("button", { name: "Скопіювати" }));

        expect(await screen.findByText("Не вдалося скопіювати")).toBeInTheDocument();
    });
});

describe("PasswordInput", () => {
    it("hides the password until asked to show it", async () => {
        const { user } = renderPage(<PasswordInput aria-label="Пароль" defaultValue="secret" />);
        const input = screen.getByLabelText("Пароль");

        expect(input).toHaveAttribute("type", "password");
        await user.click(screen.getByRole("button", { name: "Показати пароль" }));
        expect(input).toHaveAttribute("type", "text");
        expect(screen.getByRole("button", { name: "Сховати пароль" })).toHaveAttribute("aria-pressed", "true");
        await user.click(screen.getByRole("button", { name: "Сховати пароль" }));
        expect(input).toHaveAttribute("type", "password");
    });
});

describe("FileDrop", () => {
    it("hands over the picked files and lets the same file be picked again", () => {
        const onFiles = vi.fn();
        render(<FileDrop icon={Upload} title="Оберіть файл" accept=".pdf" onFiles={onFiles} />);
        const input = document.querySelector('input[type="file"]') as HTMLInputElement;
        const file = new File(["x"], "a.pdf");

        fireEvent.change(input, { target: { files: [file] } });

        expect(onFiles).toHaveBeenCalledWith([file]);
        expect(input.value).toBe("");
        expect(input).toHaveAttribute("accept", ".pdf");
    });

    it("marks the zone while a file is dragged over it", () => {
        render(<FileDrop icon={Upload} title="Оберіть файл" onFiles={vi.fn()} />);
        const zone = screen.getByText("Оберіть файл").closest("label") as HTMLElement;

        fireEvent.dragEnter(zone);
        expect(zone).toHaveAttribute("data-dragover", "true");
        fireEvent.dragLeave(zone);
        expect(zone).not.toHaveAttribute("data-dragover");
    });
});

describe("Wizard", () => {
    /**
     * A wizard whose first step completes once its box is ticked.
     * @param props The finish action.
     * @returns The wizard.
     */
    function Harness({ run }: { readonly run: () => Promise<void> }) {
        const [ready, setReady] = useState(false);
        const steps: WizardStep[] = [
            {
                id: "file",
                title: "Файл",
                complete: ready,
                content: <input type="checkbox" aria-label="Готово" onChange={(event) => setReady(event.target.checked)} />,
            },
            { id: "key", title: "Ключ", content: <p>Крок ключа</p> },
        ];
        return <Wizard label="Підписання" steps={steps} finish={{ text: "Підписати", run }} />;
    }

    it("moves on only from a complete step and finishes on the last one", async () => {
        const run = vi.fn(() => Promise.resolve());
        const { user } = renderPage(<Harness run={run} />);

        expect(screen.getByText(/Крок 1 з 2/)).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Далі" })).toBeDisabled();
        await user.click(screen.getByRole("checkbox", { name: "Готово" }));
        await user.click(screen.getByRole("button", { name: "Далі" }));

        expect(await screen.findByText("Крок ключа")).toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Підписати" }));
        expect(run).toHaveBeenCalledOnce();

        await user.click(screen.getByRole("button", { name: "Назад" }));
        expect(screen.getByRole("checkbox", { name: "Готово" })).toBeInTheDocument();
    });
});
