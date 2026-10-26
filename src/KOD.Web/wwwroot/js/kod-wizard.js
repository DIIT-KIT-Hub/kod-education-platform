/**
 * KOD Design System multi-step wizard (ES module).
 *
 * A small state machine that shows one step at a time with a stepper header and Back / Next / Finish actions.
 * Design rules:
 *  - transitions are guarded: "Next" and the stepper only move forward when every earlier step reports
 *    `isComplete()`; the guard is checked again on every move, not only through the disabled state of a button;
 *  - step bodies are built once by the caller and only hidden, so entered values survive going back and forth;
 *  - while the wizard is busy (e.g. the finish action runs) navigation is locked, which also prevents double submits;
 *  - focus moves to the heading of the new step and a live region announces it (keyboard and screen reader users);
 *  - all text is set with textContent (no innerHTML); the wizard's own texts follow the page language (kodDs.t).
 *
 * Usage:
 *   const wizard = createWizard({
 *       label: "Signing",
 *       steps: [{ id: "data", title: "Data", body, isComplete: () => true, onEnter, onLeave }],
 *       finish: { text: "Sign", icon: "signature", run: async () => { ... } },
 *   });
 *   container.append(wizard.root);
 *   wizard.refresh(); // after the state that a step's isComplete() reads has changed
 */

/**
 * @typedef {object} WizardStep
 * @property {string} id A unique step identifier.
 * @property {string} title The step title shown in the stepper and above the body.
 * @property {string} [description] An optional line under the title.
 * @property {HTMLElement} body The step content (built once by the caller).
 * @property {() => boolean} [isComplete] Whether the user may continue past this step (defaults to true).
 * @property {(direction: "forward"|"back"|"reset") => void} [onEnter] Called after the step becomes current.
 * @property {(direction: "forward"|"back"|"reset") => void} [onLeave] Called before the step stops being current.
 */

/**
 * @typedef {object} WizardOptions
 * @property {string} label Accessible name of the wizard.
 * @property {WizardStep[]} steps The steps in order (at least one).
 * @property {{text: string, icon?: string, run: () => Promise<void>}} finish The action of the last step.
 */

/**
 * @typedef {object} Wizard
 * @property {HTMLElement} root The wizard element.
 * @property {() => number} current The index of the current step.
 * @property {() => boolean} busy Whether an action is running.
 * @property {(index: number) => boolean} go Moves to a step if the guards allow it; returns whether it moved.
 * @property {() => void} reset Returns to the first step.
 * @property {() => void} refresh Re-evaluates the guards (call after the state of a step changed).
 * @property {(value: boolean) => void} setBusy Locks or unlocks navigation.
 */

let sequence = 0;

/**
 * Translates a Ukrainian interface text into the page language (see kod-ds.js).
 * @param {string} text The Ukrainian text.
 * @param {...(string|number)} args The placeholder values.
 * @returns {string} The text.
 */
const t = (text, ...args) => (window.kodDs ? window.kodDs.t(text, ...args) : text);

/**
 * Creates an element.
 * @param {string} tag The tag name.
 * @param {string} [className] The class list.
 * @param {string} [text] The text content.
 * @returns {HTMLElement} The element.
 */
function create(tag, className, text) {
    const node = document.createElement(tag);
    if (className) {
        node.className = className;
    }

    if (text) {
        node.textContent = text;
    }

    return node;
}

/**
 * Creates an icon from the design system sprite.
 * @param {string} name The icon name.
 * @returns {SVGElement} The icon.
 */
function icon(name) {
    const ns = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(ns, "svg");
    svg.setAttribute("class", "ds-icon");
    svg.setAttribute("aria-hidden", "true");
    const use = document.createElementNS(ns, "use");
    use.setAttribute("href", `/icons/sprite.svg#${name}`);
    svg.append(use);
    return svg;
}

/**
 * Creates a button with an optional icon.
 * @param {string} className The class list.
 * @param {string} text The label.
 * @param {string} [iconName] The icon.
 * @param {boolean} [iconEnd] Whether the icon follows the label.
 * @returns {HTMLButtonElement} The button.
 */
function actionButton(className, text, iconName, iconEnd = false) {
    const node = /** @type {HTMLButtonElement} */ (create("button", className));
    node.type = "button";
    const label = create("span", "", text);
    if (iconName && !iconEnd) {
        node.append(icon(iconName));
    }

    node.append(label);
    if (iconName && iconEnd) {
        node.append(icon(iconName));
    }

    return node;
}

/**
 * Creates a multi-step wizard.
 * @param {WizardOptions} options The options.
 * @returns {Wizard} The wizard.
 */
