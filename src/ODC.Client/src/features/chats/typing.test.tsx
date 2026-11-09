import { act, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { maria, petro } from "./testing";
import { clearTyping, noteTyping, useTyping } from "./typing";

/**
 * Shows who is typing in a chat.
 * @param props The props.
 * @param props.chatId The chat.
 * @returns The names.
 */
function Typing({ chatId }: { readonly chatId: string }) {
    const people = useTyping(chatId);
    return <p data-testid={chatId}>{people.map((user) => user.fullName).join(", ") || "nobody"}</p>;
}

describe("typing", () => {
    beforeEach(() => {
        vi.useFakeTimers({ shouldAdvanceTime: true });
    });

    afterEach(() => {
        act(() => {
            clearTyping("a", maria.id);
            clearTyping("a", petro.id);
            clearTyping("b", maria.id);
        });
        vi.useRealTimers();
    });

    it("shows nobody at first and keeps a stable snapshot", () => {
        render(<Typing chatId="a" />);

        expect(screen.getByTestId("a")).toHaveTextContent("nobody");
    });

    it("shows the people typing in each chat separately", () => {
        render(
            <>
                <Typing chatId="a" />
                <Typing chatId="b" />
            </>,
        );

        act(() => {
            noteTyping("a", maria);
            noteTyping("a", petro);
            noteTyping("b", maria);
        });

        expect(screen.getByTestId("a")).toHaveTextContent(`${maria.fullName}, ${petro.fullName}`);
        expect(screen.getByTestId("b")).toHaveTextContent(maria.fullName);
    });

    it("hides a signal after 4.5 seconds and keeps a renewed one", () => {
        render(<Typing chatId="a" />);

        act(() => noteTyping("a", maria));
        act(() => vi.advanceTimersByTime(3000));
        act(() => noteTyping("a", petro));
        expect(screen.getByTestId("a")).toHaveTextContent(`${maria.fullName}, ${petro.fullName}`);

        act(() => vi.advanceTimersByTime(2000));
        expect(screen.getByTestId("a")).toHaveTextContent(petro.fullName);
        expect(screen.getByTestId("a")).not.toHaveTextContent(maria.fullName);

        act(() => vi.advanceTimersByTime(3000));
        expect(screen.getByTestId("a")).toHaveTextContent("nobody");
    });

    it("hides a person whose message arrived", () => {
        render(<Typing chatId="a" />);
        act(() => noteTyping("a", maria));

        act(() => clearTyping("a", maria.id));

        expect(screen.getByTestId("a")).toHaveTextContent("nobody");
    });

    it("ignores clearing a person who is not typing", () => {
        render(<Typing chatId="a" />);

        act(() => {
            clearTyping("a", maria.id);
            clearTyping("missing", maria.id);
        });

        expect(screen.getByTestId("a")).toHaveTextContent("nobody");
    });

    it("stops telling an unmounted component", () => {
        const { unmount } = render(<Typing chatId="a" />);
        unmount();

        expect(() => act(() => noteTyping("a", maria))).not.toThrow();
    });
});
