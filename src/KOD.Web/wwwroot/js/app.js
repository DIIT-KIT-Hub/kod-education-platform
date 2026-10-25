/**
 * Application-level behaviors (ES module).
 *  - Replays the page entrance animation after Blazor enhanced navigation.
 *  - Closes the mobile drawer after navigating.
 *  - Shows toasts passed through the `toast` query parameter and removes it from the address bar.
 *  - Mounts the signing UI on pages that contain [data-signing].
 *  - Mounts the user manual behaviors on the page that contains [data-help].
 *  - Shows menu item tooltips while the desktop sidebar is collapsed to an icon rail.
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
 * Lazily loads and mounts the signing UI when the page contains signing containers.
 */
async function mountSigning() {
    const containers = document.querySelectorAll("[data-signing]:not([data-signing-mounted])");
    if (containers.length === 0) {
        return;
    }

    const { mountSigningPanel } = await import("./signing/signing-ui.js");
    containers.forEach((container) => {
        container.setAttribute("data-signing-mounted", "true");
        mountSigningPanel(/** @type {HTMLElement} */ (container));
    });
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
 * Runs after the initial load and after every enhanced navigation.
 */
function onPageReady() {
    const drawer = document.getElementById("app-drawer");
    if (drawer && drawer.matches(":popover-open")) {
        drawer.hidePopover();
    }

    showQueryToast();
    mountSigning().catch((error) => console.error("Signing UI failed to load", error));
    mountHelpPage().catch((error) => console.error("Manual behaviors failed to load", error));
}

/**
 * Saves a .NET stream (DotNetStreamReference from an interactive component) as a file.
 * @param {string} fileName The file name.
 * @param {string} contentType The MIME type.
 * @param {{arrayBuffer: () => Promise<ArrayBuffer>}} streamReference The stream reference.
 * @returns {Promise<void>} Resolves when the download started.
 */
window.kodDownloadStream = async (fileName, contentType, streamReference) => {
    const buffer = await streamReference.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.append(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
};

wireRailTooltips();
onPageReady();

if (window.Blazor) {
    window.Blazor.addEventListener("enhancedload", () => {
        animatePage();
        onPageReady();
    });
}
