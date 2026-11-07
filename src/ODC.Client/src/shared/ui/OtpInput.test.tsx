import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { initials } from "./Avatar";
import { OtpInput } from "./OtpInput";

/**
 * A controlled one-time code input for the tests.
 * @param props Called when every cell is filled.
 * @returns The input.
 */
function Harness({ onComplete }: { readonly onComplete: (value: string) => void }) {
    const [value, setValue] = useState("");
    return <OtpInput id="code" value={value} onChange={setValue} onComplete={onComplete} />;
}

describe("OtpInput", () => {
    it("keeps digits only, draws them into the cells and reports a full code once", () => {
        const onComplete = vi.fn();
        const { container } = render(<Harness onComplete={onComplete} />);
        const input = screen.getByRole("textbox");

        fireEvent.change(input, { target: { value: "12 3-4" } });
        expect(input).toHaveProperty("value", "1234");
        expect(container.querySelectorAll("[data-filled]")).toHaveLength(4);
        expect(onComplete).not.toHaveBeenCalled();

        fireEvent.change(input, { target: { value: "1234567" } });
        expect(input).toHaveProperty("value", "123456");
        expect(onComplete).toHaveBeenCalledExactlyOnceWith("123456");
    });
});

describe("initials", () => {
    it.each([
        ["Шевченко Тарас Григорович", "ШТ"],
        ["Тарас", "Т"],
        ["", "?"],
    ])("of %s is %s", (name, expected) => {
        expect(initials(name)).toBe(expected);
    });
});
