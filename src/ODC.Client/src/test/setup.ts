import "@testing-library/jest-dom/vitest";
import { cleanup, configure } from "@testing-library/react";
import { afterAll, afterEach, vi } from "vitest";
import { activateLocale } from "@/shared/i18n/i18n";
import { clearConfirm } from "@/shared/ui/ConfirmHost";
import { clearToasts } from "@/shared/ui/Toast";
import { server } from "./server";

// Listen before any test module is imported: openapi-fetch keeps the fetch it found when the API client was created.
server.listen({ onUnhandledRequest: "error" });

// The whole suite runs files in parallel, and a page can take more than the default second to settle under that
// load: findBy*/waitFor wait up to 5 s (a passing test still finishes as soon as its element appears).
configure({ asyncUtilTimeout: 5000 });

// Ukrainian is the source language: tests read the texts as the user sees them.
await activateLocale("uk");

// happy-dom has no popover API: dialogs and menus use it.
if (!("showPopover" in HTMLElement.prototype)) {
    Object.assign(HTMLElement.prototype, {
        showPopover(this: HTMLElement) {
            this.setAttribute("data-popover-open", "");
        },
        hidePopover(this: HTMLElement) {
            this.removeAttribute("data-popover-open");
        },
        togglePopover(this: HTMLElement) {
            this.toggleAttribute("data-popover-open");
        },
    });
}

// happy-dom's dialog has no modal mode.
if (typeof HTMLDialogElement !== "undefined" && !HTMLDialogElement.prototype.showModal) {
    Object.assign(HTMLDialogElement.prototype, {
        showModal(this: HTMLDialogElement) {
            this.setAttribute("open", "");
        },
    });
}

// Scrolling into view is a no-op in tests.
Element.prototype.scrollIntoView ??= vi.fn();

afterEach(() => {
    cleanup();
    clearToasts();
    clearConfirm();
    server.resetHandlers();
    window.history.replaceState(null, "", "/");
});

afterAll(() => server.close());
