/**
 * Behaviors of the user manual page (/help):
 *  - highlights the table of contents entry of the topic being read (scroll position, one update per frame);
 *  - filters topics by a search query and highlights the matches with the CSS Custom Highlight API
 *    (the page markup is never modified, so no HTML is ever built from user input);
 *  - prints the manual.
 */

/**
 * Translates an interface text into the page language (see kodDs.t in kod-ds.js).
 * @param {string} text The Ukrainian text.
 * @param {...(string|number)} args The placeholder values.
 * @returns {string} The translation.
 */
const t = (text, ...args) => (window.kodDs ? window.kodDs.t(text, ...args) : text);

/** Name of the CSS highlight registered for search matches (see ::highlight(help-match) in app.css). */
const HIGHLIGHT = "help-match";

/**
 * Normalizes text for case- and apostrophe-insensitive matching.
 * @param {string} text The text.
 * @returns {string} The normalized text.
 */
function normalize(text) {
    return text.toLocaleLowerCase(document.documentElement.lang || "uk").replace(/[’ʼ`]/g, "'");
}

/** Distance from the top of the viewport at which a topic counts as "being read". */
const READING_LINE = 140;

/**
 * Keeps the table of contents in sync with the scroll position: the topic being read is the last one whose heading has
 * passed the reading line. Work is batched to one update per animation frame.
 * @param {HTMLElement} root The manual root.
 * @returns {() => void} Stops tracking.
 */
function trackActiveTopic(root) {
    const toc = /** @type {HTMLElement|null} */ (root.querySelector(".help-toc"));
    const links = new Map(Array.from(root.querySelectorAll(".help-toc a[href*='#']"), (a) => [a.hash.slice(1), a]));
    const topics = Array.from(root.querySelectorAll("[data-help-topic]"));
    let currentId = "";
    let scheduled = false;

    const update = () => {
        scheduled = false;
        let current = null;
        for (const topic of topics) {
            if (topic.hidden) {
                continue;
            }

            if (topic.getBoundingClientRect().top > READING_LINE) {
                break;
            }

            current = topic;
        }

        current ??= topics.find((topic) => !topic.hidden) || null;
        if (!current || current.id === currentId) {
            return;
        }

        currentId = current.id;
        const chapterId = current.closest("[data-help-chapter]")?.id;
        for (const [id, link] of links) {
            link.classList.toggle("is-active", id === currentId || id === chapterId);
            if (id === currentId) {
                link.setAttribute("aria-current", "location");
            } else {
                link.removeAttribute("aria-current");
            }
        }

        // Keep the active entry visible inside the sticky table of contents without scrolling the page.
        const link = links.get(currentId);
        if (toc && link && toc.scrollHeight > toc.clientHeight) {
            const linkBox = link.getBoundingClientRect();
            const tocBox = toc.getBoundingClientRect();
            if (linkBox.top < tocBox.top + 48 || linkBox.bottom > tocBox.bottom - 24) {
                toc.scrollTop += linkBox.top - tocBox.top - toc.clientHeight / 3;
            }
        }
    };

    const schedule = () => {
        if (!scheduled) {
            scheduled = true;
            requestAnimationFrame(update);
        }
    };

    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule, { passive: true });
    schedule();
    return () => {
        window.removeEventListener("scroll", schedule);
        window.removeEventListener("resize", schedule);
    };
}

/**
 * Wires the search box: hides topics without matches and highlights the matches.
 * @param {HTMLElement} root The manual root.
 */
function wireSearch(root) {
    const input = /** @type {HTMLInputElement|null} */ (root.querySelector("[data-help-search]"));
    const status = root.querySelector("[data-help-search-status]");
    if (!input || !status) {
        return;
    }

    const topics = Array.from(root.querySelectorAll("[data-help-topic]"));
    const chapters = Array.from(root.querySelectorAll("[data-help-chapter]"));
    const tocItems = Array.from(root.querySelectorAll(".help-toc li[data-target]"));
    const index = topics.map((topic) => ({ topic, text: normalize(topic.textContent || "") }));
    const highlights = typeof CSS !== "undefined" && "highlights" in CSS ? CSS.highlights : null;

    const apply = () => {
        const query = normalize(input.value.trim());
        const matched = new Set(query.length < 2 ? topics : index.filter((entry) => entry.text.includes(query)).map((entry) => entry.topic));

        for (const topic of topics) {
            topic.hidden = !matched.has(topic);
        }

        for (const chapter of chapters) {
            chapter.hidden = !chapter.querySelector("[data-help-topic]:not([hidden])");
        }

        for (const item of tocItems) {
            const target = document.getElementById(item.dataset.target || "");
            item.hidden = !target || target.hidden;
        }

        root.toggleAttribute("data-searching", query.length >= 2);
        window.dispatchEvent(new Event("scroll"));
        status.textContent = query.length < 2
            ? ""
            : matched.size === 0
                ? t("Нічого не знайдено. Спробуйте інше слово.")
                : t("Знайдено тем: {0}", matched.size);

        highlights?.delete(HIGHLIGHT);
        if (highlights && query.length >= 2 && matched.size > 0) {
            const ranges = [];
            for (const topic of matched) {
                const walker = document.createTreeWalker(topic, NodeFilter.SHOW_TEXT);
                for (let node = walker.nextNode(); node; node = walker.nextNode()) {
                    const text = normalize(node.nodeValue || "");
                    for (let at = text.indexOf(query); at >= 0; at = text.indexOf(query, at + query.length)) {
                        const range = new Range();
                        range.setStart(node, at);
                        range.setEnd(node, at + query.length);
                        ranges.push(range);
                    }
                }
            }

            highlights.set(HIGHLIGHT, new Highlight(...ranges));
        }
    };

    input.addEventListener("input", apply);
    input.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            input.value = "";
            apply();
        }
    });
}

/**
 * Clears the search box so every topic is visible again.
 * @param {HTMLElement} root The manual root.
 */
function clearSearch(root) {
    const input = /** @type {HTMLInputElement|null} */ (root.querySelector("[data-help-search]"));
    if (input && input.value) {
        input.value = "";
        input.dispatchEvent(new Event("input"));
    }
}

/**
 * Mounts the manual behaviors.
 * @param {HTMLElement} root The element marked with [data-help].
 */
export function mountHelp(root) {
    // The table of contents is a sticky column on desktops and a collapsed card on smaller screens.
    const toc = /** @type {HTMLDetailsElement|null} */ (root.querySelector("details.help-toc"));
    const desktop = window.matchMedia("(min-width: 1280px)");
    const syncToc = () => {
        if (toc) {
            toc.open = desktop.matches;
        }
    };
    syncToc();
    desktop.addEventListener("change", syncToc);
    // Links to topics of this page (table of contents, cross-references, heading anchors) scroll here directly; handling
    // them before Blazor's navigation interceptor (which skips default-prevented clicks) keeps the scroll smooth and exact.
    root.addEventListener("click", (event) => {
        const link = event.target instanceof Element ? event.target.closest("a[href*='#']") : null;
        if (!link || link.pathname !== window.location.pathname || event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }

        const target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
        if (!target) {
            return;
        }

        event.preventDefault();
        if (target.hidden || target.closest("[hidden]")) {
            clearSearch(root);
        }

        if (toc && !desktop.matches) {
            toc.open = false;
        }

        // The absolute address: a bare "#id" would resolve against <base href="/"> and drop "/help".
        window.history.pushState(window.history.state, "", link.href);
        target.scrollIntoView({ behavior: "smooth", block: "start" });
    });

    const stop = trackActiveTopic(root);
    wireSearch(root);
    root.querySelector("[data-help-print]")?.addEventListener("click", () => window.print());

    // Clean up after enhanced navigation replaces the page.
    if (window.Blazor) {
        const onLoad = () => {
            if (!root.isConnected) {
                stop();
                desktop.removeEventListener("change", syncToc);
                if (typeof CSS !== "undefined" && "highlights" in CSS) {
                    CSS.highlights.delete(HIGHLIGHT);
                }

                window.Blazor.removeEventListener("enhancedload", onLoad);
            }
        };
        window.Blazor.addEventListener("enhancedload", onLoad);
    }
}