export function createWizard(options) {
    const steps = options.steps;
    if (steps.length === 0) {
        throw new Error("A wizard needs at least one step.");
    }

    const id = `ds-wizard-${++sequence}`;
    let current = 0;
    let furthest = 0;
    let busy = false;

    const root = create("div", "ds-wizard");
    root.setAttribute("role", "group");
    root.setAttribute("aria-label", options.label);

    const stepper = create("ol", "ds-stepper");
    const counter = create("p", "ds-stepper__counter");
    const announcer = create("p", "ds-visually-hidden");
    announcer.setAttribute("aria-live", "polite");

    const markers = steps.map((step, index) => {
        const item = create("li", "ds-stepper__item");
        const trigger = /** @type {HTMLButtonElement} */ (create("button", "ds-stepper__trigger"));
        trigger.type = "button";
        trigger.setAttribute("aria-controls", `${id}-panel-${index}`);
        const marker = create("span", "ds-stepper__marker");
        marker.append(create("span", "ds-stepper__number", String(index + 1)), icon("check"));
        trigger.append(marker, create("span", "ds-stepper__title", step.title));
        trigger.addEventListener("click", () => go(index));
        item.append(trigger);
        stepper.append(item);
        return { item, trigger };
    });

    const panels = steps.map((step, index) => {
        const panel = create("section", "ds-wizard__panel");
        panel.id = `${id}-panel-${index}`;
        panel.hidden = index !== 0;
        const heading = create("h3", "ds-wizard__title", step.title);
        heading.id = `${id}-title-${index}`;
        heading.tabIndex = -1;
        panel.setAttribute("aria-labelledby", heading.id);
        const header = create("header", "ds-wizard__header");
        header.append(heading);
        if (step.description) {
            header.append(create("p", "ds-wizard__description", step.description));
        }

        panel.append(header, step.body);
        return { panel, heading };
    });

    const back = actionButton("ds-btn ds-btn--ghost", t("Назад"), "arrow-left");
    const next = actionButton("ds-btn", t("Далі"), "arrow-right", true);
    const finish = actionButton("ds-btn", options.finish.text, options.finish.icon);
    const footer = create("div", "ds-wizard__footer");
    footer.append(back, create("span", "ds-wizard__spacer"), next, finish);

    root.append(stepper, counter, announcer, ...panels.map((p) => p.panel), footer);

    /**
     * Whether a step lets the user continue.
     * @param {number} index The step index.
     * @returns {boolean} The result.
     */
    function complete(index) {
        const check = steps[index].isComplete;
        return check ? Boolean(check()) : true;
    }

    /**
     * The furthest step the guards allow: the first incomplete step (or the last step).
     * @returns {number} The index.
     */
    function reachable() {
        for (let index = 0; index < steps.length - 1; index++) {
            if (!complete(index)) {
                return index;
            }
        }

        return steps.length - 1;
    }

    /** Re-renders the stepper and the actions from the current state. */
    function refresh() {
        // The stepper opens only steps the user has already visited; new steps are reached with "Next".
        const open = Math.min(furthest, reachable());
        markers.forEach(({ item, trigger }, index) => {
            const state = index === current ? "current" : index < current || (index <= open && complete(index)) ? "done" : "upcoming";
            item.dataset.state = state;
            trigger.disabled = busy || index === current || index > open;
            if (index === current) {
                trigger.setAttribute("aria-current", "step");
            } else {
                trigger.removeAttribute("aria-current");
            }
        });

        counter.textContent = t("Крок {0} з {1} · {2}", current + 1, steps.length, steps[current].title);

        const last = current === steps.length - 1;
        back.hidden = current === 0;
        back.disabled = busy;
        next.hidden = last;
        next.disabled = busy || !complete(current);
        finish.hidden = !last;
        finish.disabled = busy || !complete(current);
        root.toggleAttribute("aria-busy", busy);
    }

    /**
     * Moves to a step if every earlier step is complete.
     * @param {number} index The target step.
     * @param {"forward"|"back"|"reset"} [reason] Why the step changes (derived from the indices when omitted).
     * @returns {boolean} Whether the wizard moved.
     */
    function go(index, reason) {
        // Forward moves go one new step at a time; jumps reach only steps that were visited before.
        if (busy || index === current || index < 0 || index >= steps.length || index > reachable() || index > Math.max(furthest, current + 1)) {
            return false;
        }

        const direction = reason || (index > current ? "forward" : "back");
        steps[current].onLeave?.(direction);
        panels[current].panel.hidden = true;
        current = index;
        furthest = direction === "reset" ? 0 : Math.max(furthest, current);

        const { panel, heading } = panels[current];
        panel.dataset.direction = direction === "back" ? "back" : "forward";
        panel.hidden = false;
        refresh();
        steps[current].onEnter?.(direction);
        announcer.textContent = t("Крок {0} з {1}: {2}", current + 1, steps.length, steps[current].title);
        heading.focus({ preventScroll: true });
        root.scrollIntoView({ block: "nearest", behavior: "smooth" });
        return true;
    }

    back.addEventListener("click", () => go(current - 1));
    next.addEventListener("click", () => go(current + 1));
    finish.addEventListener("click", async () => {
        if (busy || current !== steps.length - 1 || reachable() !== current || !complete(current)) {
            return;
        }

        setBusy(true);
        finish.setAttribute("data-busy", "true");
        try {
            await options.finish.run();
        } finally {
            finish.removeAttribute("data-busy");
            setBusy(false);
        }
    });

    /**
     * Locks or unlocks navigation.
     * @param {boolean} value Whether an action is running.
     */
    function setBusy(value) {
        busy = value;
        refresh();
    }

    refresh();
    return {
        root,
        current: () => current,
        busy: () => busy,
        go: (index) => go(index),
        reset: () => {
            if (current !== 0) {
                busy = false;
                go(0, "reset");
            }
        },
        refresh,
        setBusy,
    };
}
