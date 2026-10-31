import { useLingui } from "@lingui/react/macro";
import { CalendarDays, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";
import { useEffect, useEffectEvent, useId, useRef, useState, type KeyboardEvent, type RefObject } from "react";
import { formattingLocale } from "@/shared/i18n/format";
import { inkHandlers } from "@/shared/lib/ink";
import { Button } from "./Button";
import styles from "./DatePicker.module.css";
import { Icon } from "./Icon";

/** The distance between the field and the calendar. */
const gap = 6;

/** The smallest distance between the calendar and the window edge. */
const margin = 8;

/**
 * Props of {@link DatePicker}.
 */
export interface DatePickerProps {
    /** The date as `yyyy-MM-dd`, or an empty string. */
    readonly value: string;
    /** Called with the new date (`yyyy-MM-dd`, or an empty string when cleared). */
    readonly onChange: (value: string) => void;
    /** The earliest allowed date (`yyyy-MM-dd`). */
    readonly min?: string | undefined;
    /** The latest allowed date (`yyyy-MM-dd`). */
    readonly max?: string | undefined;
    /** The placeholder of the empty field. */
    readonly placeholder?: string;
    /** A small field for toolbars. */
    readonly compact?: boolean;
    /** The id of the text field (from `Field`). */
    readonly id?: string;
    /** The accessible name when the field has no visible label. */
    readonly "aria-label"?: string;
    /** The hint and error ids. */
    readonly "aria-describedby"?: string | undefined;
    /** Marks an invalid value. */
    readonly "aria-invalid"?: boolean | undefined;
}

/**
 * Pads a number to two digits.
 * @param value The number.
 * @returns The text.
 */
function pad(value: number): string {
    return String(value).padStart(2, "0");
}

/**
 * Formats a local date as `yyyy-MM-dd`.
 * @param date The date.
 * @returns The ISO date.
 */
export function toIsoDate(date: Date): string {
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Builds a date when the parts form a real calendar day.
 * @param year The year.
 * @param month The month (1–12).
 * @param day The day.
 * @returns The date, or null.
 */
function valid(year: number, month: number, day: number): Date | null {
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
}

/**
 * Parses `yyyy-MM-dd`.
 * @param value The ISO date.
 * @returns The local date, or null.
 */
export function fromIsoDate(value: string | null | undefined): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/u.exec(value ?? "");
    return match ? valid(Number(match[1]), Number(match[2]), Number(match[3])) : null;
}

/**
 * The separator of the typed date in the interface language: 25.10.2026 or 25/10/2026.
 * @param locale The interface language.
 * @returns The separator.
 */
function separator(locale: string): string {
    return locale === "en" ? "/" : ".";
}

/**
 * Formats a date as the field shows it (day, month, year).
 * @param date The date.
 * @param locale The interface language.
 * @returns The text.
 */
function display(date: Date, locale: string): string {
    const by = separator(locale);
    return `${pad(date.getDate())}${by}${pad(date.getMonth() + 1)}${by}${date.getFullYear()}`;
}

/**
 * Parses a typed date: day, month and year with any separator, eight digits in a row, two-digit years or ISO input.
 * @param text The typed text.
 * @returns The date, or null when the text is not a date.
 */
export function parseTypedDate(text: string): Date | null {
    const value = text.trim();
    if (/^\d{4}-\d{1,2}-\d{1,2}$/u.test(value)) {
        const [year, month, day] = value.split("-").map(Number);
        return valid(year ?? 0, month ?? 0, day ?? 0);
    }

    let parts = value.split(/\D+/u).filter(Boolean);
    if (parts.length === 1 && /^\d{8}$/u.test(parts[0] ?? "")) {
        const digits = parts[0] ?? "";
        parts = [digits.slice(0, 2), digits.slice(2, 4), digits.slice(4)];
    }

    if (parts.length !== 3) {
        return null;
    }

    const [day, month, year] = parts.map(Number);
    return valid((year ?? 0) < 100 ? (year ?? 0) + 2000 : (year ?? 0), month ?? 0, day ?? 0);
}

/**
 * Today at local midnight.
 * @returns The date.
 */
function today(): Date {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

/**
 * A date field: typed in the day-month-year order of the interface language or picked in a calendar (weeks start on
 * Monday). The calendar is a popover in the top layer next to the field.
 * @param props The props.
 * @returns The field.
 */
export function DatePicker({ value, onChange, min, max, placeholder, compact, id, ...aria }: DatePickerProps) {
    const { i18n, t } = useLingui();
    const locale = i18n.locale;
    const selected = fromIsoDate(value);
    const [text, setText] = useState(selected ? display(selected, locale) : "");
    const [shown, setShown] = useState(value);
    const [open, setOpen] = useState(false);
    const [rejected, setRejected] = useState(false);
    const root = useRef<HTMLDivElement>(null);
    const toggle = useRef<HTMLButtonElement>(null);

    // The value changed outside (another filter, a reset): show it.
    if (shown !== value) {
        setShown(value);
        setText(selected ? display(selected, locale) : "");
    }

    /**
     * Whether a date is outside the allowed range.
     * @param date The date.
     * @returns True when not allowed.
     */
    function outOfRange(date: Date): boolean {
        const iso = toIsoDate(date);
        return (min !== undefined && min !== "" && iso < min) || (max !== undefined && max !== "" && iso > max);
    }

    /**
     * Sets the value.
     * @param date The date, or null to clear.
     */
    function commit(date: Date | null): void {
        const next = date ? toIsoDate(date) : "";
        setText(date ? display(date, locale) : "");
        if (next !== value) {
            setShown(next);
            onChange(next);
        }
    }

    /**
     * Applies the typed text: a valid date is normalized, an empty field clears the value, anything else is rejected.
     */
    function commitTyped(): void {
        if (text.trim() === "") {
            commit(null);
            return;
        }

        const date = parseTypedDate(text);
        if (date && !outOfRange(date)) {
            commit(date);
            return;
        }

        setText(selected ? display(selected, locale) : "");
        setRejected(true);
        window.setTimeout(() => setRejected(false), 400);
    }

    return (
        <div
            ref={root}
            className={[styles.picker, compact ? styles.compact : undefined].filter(Boolean).join(" ")}
            data-open={open || undefined}
        >
            <input
                {...aria}
                id={id}
                className={styles.input}
                data-rejected={rejected || undefined}
                value={text}
                placeholder={placeholder ?? (locale === "en" ? "dd/mm/yyyy" : "дд.мм.рррр")}
                inputMode="numeric"
                autoComplete="off"
                onChange={(event) => setText(event.target.value)}
                onBlur={commitTyped}
                onKeyDown={(event) => {
                    if (event.key === "Enter") {
                        event.preventDefault();
                        commitTyped();
                    } else if (event.key === "ArrowDown" && event.altKey) {
                        event.preventDefault();
                        setOpen(true);
                    }
                }}
            />
            <button
                ref={toggle}
                type="button"
                className={styles.toggle}
                aria-label={t`Оберіть дату`}
                aria-expanded={open}
                aria-haspopup="dialog"
                title={t`Оберіть дату`}
                onClick={() => setOpen((current) => !current)}
                {...inkHandlers}
            >
                <Icon icon={CalendarDays} />
            </button>
            {open && (
                <Calendar
                    anchor={root}
                    selected={selected}
                    start={selected ?? parseTypedDate(text) ?? today()}
                    outOfRange={outOfRange}
                    onPick={(date) => {
                        commit(date);
                        setOpen(false);
                        toggle.current?.focus();
                    }}
                    onClose={(returnFocus) => {
                        setOpen(false);
                        if (returnFocus) {
                            toggle.current?.focus();
                        }
                    }}
                />
            )}
        </div>
    );
}

/**
 * Props of {@link Calendar}.
 */
interface CalendarProps {
    /** The field the calendar opens next to. */
    readonly anchor: RefObject<HTMLDivElement | null>;
    /** The chosen date. */
    readonly selected: Date | null;
    /** The day focused first. */
    readonly start: Date;
    /** Whether a day may not be chosen. */
    readonly outOfRange: (date: Date) => boolean;
    /** Called with the chosen day, or null for "clear". */
    readonly onPick: (date: Date | null) => void;
    /** Closes the calendar; `returnFocus` moves the focus back to the field. */
    readonly onClose: (returnFocus: boolean) => void;
}

/**
 * The month calendar: header with month and year steps, a grid of 6 weeks, "today" and "clear". Arrow keys move by
 * day and week, Page Up / Page Down by month (with Shift by year), Home / End to the week edges.
 * @param props The props.
 * @returns The popover.
 */
function Calendar({ anchor, selected, start, outOfRange, onPick, onClose }: CalendarProps) {
    const { t } = useLingui();
    const titleId = useId();
    const panel = useRef<HTMLDivElement>(null);
    const [focused, setFocused] = useState(start);
    const [direction, setDirection] = useState<"next" | "prev" | null>(null);
    const [turn, setTurn] = useState(0);
    const moved = useRef(true);
    const now = today();
    const locale = formattingLocale();
    const month = new Date(focused.getFullYear(), focused.getMonth(), 1);
    const monthName = new Intl.DateTimeFormat(locale, { month: "long" }).format(month);
    const weekday = new Intl.DateTimeFormat(locale, { weekday: "short" });
    const dayName = new Intl.DateTimeFormat(locale, { day: "numeric", month: "long", year: "numeric" });
    const offset = (month.getDay() + 6) % 7;
    const close = useEffectEvent(onClose);
    const days = Array.from({ length: 42 }, (_, index) => new Date(month.getFullYear(), month.getMonth(), 1 - offset + index));

    // Shows the calendar in the top layer below the field (above when there is no room), inside the window.
    useEffect(() => {
        const element = panel.current;
        const field = anchor.current;
        if (!element || !field) {
            return;
        }

        element.showPopover();
        const rect = field.getBoundingClientRect();
        const width = document.documentElement.clientWidth;
        const height = document.documentElement.clientHeight;
        const below = height - rect.bottom - gap - margin;
        const placeBelow = element.offsetHeight <= below || below >= rect.top - gap - margin;
        const top = placeBelow ? rect.bottom + gap : rect.top - gap - element.offsetHeight;
        element.style.top = `${Math.max(margin, Math.min(top, height - element.offsetHeight - margin))}px`;
        element.style.left = `${Math.max(margin, Math.min(rect.left, width - element.offsetWidth - margin))}px`;
        element.dataset.placement = placeBelow ? "bottom" : "top";

        const outside = (event: PointerEvent) => {
            if (!element.contains(event.target as Node) && !field.contains(event.target as Node)) {
                close(false);
            }
        };
        const scroll = (event: Event) => {
            if (!element.contains(event.target as Node)) {
                close(false);
            }
        };
        const resize = () => close(false);
        document.addEventListener("pointerdown", outside, true);
        document.addEventListener("scroll", scroll, true);
        window.addEventListener("resize", resize);
        return () => {
            document.removeEventListener("pointerdown", outside, true);
            document.removeEventListener("scroll", scroll, true);
            window.removeEventListener("resize", resize);
            if (element.matches(":popover-open")) {
                element.hidePopover();
            }
        };
    }, [anchor]);

    // The keyboard focus follows the focused day once the user moved it with the keyboard.
    useEffect(() => {
        if (moved.current) {
            panel.current?.querySelector<HTMLButtonElement>("[tabindex='0']")?.focus({ preventScroll: true });
        }
    }, [focused]);

    /**
     * Moves the focused day.
     * @param date The new day.
     * @param keyboard Whether the keyboard moved it (the focus follows).
     */
    function focus(date: Date, keyboard: boolean): void {
        if (date.getMonth() !== focused.getMonth() || date.getFullYear() !== focused.getFullYear()) {
            setDirection(date > focused ? "next" : "prev");
            setTurn((count) => count + 1);
        }

        moved.current = keyboard;
        setFocused(date);
    }

    /**
     * Moves by months, keeping the day where possible.
     * @param months The number of months (negative goes back).
     * @param keyboard Whether the keyboard moved it.
     */
    function shift(months: number, keyboard: boolean): void {
        const target = new Date(focused.getFullYear(), focused.getMonth() + months, 1);
        const last = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
        focus(new Date(target.getFullYear(), target.getMonth(), Math.min(focused.getDate(), last)), keyboard);
    }

    /**
     * Handles the keys of the day grid.
     * @param event The key event.
     */
    function keyDown(event: KeyboardEvent<HTMLDivElement>): void {
        const day = (offsetDays: number) =>
            focus(new Date(focused.getFullYear(), focused.getMonth(), focused.getDate() + offsetDays), true);
        const weekdayIndex = (focused.getDay() + 6) % 7;
        const keys: Record<string, () => void> = {
            ArrowLeft: () => day(-1),
            ArrowRight: () => day(1),
            ArrowUp: () => day(-7),
            ArrowDown: () => day(7),
            Home: () => day(-weekdayIndex),
            End: () => day(6 - weekdayIndex),
            PageUp: () => shift(event.shiftKey ? -12 : -1, true),
            PageDown: () => shift(event.shiftKey ? 12 : 1, true),
        };
        const action = keys[event.key];
        if (action) {
            event.preventDefault();
            action();
        }
    }

    /**
     * A month or year step button.
     * @param icon The icon.
     * @param label The accessible name.
     * @param months The step in months.
     * @returns The button.
     */
    const nav = (icon: typeof ChevronLeft, label: string, months: number) => (
        <button type="button" className={styles.nav} aria-label={label} title={label} onClick={() => shift(months, false)} {...inkHandlers}>
            <Icon icon={icon} />
        </button>
    );

    return (
        <div
            ref={panel}
            className={styles.calendar}
            popover="manual"
            role="dialog"
            aria-labelledby={titleId}
            onKeyDown={(event) => {
                if (event.key === "Escape") {
                    event.preventDefault();
                    event.stopPropagation();
                    onClose(true);
                }
            }}
        >
            <div className={styles.header}>
                {nav(ChevronsLeft, t`Попередній рік`, -12)}
                {nav(ChevronLeft, t`Попередній місяць`, -1)}
                <div id={titleId} className={styles.title} aria-live="polite">
                    {monthName.charAt(0).toLocaleUpperCase() + monthName.slice(1)} {month.getFullYear()}
                </div>
                {nav(ChevronRight, t`Наступний місяць`, 1)}
                {nav(ChevronsRight, t`Наступний рік`, 12)}
            </div>
            <div key={turn} className={styles.grid} role="grid" data-direction={direction ?? undefined} onKeyDown={keyDown}>
                {days.slice(0, 7).map((date) => (
                    <div key={`w${date.getDay()}`} className={styles.weekday} role="columnheader">
                        {weekday.format(date).replace(".", "")}
                    </div>
                ))}
                {days.map((date) => {
                    const time = date.getTime();
                    return (
                        <button
                            key={time}
                            type="button"
                            className={styles.day}
                            role="gridcell"
                            aria-label={dayName.format(date)}
                            aria-selected={selected?.getTime() === time || undefined}
                            aria-current={time === now.getTime() ? "date" : undefined}
                            data-outside={date.getMonth() !== month.getMonth() || undefined}
                            data-today={time === now.getTime() || undefined}
                            disabled={outOfRange(date)}
                            tabIndex={time === focused.getTime() ? 0 : -1}
                            onClick={() => onPick(date)}
                        >
                            {date.getDate()}
                        </button>
                    );
                })}
            </div>
            <div className={styles.footer}>
                <Button variant="ghost" size="sm" disabled={outOfRange(now)} onClick={() => onPick(now)}>
                    {t`Сьогодні`}
                </Button>
                <Button variant="ghost" size="sm" onClick={() => onPick(null)}>
                    {t`Очистити`}
                </Button>
            </div>
        </div>
    );
}
