/**
 * KOD Design System pickers: the drop-down of `select.ds-select` and the calendar of `[data-ds-datepicker]`.
 *
 * Both keep a native form control as the source of truth — the <select> itself, or the hidden ISO (yyyy-MM-dd) input of
 * the date picker — so they work in statically rendered forms (including `data-autosubmit`) and with Blazor bindings:
 * a choice sets the value and dispatches `input` + `change`. The browser's own drop-down / date popup is never shown.
 *
 * The floating panels are popovers in the top layer (so they also open above modal dialogs) appended to <body>, never
 * inside Blazor-rendered markup. Everything uses event delegation on `document`, so it keeps working after enhanced
 * navigation. Texts go through `kodDs.t` (js/kod-ds.js).
 */
(function () {
    "use strict";

    var SPRITE = "/icons/sprite.svg";

    /**
     * Translates an interface text.
     * @param {string} text The Ukrainian text.
     * @returns {string} The translation.
     */
    function t(text) {
        return window.kodDs ? window.kodDs.t.apply(null, arguments) : text;
    }

    /**
     * Returns the locale of the page for month and weekday names.
     * @returns {string} The BCP 47 locale.
     */
    function locale() {
        return document.documentElement.lang === "en" ? "en-GB" : "uk-UA";
    }

    /**
     * Builds an icon from the sprite.
     * @param {string} name The icon name.
     * @param {string} [className] Extra classes.
     * @returns {SVGSVGElement} The icon.
     */
    function icon(name, className) {
        var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("class", "ds-icon" + (className ? " " + className : ""));
        svg.setAttribute("aria-hidden", "true");
        var use = document.createElementNS("http://www.w3.org/2000/svg", "use");
        use.setAttribute("href", SPRITE + "#" + name);
        svg.appendChild(use);
        return svg;
    }

    var uid = 0;

    /**
     * @typedef {Object} OpenPanel
     * @property {HTMLElement} panel The floating panel.
     * @property {HTMLElement} owner The control that owns it.
     * @property {HTMLElement} anchor The element the panel is positioned against.
     * @property {function(KeyboardEvent): boolean} onKey Handles a key; returns true when consumed.
     * @property {function(): void} [onClose] Runs when the panel closes.
     */

    /** @type {OpenPanel|null} */
    var current = null;

    /**
     * Creates an empty floating panel.
     * @param {string} className The panel classes.
     * @returns {HTMLElement} The panel.
     */
    function createPanel(className) {
        var panel = document.createElement("div");
        panel.className = "ds-popover " + className;
        panel.id = "ds-popover-" + ++uid;
        panel.setAttribute("popover", "manual");
        document.body.appendChild(panel);
        return panel;
    }

    /**
     * Positions a panel under (or, without room, above) its anchor and keeps it inside the viewport.
     * @param {HTMLElement} panel The panel.
     * @param {HTMLElement} anchor The anchor.
     * @param {boolean} matchWidth Whether the panel is at least as wide as the anchor.
     */
    function place(panel, anchor, matchWidth) {
        var rect = anchor.getBoundingClientRect();
        var gap = 6;
        var margin = 8;
        if (matchWidth) {
            panel.style.minWidth = rect.width + "px";
        }

        // The visible area without the scrollbars (innerWidth/innerHeight include them).
        var viewportWidth = document.documentElement.clientWidth;
        var viewportHeight = document.documentElement.clientHeight;
        var below = viewportHeight - rect.bottom - gap - margin;
        var above = rect.top - gap - margin;
        panel.style.setProperty("--ds-popover-room", Math.max(below, above) + "px");
        var height = panel.offsetHeight;
        var width = panel.offsetWidth;
        var placeBelow = height <= below || (height > above && below >= above);
        panel.style.setProperty("--ds-popover-room", (placeBelow ? below : above) + "px");
        height = panel.offsetHeight;
        var top = placeBelow ? rect.bottom + gap : rect.top - gap - height;

        // A panel taller than either side (the calendar cannot scroll) is pushed back into view, over its control if need be.
        top = Math.max(margin, Math.min(top, viewportHeight - height - margin));
        var left = Math.max(margin, Math.min(rect.left, viewportWidth - width - margin));
        panel.setAttribute("data-placement", placeBelow ? "bottom" : "top");
        panel.style.top = top + "px";
        panel.style.left = left + "px";
    }

    /**
     * Shows a panel as the current one.
     * @param {OpenPanel} entry The panel description.
     * @param {boolean} matchWidth Whether the panel is at least as wide as the anchor.
     */
    function show(entry, matchWidth) {
        close(false);
        current = entry;
        try {
            entry.panel.showPopover();
        } catch (e) {
            /* No popover API: the panel still shows as a fixed element. */
        }

        place(entry.panel, entry.anchor, matchWidth);
        entry.owner.setAttribute("aria-expanded", "true");
        entry.panel.addEventListener("pointerdown", function (event) {
            // Keep the focus in the control while the pointer is used inside the panel.
            if (!(event.target instanceof Element) || !event.target.closest("button")) {
                event.preventDefault();
            }
        });
    }

    /**
     * Closes the current panel with a short exit animation.
     * @param {boolean} restoreFocus Whether to return the focus to the control.
     */
    function close(restoreFocus) {
        if (!current) {
            return;
        }

        var entry = current;
        current = null;
        entry.owner.setAttribute("aria-expanded", "false");
        if (entry.onClose) {
            entry.onClose();
        }

        if (restoreFocus && entry.owner.isConnected) {
            entry.owner.focus({ preventScroll: true });
        }

        entry.panel.setAttribute("data-leaving", "");
        window.setTimeout(function () {
            try {
                entry.panel.hidePopover();
            } catch (e) {
                /* Already hidden. */
            }

            entry.panel.remove();
        }, 140);
    }

    /**
     * Dispatches the events a user edit produces, so forms and Blazor bindings see the new value.
     * @param {HTMLElement} control The control.
     */
    function notify(control) {
        control.dispatchEvent(new Event("input", { bubbles: true }));
        control.dispatchEvent(new Event("change", { bubbles: true }));
    }

    /* ---------------------------------------------------------------------------------------------------------------
     * Select
     * ------------------------------------------------------------------------------------------------------------ */

    /**
     * Determines whether a <select> gets the custom drop-down.
     * @param {EventTarget|null} target The element.
     * @returns {HTMLSelectElement|null} The select.
     */
    function enhancedSelect(target) {
        return target instanceof HTMLSelectElement && target.classList.contains("ds-select") && !target.multiple && target.size <= 1 && !target.disabled
            ? target
            : null;
    }

    /**
     * Opens the drop-down of a select.
     * @param {HTMLSelectElement} select The select.
     */
    function openSelect(select) {
        var panel = createPanel("ds-listbox");
        panel.setAttribute("role", "listbox");
        var label = select.getAttribute("aria-label") || (select.labels && select.labels[0] ? select.labels[0].textContent.trim() : "");
        if (label) {
            panel.setAttribute("aria-label", label);
        }

        /** @type {{ element: HTMLElement, option: HTMLOptionElement }[]} */
        var items = [];
        var addOption = function (/** @type {HTMLOptionElement} */ option) {
            if (option.hidden) {
                return;
            }

            var item = document.createElement("div");
            item.className = "ds-listbox__option";
            item.id = panel.id + "-" + items.length;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", option.selected ? "true" : "false");
            if (option.disabled) {
                item.setAttribute("aria-disabled", "true");
            }

            var text = document.createElement("span");
            text.className = "ds-listbox__label";
            text.textContent = option.label || option.textContent;
            item.appendChild(text);
            item.appendChild(icon("check", "ds-listbox__check"));
            item.setAttribute("data-index", String(items.length));
            panel.appendChild(item);
            items.push({ element: item, option: option });
        };

        Array.prototype.forEach.call(select.children, function (child) {
            if (child instanceof HTMLOptGroupElement) {
                var group = document.createElement("div");
                group.className = "ds-listbox__group";
                group.setAttribute("role", "presentation");
                group.textContent = child.label;
                panel.appendChild(group);
                Array.prototype.forEach.call(child.children, addOption);
            } else if (child instanceof HTMLOptionElement) {
                addOption(child);
            }
        });

        var active = -1;
        var setActive = function (/** @type {number} */ index, /** @type {boolean} */ scroll) {
            if (active >= 0 && items[active]) {
                items[active].element.removeAttribute("data-active");
            }

            active = index;
            if (items[active]) {
                items[active].element.setAttribute("data-active", "");
                select.setAttribute("aria-activedescendant", items[active].element.id);
                if (scroll) {
                    items[active].element.scrollIntoView({ block: "nearest" });
                }
            }
        };

        var move = function (/** @type {number} */ from, /** @type {number} */ step) {
            for (var i = from + step; i >= 0 && i < items.length; i += step) {
                if (!items[i].option.disabled) {
                    setActive(i, true);
                    return;
                }
            }
        };

        var choose = function (/** @type {number} */ index) {
            var item = items[index];
            if (!item || item.option.disabled) {
                return;
            }

            var changed = !item.option.selected;
            item.option.selected = true;
            close(true);
            if (changed) {
                notify(select);
            }
        };

        var typed = "";
        var typedAt = 0;
        var typeahead = function (/** @type {string} */ key) {
            var now = Date.now();
            typed = (now - typedAt < 700 ? typed : "") + key.toLocaleLowerCase();
            typedAt = now;
            var start = typed.length === 1 ? active + 1 : Math.max(active, 0);
            for (var n = 0; n < items.length; n++) {
                var i = (start + n) % items.length;
                if (!items[i].option.disabled && items[i].element.textContent.trim().toLocaleLowerCase().indexOf(typed) === 0) {
                    setActive(i, true);
                    return;
                }
            }
        };

        panel.addEventListener("pointermove", function (event) {
            var item = event.target instanceof Element ? event.target.closest(".ds-listbox__option") : null;
            if (item) {
                setActive(Number(item.getAttribute("data-index")), false);
            }
        });

        panel.addEventListener("click", function (event) {
            var item = event.target instanceof Element ? event.target.closest(".ds-listbox__option") : null;
            if (item) {
                choose(Number(item.getAttribute("data-index")));
            }
        });

        show({
            panel: panel,
            owner: select,
            anchor: select,
            onClose: function () { select.removeAttribute("aria-activedescendant"); },
            onKey: function (event) {
                switch (event.key) {
                    case "ArrowDown":
                        move(active, 1);
                        return true;
                    case "ArrowUp":
                        if (event.altKey) {
                            close(true);
                        } else {
                            move(active < 0 ? items.length : active, -1);
                        }

                        return true;
                    case "Home":
                        move(-1, 1);
                        return true;
                    case "End":
                        move(items.length, -1);
                        return true;
                    case "PageDown":
                        move(Math.min(active + 7, items.length - 2), 1);
                        return true;
                    case "PageUp":
                        move(Math.max(active - 7, 1), -1);
                        return true;
                    case "Enter":
                    case " ":
                        if (event.key === " " && typed && Date.now() - typedAt < 700) {
                            typeahead(" ");
                        } else {
                            choose(active);
                        }

                        return true;
                    case "Escape":
                        close(true);
                        return true;
                    case "Tab":
                        close(false);
                        return false;
                    default:
                        if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
                            typeahead(event.key);
                            return true;
                        }

                        return false;
                }
            },
        }, true);

        select.setAttribute("aria-controls", panel.id);
        setActive(Math.max(0, items.findIndex(function (i) { return i.option.selected; })), true);
    }

    /**
     * Opens or closes the drop-down of a select.
     * @param {HTMLSelectElement} select The select.
     */
    function toggleSelect(select) {
        if (current && current.owner === select) {
            close(true);
        } else {
            select.focus({ preventScroll: true });
            openSelect(select);
        }
    }

    /* ---------------------------------------------------------------------------------------------------------------
     * Date picker
     * ------------------------------------------------------------------------------------------------------------ */

    /**
     * Pads a number to two digits.
     * @param {number} n The number.
     * @returns {string} The text.
     */
    function pad(n) {
        return (n < 10 ? "0" : "") + n;
    }

    /**
     * Formats a date as yyyy-MM-dd.
     * @param {Date} date The date.
     * @returns {string} The ISO date.
     */
    function iso(date) {
        return date.getFullYear() + "-" + pad(date.getMonth() + 1) + "-" + pad(date.getDate());
    }

    /**
     * Parses yyyy-MM-dd.
     * @param {string|null|undefined} value The ISO date.
     * @returns {Date|null} The local date.
     */
    function fromIso(value) {
        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || "");
        return match ? valid(Number(match[1]), Number(match[2]), Number(match[3])) : null;
    }

    /**
     * Builds a date when the parts form a real calendar day.
     * @param {number} year The year.
     * @param {number} month The month (1–12).
     * @param {number} day The day.
     * @returns {Date|null} The date.
     */
    function valid(year, month, day) {
        var date = new Date(year, month - 1, day);
        return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
    }

    /**
     * Formats a date with the server-supplied pattern (dd, MM, yyyy tokens; e.g. "dd.MM.yyyy", "dd/MM/yyyy").
     * @param {Date} date The date.
     * @param {string} format The pattern.
     * @returns {string} The text.
     */
    function format(date, format) {
        return format.replace(/yyyy|MM|dd|M|d/g, function (token) {
            switch (token) {
                case "yyyy": return String(date.getFullYear());
                case "MM": return pad(date.getMonth() + 1);
                case "M": return String(date.getMonth() + 1);
                case "dd": return pad(date.getDate());
                default: return String(date.getDate());
            }
        });
    }

    /**
     * Parses a typed date in the field's day/month/year order; any separator, two-digit years and ISO input are accepted.
     * @param {string} text The typed text.
     * @param {string} pattern The display pattern (its token order decides the order of the numbers).
     * @returns {Date|null} The date, or null when the text is not a date.
     */
    function parse(text, pattern) {
        var value = text.trim();
        if (/^\d{4}-\d{1,2}-\d{1,2}$/.test(value)) {
            var p = value.split("-");
            return valid(Number(p[0]), Number(p[1]), Number(p[2]));
        }

        var parts = value.split(/\D+/).filter(Boolean);
        if (parts.length === 1 && /^\d{8}$/.test(parts[0])) {
            parts = [parts[0].slice(0, 2), parts[0].slice(2, 4), parts[0].slice(4)];
        }

        if (parts.length !== 3) {
            return null;
        }

        var order = (pattern.match(/y+|M+|d+/g) || ["d", "M", "y"]).map(function (token) { return token[0]; });
        var values = { d: 0, M: 0, y: 0 };
        order.forEach(function (key, index) { values[key] = Number(parts[index]); });
        if (values.y < 100) {
            values.y += 2000;
        }

        return valid(values.y, values.M, values.d);
    }

    /**
     * Reads the parts of a date picker.
     * @param {Element} root The [data-ds-datepicker] element.
     * @returns {{ root: HTMLElement, input: HTMLInputElement, hidden: HTMLInputElement, pattern: string }} The parts.
     */
    function partsOf(root) {
        return {
            root: /** @type {HTMLElement} */ (root),
            input: /** @type {HTMLInputElement} */ (root.querySelector(".ds-datepicker__input")),
            hidden: /** @type {HTMLInputElement} */ (root.querySelector("[data-ds-date-value]")),
            pattern: root.getAttribute("data-format") || "dd.MM.yyyy",
        };
    }

    /**
     * Sets the value of a date picker (null clears it) and notifies the form when it changed.
     * @param {ReturnType<typeof partsOf>} parts The picker.
     * @param {Date|null} date The date.
     */
    function commit(parts, date) {
        var value = date ? iso(date) : "";
        parts.input.value = date ? format(date, parts.pattern) : "";
        if (parts.hidden.value !== value) {
            parts.hidden.value = value;
            notify(parts.hidden);
        }
    }

    /**
     * Applies the typed text: a valid date is normalized, an empty field clears the value, anything else is rejected.
     * @param {ReturnType<typeof partsOf>} parts The picker.
     */
    function commitTyped(parts) {
        var text = parts.input.value.trim();
        if (text === "") {
            commit(parts, null);
            return;
        }

        var date = parse(text, parts.pattern);
        if (date && !outOfRange(parts, date)) {
            commit(parts, date);
            return;
        }

        var previous = fromIso(parts.hidden.value);
        parts.input.value = previous ? format(previous, parts.pattern) : "";
        parts.input.removeAttribute("data-invalid");
        void parts.input.offsetWidth;
        parts.input.setAttribute("data-invalid", "");
        window.setTimeout(function () { parts.input.removeAttribute("data-invalid"); }, 400);
    }

    /**
     * Checks the min / max limits of the hidden input.
     * @param {ReturnType<typeof partsOf>} parts The picker.
     * @param {Date} date The date.
     * @returns {boolean} True when the date is not allowed.
     */
    function outOfRange(parts, date) {
        var value = iso(date);
        return (parts.hidden.min && value < parts.hidden.min) || (parts.hidden.max && value > parts.hidden.max) ? true : false;
    }

    /**
     * Opens the calendar of a date picker.
     * @param {Element} root The [data-ds-datepicker] element.
     * @param {boolean} focusDays Whether the keyboard focus moves into the calendar.
     */
    function openCalendar(root, focusDays) {
        var parts = partsOf(root);
        var selected = fromIso(parts.hidden.value);
        var today = new Date();
        today = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        var focused = selected || parse(parts.input.value, parts.pattern) || today;
        var month = new Date(focused.getFullYear(), focused.getMonth(), 1);
        var monthName = new Intl.DateTimeFormat(locale(), { month: "long" });
        var weekdayName = new Intl.DateTimeFormat(locale(), { weekday: "short" });
        var dayName = new Intl.DateTimeFormat(locale(), { day: "numeric", month: "long", year: "numeric" });

        var panel = createPanel("ds-calendar");
        panel.setAttribute("role", "dialog");
        panel.setAttribute("aria-label", t("Оберіть дату"));

        var header = document.createElement("div");
        header.className = "ds-calendar__header";
        var navButton = function (/** @type {string} */ name, /** @type {string} */ label, /** @type {number} */ months) {
            var button = document.createElement("button");
            button.type = "button";
            button.className = "ds-calendar__nav";
            button.setAttribute("aria-label", label);
            button.title = label;
            button.appendChild(icon(name));
            button.addEventListener("click", function () { shift(months); });
            return button;
        };

        var title = document.createElement("div");
        title.className = "ds-calendar__title";
        title.setAttribute("aria-live", "polite");
        header.appendChild(navButton("chevrons-left", t("Попередній рік"), -12));
        header.appendChild(navButton("chevron-left", t("Попередній місяць"), -1));
        header.appendChild(title);
        header.appendChild(navButton("chevron-right", t("Наступний місяць"), 1));
        header.appendChild(navButton("chevrons-right", t("Наступний рік"), 12));

        var grid = document.createElement("div");
        grid.className = "ds-calendar__grid";
        grid.setAttribute("role", "grid");

        var footer = document.createElement("div");
        footer.className = "ds-calendar__footer";
        var footerButton = function (/** @type {string} */ text, /** @type {function(): void} */ action) {
            var button = document.createElement("button");
            button.type = "button";
            button.className = "ds-btn ds-btn--ghost ds-btn--sm";
            button.textContent = text;
            button.addEventListener("click", action);
            return button;
        };

        footer.appendChild(footerButton(t("Сьогодні"), function () { pick(today); }));
        footer.appendChild(footerButton(t("Очистити"), function () {
            commit(parts, null);
            close(true);
        }));

        panel.appendChild(header);
        panel.appendChild(grid);
        panel.appendChild(footer);

        /**
         * Selects a day and closes the calendar.
         * @param {Date} date The day.
         */
        function pick(date) {
            if (outOfRange(parts, date)) {
                return;
            }

            commit(parts, date);
            close(true);
        }

        /**
         * Moves the visible month.
         * @param {number} months The number of months (negative goes back).
         */
        function shift(months) {
            focused = new Date(focused.getFullYear(), focused.getMonth() + months, Math.min(focused.getDate(), 28));
            render(months > 0 ? "next" : "prev", false);
        }

        /**
         * Draws the month of the focused day.
         * @param {string} [direction] The animation direction.
         * @param {boolean} [focusDay] Whether to move the focus to the focused day.
         */
        function render(direction, focusDay) {
            month = new Date(focused.getFullYear(), focused.getMonth(), 1);
            var name = monthName.format(month);
            title.textContent = name.charAt(0).toLocaleUpperCase() + name.slice(1) + " " + month.getFullYear();
            grid.textContent = "";
            if (direction) {
                grid.setAttribute("data-direction", direction);
                grid.style.animation = "none";
                void grid.offsetWidth;
                grid.style.animation = "";
            }

            // Weeks start on Monday (Kyiv conventions in both languages).
            var monday = new Date(2024, 0, 1);
            for (var w = 0; w < 7; w++) {
                var weekday = document.createElement("div");
                weekday.className = "ds-calendar__weekday";
                weekday.setAttribute("role", "columnheader");
                weekday.textContent = weekdayName.format(new Date(monday.getFullYear(), 0, 1 + w)).replace(".", "");
                grid.appendChild(weekday);
            }

            var offset = (month.getDay() + 6) % 7;
            var start = new Date(month.getFullYear(), month.getMonth(), 1 - offset);
            var focusTarget = null;
            for (var i = 0; i < 42; i++) {
                var day = new Date(start.getFullYear(), start.getMonth(), start.getDate() + i);
                var button = document.createElement("button");
                button.type = "button";
                button.className = "ds-calendar__day";
                button.textContent = String(day.getDate());
                button.setAttribute("role", "gridcell");
                button.setAttribute("aria-label", dayName.format(day));
                button.setAttribute("data-date", iso(day));
                button.tabIndex = -1;
                if (day.getMonth() !== month.getMonth()) {
                    button.setAttribute("data-outside", "");
                }

                if (day.getTime() === today.getTime()) {
                    button.setAttribute("data-today", "");
                    button.setAttribute("aria-current", "date");
                }

                if (selected && day.getTime() === selected.getTime()) {
                    button.setAttribute("aria-selected", "true");
                }

                if (outOfRange(parts, day)) {
                    button.disabled = true;
                }

                if (day.getTime() === focused.getTime()) {
                    button.tabIndex = 0;
                    focusTarget = button;
                }

                grid.appendChild(button);
            }

            if (focusDay && focusTarget) {
                focusTarget.focus({ preventScroll: true });
            }
        }

        grid.addEventListener("click", function (event) {
            var button = event.target instanceof Element ? event.target.closest(".ds-calendar__day") : null;
            if (button && !(/** @type {HTMLButtonElement} */ (button)).disabled) {
                pick(/** @type {Date} */ (fromIso(button.getAttribute("data-date"))));
            }
        });

        grid.addEventListener("keydown", function (event) {
            var steps = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
            var target = /** @type {Element} */ (event.target);
            if (!target.classList || !target.classList.contains("ds-calendar__day")) {
                return;
            }

            var before = focused;
            if (Object.prototype.hasOwnProperty.call(steps, event.key)) {
                focused = new Date(focused.getFullYear(), focused.getMonth(), focused.getDate() + steps[event.key]);
            } else if (event.key === "Home") {
                focused = new Date(focused.getFullYear(), focused.getMonth(), focused.getDate() - ((focused.getDay() + 6) % 7));
            } else if (event.key === "End") {
                focused = new Date(focused.getFullYear(), focused.getMonth(), focused.getDate() + (6 - ((focused.getDay() + 6) % 7)));
            } else if (event.key === "PageUp" || event.key === "PageDown") {
                var months = (event.key === "PageUp" ? -1 : 1) * (event.shiftKey ? 12 : 1);
                focused = new Date(focused.getFullYear(), focused.getMonth() + months, Math.min(focused.getDate(), 28));
            } else {
                return;
            }

            event.preventDefault();
            var sameMonth = focused.getMonth() === before.getMonth() && focused.getFullYear() === before.getFullYear();
            render(sameMonth ? undefined : focused > before ? "next" : "prev", true);
        });

        root.setAttribute("data-open", "");
        show({
            panel: panel,
            owner: parts.input,
            anchor: parts.input,
            onClose: function () { root.removeAttribute("data-open"); },
            onKey: function (event) {
                if (event.key === "Escape") {
                    close(true);
                    return true;
                }

                if (event.key === "Tab" && !panel.contains(/** @type {Node} */ (event.target)) && !event.shiftKey) {
                    // From the text field Tab enters the calendar on the focused day.
                    var day = /** @type {HTMLElement|null} */ (grid.querySelector('.ds-calendar__day[tabindex="0"]'));
                    if (day) {
                        day.focus();
                        return true;
                    }
                }

                return false;
            },
        }, false);

        // Placed again once the month is drawn: the empty panel had no height yet.
        render(undefined, focusDays);
        place(panel, parts.input, false);
    }

    /* ---------------------------------------------------------------------------------------------------------------
     * Events
     * ------------------------------------------------------------------------------------------------------------ */

    var touchStart = null;

    document.addEventListener("touchstart", function (event) {
        var touch = event.touches[0];
        touchStart = enhancedSelect(event.target) && touch ? { x: touch.clientX, y: touch.clientY } : null;
    }, { passive: true });

    document.addEventListener("touchend", function (event) {
        var select = enhancedSelect(event.target);
        var touch = event.changedTouches[0];
        if (!select || !touchStart || !touch || Math.abs(touch.clientX - touchStart.x) > 10 || Math.abs(touch.clientY - touchStart.y) > 10) {
            return;
        }

        // Prevents the synthesized mouse events, and with them the native picker of the phone.
        event.preventDefault();
        toggleSelect(select);
    });

    document.addEventListener("mousedown", function (event) {
        var select = enhancedSelect(event.target);
        if (select && event.button === 0) {
            event.preventDefault();
            toggleSelect(select);
            return;
        }

        var toggle = event.target instanceof Element ? event.target.closest(".ds-datepicker__toggle") : null;
        if (toggle) {
            event.preventDefault();
            var root = toggle.closest("[data-ds-datepicker]");
            var input = root ? root.querySelector(".ds-datepicker__input") : null;
            if (root && input) {
                if (current && current.owner === input) {
                    close(true);
                } else {
                    /** @type {HTMLElement} */ (input).focus({ preventScroll: true });
                    openCalendar(root, false);
                }
            }

            return;
        }

        var dateInput = event.target instanceof HTMLInputElement && event.target.classList.contains("ds-datepicker__input") ? event.target : null;
        if (dateInput && !(current && current.owner === dateInput)) {
            var picker = dateInput.closest("[data-ds-datepicker]");
            if (picker) {
                openCalendar(picker, false);
            }

            return;
        }

        if (current && !current.panel.contains(/** @type {Node} */ (event.target)) && event.target !== current.owner) {
            close(false);
        }
    }, true);

    document.addEventListener("keydown", function (event) {
        var target = event.target;
        if (current && (target === current.owner || current.panel.contains(/** @type {Node} */ (target)))) {
            if (current.onKey(event)) {
                event.preventDefault();
                event.stopPropagation();
                return;
            }
        }

        var select = enhancedSelect(target);
        if (select && !current && (event.key === "Enter" || event.key === " " || event.key === "F4" || (event.altKey && event.key === "ArrowDown"))) {
            event.preventDefault();
            openSelect(select);
            return;
        }

        var dateInput = target instanceof HTMLInputElement && target.classList.contains("ds-datepicker__input") ? target : null;
        if (dateInput) {
            var root = dateInput.closest("[data-ds-datepicker]");
            if (!root) {
                return;
            }

            if (event.key === "ArrowDown" && !current) {
                event.preventDefault();
                openCalendar(root, true);
            } else if (event.key === "Enter") {
                // Commit before the form submits, so the hidden value goes with it. An auto-submitting picker submits
                // the form itself on change, so the implicit submission of Enter is cancelled to avoid a second one.
                var parts = partsOf(root);
                var before = parts.hidden.value;
                commitTyped(parts);
                close(false);
                if (parts.hidden.hasAttribute("data-autosubmit") && parts.hidden.value !== before) {
                    event.preventDefault();
                }
            }
        }
    }, true);

    document.addEventListener("focusout", function (event) {
        var target = event.target;
        window.setTimeout(function () {
            var active = document.activeElement;
            if (current && (target === current.owner || current.panel.contains(/** @type {Node} */ (target)))
                && active !== current.owner && !current.panel.contains(active)) {
                close(false);
            }

            if (target instanceof HTMLInputElement && target.classList.contains("ds-datepicker__input") && active !== target) {
                var root = target.closest("[data-ds-datepicker]");
                if (root && !(current && current.owner === target)) {
                    commitTyped(partsOf(root));
                }
            }
        }, 0);
    });

    var reposition = function () {
        if (current) {
            if (!current.anchor.isConnected) {
                close(false);
            } else {
                place(current.panel, current.anchor, current.panel.classList.contains("ds-listbox"));
            }
        }
    };

    window.addEventListener("resize", reposition);
    document.addEventListener("scroll", function (event) {
        if (current && !current.panel.contains(/** @type {Node} */ (event.target))) {
            reposition();
        }
    }, { capture: true, passive: true });

    if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
        window.Blazor.addEventListener("enhancedload", function () { close(false); });
    }

    window.kodPickers = { close: function () { close(false); } };
})();
