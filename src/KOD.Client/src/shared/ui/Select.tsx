import { Check, ChevronDown } from "lucide-react";
import { useEffect, useId, useRef, useState, type KeyboardEvent, type Ref } from "react";
import { Icon } from "./Icon";
import styles from "./Select.module.css";

/**
 * An option of {@link Select}.
 */
export interface SelectOption {
    /** The value. */
    readonly value: string;
    /** The text. */
    readonly label: string;
    /** Makes the option unavailable. */
    readonly disabled?: boolean;
    /** The nesting level (units of a tree), shown as an indent. */
    readonly depth?: number;
}

/**
 * Props of {@link Select}.
 */
export interface SelectProps {
    /** The options. */
    readonly options: readonly SelectOption[];
    /** The chosen value ("" for none). */
    readonly value: string;
    /** Called with the chosen value. */
    readonly onChange: (value: string) => void;
    /** Called when the control loses focus (react-hook-form touches the field). */
    readonly onBlur?: () => void;
    /** The button element. */
    readonly ref?: Ref<HTMLButtonElement>;
    /** Shown when nothing is chosen. */
    readonly placeholder?: string;
    /** The control id (a field label points to it). */
    readonly id?: string;
    /** The accessible name when there is no label. */
    readonly "aria-label"?: string;
    /** The ids of the hint and the error. */
    readonly "aria-describedby"?: string | undefined;
    /** Whether the value is invalid. */
    readonly "aria-invalid"?: boolean | undefined;
    /** The smaller, pill-shaped look of toolbars. */
    readonly compact?: boolean;
    /** Blocks the control. */
    readonly disabled?: boolean;
}

/** Space between the control and the panel, and between the panel and the window edge. */
const gap = 6;
const margin = 8;

/**
 * A drop-down list in the design system style (the browser's own drop-down is never shown): a button that opens a
 * listbox in the top layer, so it also opens above modal dialogs. Keyboard: arrows, Home / End, Page Up / Down,
 * Enter or Space to choose, Escape to close, typing to jump to an option.
 * @param props The props.
 * @returns The select.
 */
