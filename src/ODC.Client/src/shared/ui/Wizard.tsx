import { useLingui } from "@lingui/react/macro";
import { ArrowLeft, ArrowRight, Check, type LucideIcon } from "lucide-react";
import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import layout from "@/shared/styles/layout.module.css";
import { Button } from "./Button";
import styles from "./Wizard.module.css";

/**
 * Why the current step changes.
 */
export type WizardDirection = "forward" | "back";

/**
 * A step of {@link Wizard}.
 */
export interface WizardStep {
    /** A unique identifier. */
    readonly id: string;
    /** The title in the stepper and above the content. */
    readonly title: string;
    /** A line under the title. */
    readonly description?: string;
    /** The content; every step stays mounted (hidden when not current), so entered values survive going back. */
    readonly content: ReactNode;
    /** Whether the user may continue past this step (defaults to true). */
    readonly complete?: boolean;
    /** Called after the step becomes current. */
    readonly onEnter?: (direction: WizardDirection) => void;
    /** Called before the step stops being current. */
    readonly onLeave?: (direction: WizardDirection) => void;
}

/**
 * Props of {@link Wizard}.
 */
export interface WizardProps {
    /** The accessible name. */
    readonly label: string;
    /** The steps in order (at least one). */
    readonly steps: readonly WizardStep[];
    /** The action of the last step; navigation is locked while it runs (no double submits). */
    readonly finish: { readonly text: string; readonly icon?: LucideIcon; readonly run: () => Promise<void> };
    /** Called when the running state changes (e.g. to keep a dialog from closing mid-action). */
    readonly onBusyChange?: (busy: boolean) => void;
}

/**
 * A multi-step form: a stepper, one step at a time and Back / Next / Finish. Moves are guarded: "Next" and the stepper
 * go forward only when every earlier step is complete, and the guard is checked on every move, not only through the
 * disabled state of the buttons. Focus moves to the heading of the new step and a live region announces it.
 * @param props The props.
 * @returns The wizard.
 */
export function Wizard({ label, steps, finish, onBusyChange }: WizardProps) {
    const { t } = useLingui();
    const id = useId();
    const root = useRef<HTMLDivElement>(null);
    const headings = useRef<(HTMLHeadingElement | null)[]>([]);
    const [current, setCurrent] = useState(0);
    const [furthest, setFurthest] = useState(0);
    const [direction, setDirection] = useState<WizardDirection>("forward");
    const [busy, setBusy] = useState(false);
    const [announcement, setAnnouncement] = useState("");
    const moved = useRef(false);

    const complete = (index: number) => steps[index]?.complete !== false;
    let reachable = steps.length - 1;
    for (let index = 0; index < steps.length - 1; index++) {
        if (!complete(index)) {
            reachable = index;
            break;
        }
    }

    // The stepper opens only steps the user has already visited; new steps are reached with "Next".
    const open = Math.min(furthest, reachable);
    const step = steps[current];
    const last = current === steps.length - 1;

    useEffect(() => {
        if (!moved.current) {
            return;
        }

        moved.current = false;
        headings.current[current]?.focus({ preventScroll: true });
        root.current?.scrollIntoView({ block: "nearest", behavior: "smooth" });
        steps[current]?.onEnter?.(direction);
    }, [current, direction, steps]);

    /**
     * Moves to a step if the guards allow it.
     * @param index The target step.
     */
    function go(index: number): void {
        // Forward moves go one new step at a time; jumps reach only steps that were visited before.
        if (
            busy ||
            index === current ||
            index < 0 ||
            index >= steps.length ||
            index > reachable ||
            index > Math.max(furthest, current + 1)
        ) {
            return;
        }

        const next = index > current ? "forward" : "back";
        steps[current]?.onLeave?.(next);
        moved.current = true;
        setDirection(next);
        setCurrent(index);
        setFurthest(Math.max(furthest, index));
        setAnnouncement(t`Крок ${index + 1} з ${steps.length}: ${steps[index]?.title ?? ""}`);
    }

    /**
     * Runs the finish action of the last step.
     */
    async function run(): Promise<void> {
        if (busy || !last || reachable !== current || !complete(current)) {
            return;
        }

        setBusy(true);
        onBusyChange?.(true);
        try {
            await finish.run();
        } finally {
            setBusy(false);
            onBusyChange?.(false);
        }
    }

    const counterNumber = current + 1;
    const counterTotal = steps.length;
    const counterTitle = step?.title ?? "";

    return (
        <div ref={root} className={styles.wizard} role="group" aria-label={label} aria-busy={busy || undefined}>
            <ol className={styles.stepper}>
                {steps.map((item, index) => {
                    const state =
                        index === current ? "current" : index < current || (index <= open && complete(index)) ? "done" : "upcoming";
                    return (
                        <li key={item.id} className={styles.item} data-state={state}>
                            <button
                                type="button"
                                className={styles.trigger}
                                aria-controls={`${id}-panel-${index}`}
                                aria-current={index === current ? "step" : undefined}
                                disabled={busy || index === current || index > open}
                                onClick={() => go(index)}
                            >
                                <span className={styles.marker}>
                                    <span className={styles.number}>{index + 1}</span>
                                    <Check className={styles.check} aria-hidden="true" />
                                </span>
                                <span className={styles.title}>{item.title}</span>
                            </button>
                        </li>
                    );
                })}
            </ol>
            <p className={styles.counter}>{t`Крок ${counterNumber} з ${counterTotal} · ${counterTitle}`}</p>
            <p className={layout.visuallyHidden} aria-live="polite">
                {announcement}
            </p>

            {steps.map((item, index) => (
                <section
                    key={item.id}
                    id={`${id}-panel-${index}`}
                    className={styles.panel}
                    data-direction={direction}
                    hidden={index !== current}
                    aria-labelledby={`${id}-title-${index}`}
                >
                    <header className={styles.header}>
                        <h3
                            id={`${id}-title-${index}`}
                            ref={(element) => {
                                headings.current[index] = element;
                            }}
                            className={styles.heading}
                            tabIndex={-1}
                        >
                            {item.title}
                        </h3>
                        {item.description && <p className={styles.description}>{item.description}</p>}
                    </header>
                    {item.content}
                </section>
            ))}

            <div className={styles.footer}>
                {current > 0 && (
                    <Button variant="ghost" icon={ArrowLeft} disabled={busy} onClick={() => go(current - 1)}>
                        {t`Назад`}
                    </Button>
                )}
                <span className={styles.spacer} />
                {last ? (
                    <Button icon={finish.icon} busy={busy} disabled={!complete(current)} onClick={() => void run()}>
                        {finish.text}
                    </Button>
                ) : (
                    <Button iconEnd={ArrowRight} disabled={busy || !complete(current)} onClick={() => go(current + 1)}>
                        {t`Далі`}
                    </Button>
                )}
            </div>
        </div>
    );
}
