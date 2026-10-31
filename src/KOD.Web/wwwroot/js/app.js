/**
 * Application-level behaviors (ES module).
 *  - Replays the page entrance animation after Blazor enhanced navigation (not after streaming updates).
 *  - Closes the mobile drawer after navigating.
 *  - Shows toasts passed through the `toast` query parameter and removes it from the address bar.
 *  - Mounts the user manual behaviors on the page that contains [data-help].
 *  - Mounts the crop preview of profile photo forms.
 *  - Shows menu item tooltips while the desktop sidebar is collapsed to an icon rail.
 *  - Shows a progress bar and dims the page while the next page loads (links and form submissions).
 */

/**
 * Replays the entrance animation of the current page container.
 */
function animatePage() {
    const page = document.querySelector(".app-page");
    if (!page) {
        return;
    }

    page.setAttribute("data-entering", "true");
    // Force a reflow so the animation restarts.
    void page.offsetWidth;
    page.removeAttribute("data-entering");
}

/**
 * Shows a toast from the `toast` query parameter once.
 */
function showQueryToast() {
    const url = new URL(window.location.href);
    const message = url.searchParams.get("toast");
    if (!message || !window.kodDs) {
        return;
    }

    window.kodDs.toast(message);
    url.searchParams.delete("toast");
    window.history.replaceState(window.history.state, "", url.pathname + url.search + url.hash);
}

/**
 * Lazily loads and mounts the user manual behaviors (table of contents, search).
 */
async function mountHelpPage() {
    const root = document.querySelector("[data-help]:not([data-help-mounted])");
    if (!root) {
        return;
    }

    root.setAttribute("data-help-mounted", "true");
    const { mountHelp } = await import("./help.js");
    mountHelp(/** @type {HTMLElement} */ (root));
}

/**
 * Lazily loads the crop preview of profile photo forms.
 */
async function mountPhotoForms() {
    const forms = document.querySelectorAll("form[data-photo-cropper]:not([data-photo-mounted])");
    if (forms.length === 0) {
        return;
    }

    const { mountPhotoCropper } = await import("./photo-cropper.js");
    forms.forEach((form) => {
        form.setAttribute("data-photo-mounted", "true");
        mountPhotoCropper(/** @type {HTMLFormElement} */ (form));
    });
}

/**
 * Shows the menu item names next to the icons while the desktop sidebar is collapsed to a rail. The tooltip is one
 * fixed element on <body>, so the scrollable sidebar never clips it; the text is set with textContent.
 */
function wireRailTooltips() {
    /** @type {HTMLElement|null} */
    let tip = null;

    const hide = () => tip?.removeAttribute("data-visible");
    const show = (/** @type {Element} */ link) => {
        if (document.documentElement.getAttribute("data-sidebar") !== "collapsed" || !window.matchMedia("(min-width: 1024px)").matches) {
            return;
        }

        if (!tip || !tip.isConnected) {
            tip = document.createElement("div");
            tip.className = "app-rail-tip";
            tip.setAttribute("aria-hidden", "true");
            document.body.append(tip);
        }

        const box = link.getBoundingClientRect();
        tip.textContent = link.getAttribute("data-label") || "";
        tip.style.left = `${box.right + 12}px`;
        tip.style.top = `${box.top + box.height / 2}px`;
        tip.setAttribute("data-visible", "");
    };

    const linkOf = (/** @type {EventTarget|null} */ target) =>
        target instanceof Element ? target.closest(".app-sidebar .app-nav__link[data-label]") : null;

    document.addEventListener("mouseover", (event) => {
        const link = linkOf(event.target);
        if (link) {
            show(link);
        } else {
            hide();
        }
    });
    document.addEventListener("focusin", (event) => {
        const link = linkOf(event.target);
        if (link) {
            show(link);
        } else {
            hide();
        }
    });
    document.addEventListener("focusout", hide);
    document.addEventListener("click", hide);
    document.addEventListener("scroll", hide, { capture: true, passive: true });
}

/**
 * Shows a thin progress bar at the top and dims the current page while the next one loads, so a slow response never
 * looks like a dead click. Starts after a short delay (fast pages show nothing) and stops when the new page arrives:
 * enhanced navigation re-syncs the <html> attributes with the server markup, and `enhancedload` / `pageshow` follow.
 */
function wireNavigationProgress() {
    const root = document.documentElement;
    let timer = 0;
    let safety = 0;

    const stop = () => {
        window.clearTimeout(timer);
        window.clearTimeout(safety);
        root.removeAttribute("data-navigating");
    };

    const start = () => {
        if (!document.querySelector(".app-progress")) {
            const bar = document.createElement("div");
            bar.className = "app-progress";
            bar.setAttribute("aria-hidden", "true");
            document.body.append(bar);
        }

        window.clearTimeout(timer);
        timer = window.setTimeout(() => root.setAttribute("data-navigating", ""), 120);
        window.clearTimeout(safety);
        safety = window.setTimeout(stop, 15_000);
    };

    document.addEventListener("click", (event) => {
        const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey
            || link.target || link.hasAttribute("download")) {
            return;
        }

        const url = new URL(link.href, window.location.href);
        const samePage = url.pathname === window.location.pathname && url.search === window.location.search;
        if (url.origin === window.location.origin && !samePage && !url.pathname.startsWith("/api/")) {
            start();
        }
    });

    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (form instanceof HTMLFormElement && !event.defaultPrevented && form.method !== "dialog") {
            start();
        }
    });

    window.addEventListener("pageshow", stop);
    window.Blazor?.addEventListener("enhancedload", stop);
}

/**
 * Runs after the initial load and after every enhanced navigation.
 */
function onPageReady() {
    const drawer = document.getElementById("app-drawer");
    if (drawer && drawer.matches(":popover-open")) {
        drawer.hidePopover();
    }

    showQueryToast();
    mountHelpPage().catch((error) => console.error("Manual behaviors failed to load", error));
    mountPhotoForms().catch((error) => console.error("Photo cropper failed to load", error));
}

wireRailTooltips();
wireNavigationProgress();
onPageReady();

if (window.Blazor) {
    // `enhancedload` also fires for every streaming update (the content replacing a skeleton), including on the first
    // full page load. Replaying the entrance then would blink the whole page away and back once the data arrives, so
    // the entrance plays only for the first update of an enhanced navigation.
    let entering = false;
    window.Blazor.addEventListener("enhancednavigationstart", () => {
        entering = true;
    });
    window.Blazor.addEventListener("enhancedload", () => {
        if (entering) {
            entering = false;
            animatePage();
        }

        onPageReady();
    });
}