export function Select({ options, value, onChange, onBlur, ref, placeholder, compact, disabled, id, ...aria }: SelectProps) {
    const listId = useId();
    const button = useRef<HTMLButtonElement | null>(null);
    const panel = useRef<HTMLDivElement>(null);
    const typed = useRef({ text: "", at: 0 });
    const [open, setOpen] = useState(false);
    const [active, setActive] = useState(-1);
    const chosen = options.find((option) => option.value === value);

    // Shows the panel in the top layer next to the button: below when it fits, else above; never outside the window.
    useEffect(() => {
        const list = panel.current;
        const anchor = button.current;
        if (!open || !list || !anchor) {
            return;
        }

        list.showPopover();
        const place = () => {
            const rect = anchor.getBoundingClientRect();
            const width = document.documentElement.clientWidth;
            const height = document.documentElement.clientHeight;
            const below = height - rect.bottom - gap - margin;
            const above = rect.top - gap - margin;
            list.style.minWidth = `${rect.width}px`;
            list.style.setProperty("--_room", `${Math.max(below, above)}px`);
            const placeBelow = list.offsetHeight <= below || below >= above;
            list.style.setProperty("--_room", `${placeBelow ? below : above}px`);
            const top = placeBelow ? rect.bottom + gap : rect.top - gap - list.offsetHeight;
            list.style.top = `${Math.max(margin, Math.min(top, height - list.offsetHeight - margin))}px`;
            list.style.left = `${Math.max(margin, Math.min(rect.left, width - list.offsetWidth - margin))}px`;
            list.dataset.placement = placeBelow ? "bottom" : "top";
        };

        place();
        list.querySelector("[data-active]")?.scrollIntoView({ block: "nearest" });

        // A pointer outside, a scroll of the page or a resize closes the panel (scrolling inside the list does not).
        const outside = (event: PointerEvent) => {
            if (!list.contains(event.target as Node) && !anchor.contains(event.target as Node)) {
                setOpen(false);
            }
        };
        const scroll = (event: Event) => {
            if (!list.contains(event.target as Node)) {
                setOpen(false);
            }
        };
        const resize = () => setOpen(false);
        document.addEventListener("pointerdown", outside, true);
        document.addEventListener("scroll", scroll, true);
        window.addEventListener("resize", resize);
        return () => {
            document.removeEventListener("pointerdown", outside, true);
            document.removeEventListener("scroll", scroll, true);
            window.removeEventListener("resize", resize);
            if (list.matches(":popover-open")) {
                list.hidePopover();
            }
        };
    }, [open]);

    // The active option follows the keyboard into view.
    useEffect(() => {
        if (open) {
            panel.current?.querySelector("[data-active]")?.scrollIntoView({ block: "nearest" });
        }
    }, [open, active]);

    /**
     * Opens the list with the chosen option active.
     */
    function show(): void {
        setActive(
            Math.max(
                0,
                options.findIndex((option) => option.value === value),
            ),
        );
        setOpen(true);
    }

    /**
     * Chooses an option and closes the list.
     * @param index The option index.
     */
    function choose(index: number): void {
        const option = options[index];
        if (!option || option.disabled) {
            return;
        }

        setOpen(false);
        if (option.value !== value) {
            onChange(option.value);
        }
    }

    /**
     * Moves the active option, skipping unavailable ones.
     * @param from The index to start from.
     * @param step The direction.
     */
    function move(from: number, step: 1 | -1): void {
        for (let index = from + step; index >= 0 && index < options.length; index += step) {
            if (!options[index]?.disabled) {
                setActive(index);
                return;
            }
        }
    }

    /**
     * Jumps to the next option that starts with the typed text.
     * @param key The typed character.
     */
    function typeahead(key: string): void {
        const now = Date.now();
        typed.current = { text: (now - typed.current.at < 700 ? typed.current.text : "") + key.toLocaleLowerCase(), at: now };
        const text = typed.current.text;
        const start = text.length === 1 ? active + 1 : Math.max(active, 0);
        for (let n = 0; n < options.length; n++) {
            const index = (start + n) % options.length;
            const option = options[index];
            if (option && !option.disabled && option.label.trim().toLocaleLowerCase().startsWith(text)) {
                setActive(index);
                return;
            }
        }
    }

    /**
     * Handles the keyboard of the button.
     * @param event The key event.
     */
    function keyDown(event: KeyboardEvent<HTMLButtonElement>): void {
        if (!open) {
            if (["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
                event.preventDefault();
                show();
            }

            return;
        }

        const handled = () => event.preventDefault();
        switch (event.key) {
            case "ArrowDown":
                handled();
                move(active, 1);
                break;
            case "ArrowUp":
                handled();
                if (event.altKey) {
                    setOpen(false);
                } else {
                    move(active < 0 ? options.length : active, -1);
                }

                break;
            case "Home":
                handled();
                move(-1, 1);
                break;
            case "End":
                handled();
                move(options.length, -1);
                break;
            case "PageDown":
                handled();
                move(Math.min(active + 7, options.length - 2), 1);
                break;
            case "PageUp":
                handled();
                move(Math.max(active - 7, 1), -1);
                break;
            case "Enter":
            case " ":
                handled();
                if (event.key === " " && typed.current.text && Date.now() - typed.current.at < 700) {
                    typeahead(" ");
                } else {
                    choose(active);
                }

                break;
            case "Escape":
                handled();
                setOpen(false);
                break;
            case "Tab":
                setOpen(false);
                break;
            default:
                if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                    handled();
                    typeahead(event.key);
                }
        }
    }

    return (
        <>
            <button
                {...aria}
                ref={(element) => {
                    button.current = element;
                    if (typeof ref === "function") {
                        ref(element);
                    } else if (ref) {
                        ref.current = element;
                    }
                }}
                id={id}
                type="button"
                role="combobox"
                className={[styles.select, compact && styles.compact].filter(Boolean).join(" ")}
                aria-haspopup="listbox"
                aria-expanded={open}
                aria-controls={listId}
                aria-activedescendant={open && active >= 0 ? `${listId}-${active}` : undefined}
                disabled={disabled}
                onClick={() => (open ? setOpen(false) : show())}
                onKeyDown={keyDown}
                onBlur={() => {
                    setOpen(false);
                    onBlur?.();
                }}
            >
                <span className={chosen ? styles.value : styles.placeholder}>{chosen?.label ?? placeholder ?? ""}</span>
                <Icon icon={ChevronDown} className={styles.chevron} />
            </button>
            <div ref={panel} id={listId} className={styles.panel} popover="manual" role="listbox" hidden={!open}>
                {open &&
                    options.map((option, index) => (
                        <div
                            key={option.value}
                            id={`${listId}-${index}`}
                            role="option"
                            className={styles.option}
                            style={option.depth ? { paddingLeft: `${0.75 + option.depth * 1.1}rem` } : undefined}
                            aria-selected={option.value === value}
                            aria-disabled={option.disabled || undefined}
                            data-active={index === active || undefined}
                            // Keeps the focus on the button while the pointer is used in the list.
                            onPointerDown={(event) => event.preventDefault()}
                            onPointerMove={() => setActive(index)}
                            onClick={() => choose(index)}
                        >
                            <span className={styles.label}>{option.label}</span>
                            <Icon icon={Check} className={styles.check} />
                        </div>
                    ))}
            </div>
        </>
    );
}
