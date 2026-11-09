import { act, fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FileText } from "lucide-react";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderPage } from "@/test/render";
import { DataGrid, type GridColumn } from "./DataGrid";
import { Pagination, pageWindow } from "./Pagination";
import { SearchInput } from "./SearchInput";
import { Select } from "./Select";

/** The options of the drop-down tests. */
const options = [
    { value: "", label: "Усі" },
    { value: "draft", label: "Чернетка" },
    { value: "signing", label: "На підписанні", disabled: true },
    { value: "signed", label: "Підписано" },
    { value: "archived", label: "Архів" },
];

/**
 * A drop-down list that keeps its value.
 * @param props The change callback.
 * @returns The list.
 */
function Harness({ onChange }: { readonly onChange: (value: string) => void }) {
    const [value, setValue] = useState("");
    return (
        <Select
            aria-label="Статус"
            options={options}
            value={value}
            onChange={(next) => {
                setValue(next);
                onChange(next);
            }}
        />
    );
}

afterEach(() => vi.useRealTimers());

describe("Select", () => {
    it("opens with the mouse and takes the clicked option", async () => {
        const onChange = vi.fn();
        const user = userEvent.setup();
        render(<Harness onChange={onChange} />);
        const combobox = screen.getByRole("combobox", { name: "Статус" });

        await user.click(combobox);
        expect(combobox).toHaveAttribute("aria-expanded", "true");
        await user.click(screen.getByRole("option", { name: "Архів" }));

        expect(onChange).toHaveBeenCalledWith("archived");
        expect(combobox).toHaveTextContent("Архів");
        expect(combobox).toHaveAttribute("aria-expanded", "false");
    });

    it("moves with the arrows past disabled options and chooses with Enter", () => {
        const onChange = vi.fn();
        render(<Harness onChange={onChange} />);
        const combobox = screen.getByRole("combobox", { name: "Статус" });

        fireEvent.keyDown(combobox, { key: "ArrowDown" });
        fireEvent.keyDown(combobox, { key: "ArrowDown" });
        fireEvent.keyDown(combobox, { key: "ArrowDown" });
        expect(combobox.getAttribute("aria-activedescendant")).toBe(screen.getByRole("option", { name: "Підписано" }).id);
        fireEvent.keyDown(combobox, { key: "Enter" });

        expect(onChange).toHaveBeenLastCalledWith("signed");
    });

    it("jumps to the first and last options and closes with Escape without choosing", () => {
        const onChange = vi.fn();
        render(<Harness onChange={onChange} />);
        const combobox = screen.getByRole("combobox", { name: "Статус" });

        fireEvent.keyDown(combobox, { key: "Enter" });
        fireEvent.keyDown(combobox, { key: "End" });
        expect(combobox.getAttribute("aria-activedescendant")).toBe(screen.getByRole("option", { name: "Архів" }).id);
        fireEvent.keyDown(combobox, { key: "Home" });
        expect(combobox.getAttribute("aria-activedescendant")).toBe(screen.getByRole("option", { name: "Усі" }).id);
        fireEvent.keyDown(combobox, { key: "Escape" });

        expect(combobox).toHaveAttribute("aria-expanded", "false");
        expect(onChange).not.toHaveBeenCalled();
    });

    it("finds an option by its first letters", () => {
        const onChange = vi.fn();
        render(<Harness onChange={onChange} />);
        const combobox = screen.getByRole("combobox", { name: "Статус" });

        fireEvent.keyDown(combobox, { key: "ArrowDown" });
        fireEvent.keyDown(combobox, { key: "а" });
        fireEvent.keyDown(combobox, { key: "Enter" });

        expect(onChange).toHaveBeenLastCalledWith("archived");
    });

    it("marks the chosen and the disabled options", async () => {
        const user = userEvent.setup();
        render(<Select aria-label="Статус" options={options} value="draft" onChange={vi.fn()} />);

        await user.click(screen.getByRole("combobox", { name: "Статус" }));

        expect(screen.getByRole("option", { name: "Чернетка" })).toHaveAttribute("aria-selected", "true");
        expect(screen.getByRole("option", { name: "На підписанні" })).toHaveAttribute("aria-disabled", "true");
    });

    it("shows the placeholder without a value and does not open when disabled", async () => {
        const user = userEvent.setup();
        render(
            <Select aria-label="Роль" options={[{ value: "a", label: "А" }]} value="" placeholder="Оберіть…" disabled onChange={vi.fn()} />,
        );
        const combobox = screen.getByRole("combobox", { name: "Роль" });

        expect(combobox).toHaveTextContent("Оберіть…");
        await user.click(combobox);
        expect(combobox).toHaveAttribute("aria-expanded", "false");
    });
});

