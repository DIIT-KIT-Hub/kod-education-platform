import { fireEvent, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Schemas } from "@/test/factories";
import { renderPage } from "@/test/render";
import { makeReactions } from "@/features/news/testing";
import { ReactionBar, type ReactionBarProps } from "./ReactionBar";

/**
 * Renders a reaction bar.
 * @param props Changed props.
 * @returns The rendered page and the reaction handler.
 */
function renderBar(props: Partial<ReactionBarProps> = {}) {
    const onReact = vi.fn<(kind: Schemas["ReactionKind"]) => Promise<void>>(() => Promise.resolve());
    const rendered = renderPage(<ReactionBar summary={makeReactions()} canReact onReact={onReact} {...props} />);
    return { ...rendered, onReact };
}

describe("ReactionBar", () => {
    it("shows a chip per emotion with its count and marks the user's own", () => {
        renderBar({
            summary: makeReactions({
                counts: [
                    { kind: "Like", count: 5 },
                    { kind: "Love", count: 2 },
                ],
                mine: "Love",
            }),
        });

        const like = screen.getByRole("button", { name: "Подобається: 5" });
        expect(like).toHaveAttribute("aria-pressed", "false");
        expect(like).toHaveTextContent("👍");
        expect(screen.getByRole("button", { name: "Любов: 2" })).toHaveAttribute("aria-pressed", "true");
        // The picker button has no text once there are chips.
        expect(screen.getByRole("button", { name: "Поставити реакцію" })).not.toHaveTextContent("Реакція");
    });

    it("names the picker when there are no reactions yet", () => {
        renderBar();

        expect(screen.getByRole("button", { name: "Поставити реакцію" })).toHaveTextContent("Реакція");
    });

    it("keeps the compact picker without text", () => {
        renderBar({ compact: true });

        expect(screen.getByRole("button", { name: "Поставити реакцію" })).not.toHaveTextContent("Реакція");
    });

    it("toggles an emotion by its chip", async () => {
        const { user, onReact } = renderBar({ summary: makeReactions({ counts: [{ kind: "Clap", count: 1 }], mine: "Clap" }) });

        await user.click(screen.getByRole("button", { name: "Оплески: 1" }));

        expect(onReact).toHaveBeenCalledWith("Clap");
    });

    it("picks any of the ten emotions from the menu", async () => {
        const { user, onReact } = renderBar({ summary: makeReactions({ counts: [{ kind: "Wow", count: 1 }], mine: "Wow" }) });

        const add = screen.getByRole("button", { name: "Поставити реакцію" });
        expect(add).toHaveAttribute("aria-expanded", "false");
        await user.click(add);
        expect(add).toHaveAttribute("aria-expanded", "true");

        const menu = screen.getByRole("menu");
        const options = within(menu).getAllByRole("menuitemradio");
        expect(options.map((option) => option.getAttribute("aria-label"))).toEqual([
            "Подобається",
            "Не подобається",
            "Любов",
            "Оплески",
            "Вітаю",
            "Смішно",
            "Вражає",
            "Сумнівно",
            "Сумно",
            "Обурює",
        ]);
        expect(within(menu).getByRole("menuitemradio", { name: "Вражає" })).toHaveAttribute("aria-checked", "true");

        await user.click(within(menu).getByRole("menuitemradio", { name: "Вітаю" }));

        expect(onReact).toHaveBeenCalledWith("Celebrate");
        expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    });

    it("closes the menu with Escape, a second click or a click elsewhere", async () => {
        const { user } = renderBar();
        const add = screen.getByRole("button", { name: "Поставити реакцію" });

        await user.click(add);
        fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
        expect(screen.queryByRole("menu")).not.toBeInTheDocument();

        await user.click(add);
        await user.click(add);
        expect(screen.queryByRole("menu")).not.toBeInTheDocument();

        await user.click(add);
        fireEvent.keyDown(screen.getByRole("menu"), { key: "a" });
        expect(screen.getByRole("menu")).toBeInTheDocument();
        await user.click(document.body);
        expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    });

    it("blocks the bar while a reaction is being saved", async () => {
        let finish = () => {};
        const onReact = vi.fn(
            () =>
                new Promise<void>((resolve) => {
                    finish = resolve;
                }),
        );
        const { user } = renderBar({ summary: makeReactions({ counts: [{ kind: "Sad", count: 1 }] }), onReact });

        await user.click(screen.getByRole("button", { name: "Сумно: 1" }));

        expect(screen.getByRole("button", { name: "Сумно: 1" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Поставити реакцію" })).toBeDisabled();
        finish();
        expect(await screen.findByRole("button", { name: "Сумно: 1" })).toBeEnabled();
    });

    it("shows the counts read-only when the user may not react", async () => {
        const { user, onReact } = renderBar({ canReact: false, summary: makeReactions({ counts: [{ kind: "Angry", count: 3 }] }) });

        expect(screen.getByRole("button", { name: "Обурює: 3" })).toBeDisabled();
        expect(screen.queryByRole("button", { name: "Поставити реакцію" })).not.toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Обурює: 3" }));
        expect(onReact).not.toHaveBeenCalled();
    });
});
