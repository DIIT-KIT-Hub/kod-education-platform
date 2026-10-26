/**
 * KOD Design System behaviors.
 *
 * All behaviors use event delegation on `document`, so they keep working after Blazor
 * enhanced navigation replaces page content. Supported attributes:
 *
 *   data-dialog-open="id"   opens <dialog id="id"> as a modal
 *   data-dialog-close       closes the nearest <dialog>
 *   data-confirm="text"     on a <form>: asks for confirmation before submitting
 *   data-autosubmit         on a <select>/<input>: submits its form on change
 *   data-copy="text"        copies the text to the clipboard and shows a toast
 *   data-theme-toggle       switches between the light and dark themes
 *   data-sidebar-toggle     collapses / expands the desktop sidebar (<html data-sidebar>, remembered per browser)
 *   data-busy-on-submit     on a <form>: shows a spinner on the submit button while submitting
 *   .ds-dropzone            highlights while a file is dragged over and shows the chosen file names
 *   .ds-btn                 the hover fill grows from the point where the pointer entered and shrinks towards where it left
 *
 * Public API: `window.kodDs.toast(message, tone)`, `window.kodDs.t(text, ...args)`.
 */
(function () {
    "use strict";

    /**
     * Translates a Ukrainian interface text into the page language. The translations are loaded for non-Ukrainian pages
     * by js/i18n/<language>*.js into `window.kodI18n` (keyed by the Ukrainian text, like the server-side catalog).
     * `{0}`, `{1}`… placeholders are replaced by the arguments.
     * @param {string} text The Ukrainian text.
     * @param {...(string|number)} args The placeholder values.
     * @returns {string} The translation, or the text itself.
     */
    function t(text) {
        var catalog = /** @type {Record<string, string>|undefined} */ (window.kodI18n);
        var result = (catalog && Object.prototype.hasOwnProperty.call(catalog, text) && catalog[text]) || text;
        var args = Array.prototype.slice.call(arguments, 1);
        return args.length === 0
            ? result
            : result.replace(/\{(\d+)\}/g, function (match, index) { return index < args.length ? String(args[index]) : match; });
    }

    /**
     * Shows a short notification at the bottom of the screen.
     * @param {string} message The text.
     * @param {"default"|"danger"} [tone] The tone.
     */
    function toast(message, tone) {
        var region = document.querySelector(".ds-toast-region");
        if (!region) {
            region = document.createElement("div");
            region.className = "ds-toast-region";
            region.setAttribute("role", "status");
            region.setAttribute("aria-live", "polite");
            document.body.appendChild(region);
        }

        var item = document.createElement("div");
        item.className = "ds-toast" + (tone === "danger" ? " ds-toast--danger" : "");
        item.textContent = message;
        region.appendChild(item);

        window.setTimeout(function () {
            item.setAttribute("data-leaving", "true");
            window.setTimeout(function () { item.remove(); }, 260);
        }, 3800);
    }

    /**
     * Opens a modal dialog by id.
     * @param {string} id The element id.
     * @returns {HTMLDialogElement|null} The dialog.
     */
    function openDialog(id) {
        var dialog = document.getElementById(id);
        if (dialog instanceof HTMLDialogElement && !dialog.open) {
            dialog.showModal();
            var focusable = dialog.querySelector("[autofocus], input, select, textarea, button:not([data-dialog-close])");
            if (focusable) {
                focusable.focus();
            }
        }

        return dialog instanceof HTMLDialogElement ? dialog : null;
    }

    /**
     * Lazily creates the shared confirmation dialog.
     * @returns {HTMLDialogElement} The dialog.
     */
    function confirmDialog() {
        var dialog = document.getElementById("ds-confirm");
        if (dialog) {
            return /** @type {HTMLDialogElement} */ (dialog);
        }

        dialog = document.createElement("dialog");
        dialog.id = "ds-confirm";
        dialog.className = "ds-dialog";
        dialog.innerHTML =
            '<form method="dialog" class="ds-dialog__inner">' +
            '<h2 class="ds-card__title" data-confirm-text></h2>' +
            '<p class="ds-muted"></p>' +
            '<div class="ds-dialog__actions">' +
            '<button class="ds-btn ds-btn--secondary" value="cancel"></button>' +
            '<button class="ds-btn" value="confirm" autofocus></button>' +
            "</div></form>";
        dialog.querySelector("p").textContent = t("Цю дію може бути неможливо скасувати.");
        dialog.querySelector('[value="cancel"]').textContent = t("Скасувати");
        dialog.querySelector('[value="confirm"]').textContent = t("Підтвердити");
        document.body.appendChild(dialog);
        return /** @type {HTMLDialogElement} */ (dialog);
    }

    /**
     * Applies and persists a theme.
     * @param {"light"|"dark"} theme The theme.
     */
    function setTheme(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        try {
            window.localStorage.setItem("kod-theme", theme);
        } catch (e) {
            /* Storage may be unavailable (private mode); the theme still applies for this page. */
        }
    }

    /**
     * Updates the label and the expanded state of every sidebar toggle from the current sidebar state.
     */
    function syncSidebarToggles() {
        var collapsed = document.documentElement.getAttribute("data-sidebar") === "collapsed";
        var label = collapsed ? t("Розгорнути меню") : t("Згорнути меню");
        document.querySelectorAll("[data-sidebar-toggle]").forEach(function (button) {
            button.setAttribute("aria-expanded", collapsed ? "false" : "true");
            button.setAttribute("aria-label", label);
            button.setAttribute("title", label);
        });
    }

    /**
     * Collapses or expands the desktop sidebar and optionally remembers the choice in this browser.
     * @param {boolean} collapsed Whether the sidebar is collapsed to an icon rail.
     * @param {boolean} [persist] Whether to store the choice.
     */
    function setSidebar(collapsed, persist) {
        document.documentElement.setAttribute("data-sidebar", collapsed ? "collapsed" : "expanded");
        if (persist) {
            try {
                window.localStorage.setItem("kod-sidebar", collapsed ? "collapsed" : "expanded");
            } catch (e) {
                /* Storage may be unavailable (private mode); the state still applies for this page. */
            }
        }

        syncSidebarToggles();
    }

    /**
     * Re-applies the stored sidebar state (enhanced navigation resets the <html> attributes, like the theme).
     */
    function restoreSidebar() {
        var saved = null;
        try {
            saved = window.localStorage.getItem("kod-sidebar");
        } catch (e) {
            saved = null;
        }

        setSidebar(saved === "collapsed", false);
    }

    document.addEventListener("click", function (event) {
        var target = /** @type {Element} */ (event.target);
        if (!(target instanceof Element)) {
            return;
        }

        var sidebarToggle = target.closest("[data-sidebar-toggle]");
        if (sidebarToggle) {
            setSidebar(document.documentElement.getAttribute("data-sidebar") !== "collapsed", true);
            return;
        }

        var opener = target.closest("[data-dialog-open]");
        if (opener) {
            event.preventDefault();
            openDialog(opener.getAttribute("data-dialog-open") || "");
            return;
        }

        var closer = target.closest("[data-dialog-close]");
        if (closer) {
            var dialog = closer.closest("dialog");
            if (dialog) {
                dialog.close();
            }

            return;
        }

        if (target instanceof HTMLDialogElement && target.classList.contains("ds-dialog")) {
            // A click on the backdrop closes the dialog.
            var rect = target.getBoundingClientRect();
            var inside = event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom;
            if (!inside) {
                target.close();
            }

            return;
        }

        var copy = target.closest("[data-copy]");
        if (copy) {
            var text = copy.getAttribute("data-copy") || "";
            navigator.clipboard.writeText(text).then(
                function () { toast(t("Скопійовано")); },
                function () { toast(t("Не вдалося скопіювати"), "danger"); });
            return;
        }

        var themeToggle = target.closest("[data-theme-toggle]");
        if (themeToggle) {
            setTheme(document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark");
        }
    });

    document.addEventListener("submit", function (event) {
        var form = /** @type {HTMLFormElement} */ (event.target);
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        var message = form.getAttribute("data-confirm");
        if (message && form.getAttribute("data-confirmed") !== "true") {
            event.preventDefault();
            event.stopImmediatePropagation();
            var submitter = /** @type {SubmitEvent} */ (event).submitter;
            var dialog = confirmDialog();
            dialog.querySelector("[data-confirm-text]").textContent = message;
            dialog.addEventListener("close", function onClose() {
                dialog.removeEventListener("close", onClose);
                if (dialog.returnValue === "confirm") {
                    form.setAttribute("data-confirmed", "true");
                    form.requestSubmit(submitter instanceof HTMLElement ? submitter : undefined);
                    form.removeAttribute("data-confirmed");
                }
            });
            dialog.showModal();
            return;
        }

        if (form.hasAttribute("data-busy-on-submit")) {
            var button = form.querySelector('button[type="submit"]');
            if (button) {
                button.setAttribute("data-busy", "true");
            }
        }
    }, true);

    document.addEventListener("change", function (event) {
        var target = event.target;
        if (!(target instanceof HTMLElement)) {
            return;
        }

        if (target.hasAttribute("data-autosubmit") && "form" in target && target.form) {
            /** @type {HTMLFormElement} */ (target.form).requestSubmit();
            return;
        }

        if (target instanceof HTMLInputElement && target.type === "file") {
            var zone = target.closest(".ds-dropzone");
            var label = zone ? zone.querySelector("[data-file-names]") : null;
            if (label && target.files) {
                label.textContent = Array.from(target.files).map(function (f) { return f.name; }).join(", ") || label.getAttribute("data-empty") || "";
            }
        }
    });

    /**
     * Moves the origin of a button's hover fill (--_x / --_y) to the pointer.
     * @param {Event} event A pointerenter / pointerleave event.
     */
    function trackButtonPointer(event) {
        var button = event.target;
        if (!(button instanceof HTMLElement) || !button.classList.contains("ds-btn") || !(event instanceof PointerEvent)) {
            return;
        }

        var rect = button.getBoundingClientRect();
        if (rect.width > 0 && rect.height > 0) {
            button.style.setProperty("--_x", ((event.clientX - rect.left) / rect.width * 100).toFixed(1) + "%");
            button.style.setProperty("--_y", ((event.clientY - rect.top) / rect.height * 100).toFixed(1) + "%");
        }
    }

    // pointerenter/leave do not bubble, but capturing listeners on the document still see them.
    document.addEventListener("pointerenter", trackButtonPointer, true);
    document.addEventListener("pointerleave", trackButtonPointer, true);

    ["dragenter", "dragover"].forEach(function (type) {
        document.addEventListener(type, function (event) {
            var zone = event.target instanceof Element ? event.target.closest(".ds-dropzone") : null;
            if (zone) {
                zone.setAttribute("data-dragover", "true");
            }
        });
    });

    ["dragleave", "drop"].forEach(function (type) {
        document.addEventListener(type, function (event) {
            var zone = event.target instanceof Element ? event.target.closest(".ds-dropzone") : null;
            if (zone) {
                zone.removeAttribute("data-dragover");
            }
        });
    });

    /**
     * Re-applies the stored (or system) theme. Blazor enhanced navigation synchronizes the <html> attributes with the
     * server markup, which has no theme, so the attribute is restored after every enhanced page load.
     */
    function restoreTheme() {
        var saved = null;
        try {
            saved = window.localStorage.getItem("kod-theme");
        } catch (e) {
            saved = null;
        }

        var theme = saved === "dark" || saved === "light"
            ? saved
            : (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
        document.documentElement.setAttribute("data-theme", theme);
    }

    if (window.Blazor && typeof window.Blazor.addEventListener === "function") {
        window.Blazor.addEventListener("enhancedload", function () {
            restoreTheme();
            restoreSidebar();
        });
    }

    new MutationObserver(function () {
        if (!document.documentElement.hasAttribute("data-theme")) {
            restoreTheme();
        }

        if (!document.documentElement.hasAttribute("data-sidebar")) {
            restoreSidebar();
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme", "data-sidebar"] });

    syncSidebarToggles();

    window.kodDs = { t: t, toast: toast, openDialog: openDialog, setTheme: setTheme, restoreTheme: restoreTheme, setSidebar: setSidebar };
})();