describe("SearchInput", () => {
    it("searches a moment after typing stops, with the text trimmed", () => {
        vi.useFakeTimers();
        const onSearch = vi.fn();
        render(<SearchInput value="" onSearch={onSearch} placeholder="Пошук" />);

        fireEvent.change(screen.getByRole("searchbox", { name: "Пошук" }), { target: { value: "  наказ " } });
        act(() => vi.advanceTimersByTime(300));
        expect(onSearch).not.toHaveBeenCalled();
        act(() => vi.advanceTimersByTime(100));

        expect(onSearch).toHaveBeenCalledExactlyOnceWith("наказ");
    });

    it("searches at once on Enter", () => {
        const onSearch = vi.fn();
        render(<SearchInput value="" onSearch={onSearch} placeholder="Пошук" />);
        const input = screen.getByRole("searchbox", { name: "Пошук" });

        fireEvent.change(input, { target: { value: "акт" } });
        fireEvent.keyDown(input, { key: "Enter" });

        expect(onSearch).toHaveBeenCalledWith("акт");
    });

    it("does not search again for the value it already shows and follows a new value from outside", () => {
        vi.useFakeTimers();
        const onSearch = vi.fn();
        const { rerender } = render(<SearchInput value="акт" onSearch={onSearch} placeholder="Пошук" />);

        act(() => vi.advanceTimersByTime(1000));
        expect(onSearch).not.toHaveBeenCalled();
        rerender(<SearchInput value="наказ" onSearch={onSearch} placeholder="Пошук" />);

        expect(screen.getByRole("searchbox", { name: "Пошук" })).toHaveValue("наказ");
        act(() => vi.advanceTimersByTime(1000));
        expect(onSearch).not.toHaveBeenCalled();
    });
});

describe("Pagination", () => {
    it.each([
        [1, 1, [1]],
        [1, 5, [1, 2, 3, 4, 5]],
        [5, 20, [1, 2, 3, 4, 5, 6, 7, null, 20]],
        [10, 20, [1, null, 8, 9, 10, 11, 12, null, 20]],
        [1, 20, [1, 2, 3, null, 20]],
        [20, 20, [1, null, 18, 19, 20]],
    ])("shows a window of pages around page %i of %i", (page, total, expected) => {
        expect(pageWindow(page, total)).toEqual(expected);
    });

    it("summarizes the rows and keeps the other view values in the page links", async () => {
        const { router, user } = renderPage(<Pagination page={2} pageSize={20} totalCount={45} />, {
            path: "/list",
            state: { view: "search=акт&page=2" },
        });

        expect(screen.getByText("21–40 з 45")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "2" })).toHaveAttribute("aria-current", "page");
        await user.click(screen.getByRole("link", { name: "Наступна сторінка" }));

        expect(router.state.location.state).toMatchObject({ view: "search=%D0%B0%D0%BA%D1%82&page=3" });
    });

    it("disables the previous page on the first page and the next one on the last", () => {
        const first = renderPage(<Pagination page={1} pageSize={20} totalCount={45} />, { path: "/list" });
        expect(screen.getByRole("link", { name: "Попередня сторінка" })).toHaveAttribute("aria-disabled", "true");
        expect(screen.getByRole("link", { name: "Наступна сторінка" })).not.toHaveAttribute("aria-disabled");
        first.unmount();

        renderPage(<Pagination page={3} pageSize={20} totalCount={45} />, { path: "/list" });
        expect(screen.getByText("41–45 з 45")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Наступна сторінка" })).toHaveAttribute("aria-disabled", "true");
    });

    it("shows no page links for a single page", () => {
        renderPage(<Pagination page={1} pageSize={20} totalCount={15} />, { path: "/list" });

        expect(screen.getByText("1–15 з 15")).toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Наступна сторінка" })).not.toBeInTheDocument();
    });

    it("counts an empty list as zero rows", () => {
        renderPage(<Pagination page={1} pageSize={20} totalCount={0} />, { path: "/list" });

        expect(screen.getByText("0–0 з 0")).toBeInTheDocument();
    });
});

describe("DataGrid", () => {
    /** A row of the test table. */
    interface Row {
        /** The id. */
        readonly id: string;
        /** The title. */
        readonly title: string;
    }

    /** The columns of the test table. */
    const columns: GridColumn<Row>[] = [
        { key: "title", title: "Назва", sortKey: "title", primary: true, render: (row) => row.title },
        { key: "id", title: "Код", render: (row) => row.id },
    ];

    it("shows the rows and sorts by a column, ascending first and then descending", async () => {
        const { router, user } = renderPage(
            <DataGrid
                columns={columns}
                result={{ items: [{ id: "a", title: "Акт" }], page: 1, pageSize: 20, totalCount: 1 }}
                rowKey={(row) => row.id}
                caption="Документи"
                emptyIcon={FileText}
                emptyTitle="Порожньо"
            />,
            { path: "/list", state: { view: "page=3" } },
        );

        expect(within(screen.getByRole("table", { name: "Документи" })).getByText("Акт")).toBeInTheDocument();
        await user.click(screen.getByRole("link", { name: /Назва/ }));
        expect(router.state.location.state).toMatchObject({ view: "sortBy=title&descending=false" });
        await user.click(screen.getByRole("link", { name: /Назва/ }));
        expect(router.state.location.state).toMatchObject({ view: "sortBy=title&descending=true" });
        expect(screen.getByRole("columnheader", { name: /Назва/ })).toHaveAttribute("aria-sort", "descending");
        expect(screen.queryByRole("link", { name: /Код/ })).not.toBeInTheDocument();
    });

    it("says when there are no rows", () => {
        renderPage(
            <DataGrid
                columns={columns}
                result={{ items: [], page: 1, pageSize: 20, totalCount: 0 }}
                rowKey={(row) => row.id}
                caption="Документи"
                emptyIcon={FileText}
                emptyTitle="Документів не знайдено"
                emptyText="Змініть фільтри."
            />,
            { path: "/list" },
        );

        expect(screen.getByText("Документів не знайдено")).toBeInTheDocument();
        expect(screen.getByText("Змініть фільтри.")).toBeInTheDocument();
    });
});
