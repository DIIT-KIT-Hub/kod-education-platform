import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, FileDown, List } from "lucide-react";
import { lazy, Suspense, useEffect, useEffectEvent, useRef, useState, type MouseEvent } from "react";
import { useLocation, useNavigate } from "react-router";
import { hasPermission, useCurrentUser } from "@/shared/session/session";
import { Button } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { helpPermissionsQuery } from "../api";
import { ManualContext } from "../components/manualContext";
import { helpChapters } from "../contents";
import styles from "./HelpPage.module.css";

/** The Ukrainian manual (loaded only in that language). */
const UkrainianManual = lazy(async () => ({ default: (await import("../chapters/uk")).Manual }));

/** The English manual (loaded only in that language). */
const EnglishManual = lazy(async () => ({ default: (await import("../chapters/en")).Manual }));

/** The name of the search highlight (CSS Custom Highlight API). */
const highlightName = "help-match";

/** Distance from the top of the window at which a topic counts as "being read". */
const readingLine = 140;

/** The width from which the table of contents is a sticky column. */
const desktopQuery = "(min-width: 1280px)";

/**
 * Normalizes text for case- and apostrophe-insensitive matching.
 * @param text The text.
 * @param locale The interface language.
 * @returns The normalized text.
 */
function normalize(text: string, locale: string): string {
    return text.toLocaleLowerCase(locale).replace(/[’ʼ`]/gu, "'");
}

/**
 * The user manual: every module with step-by-step instructions, a table of contents that follows the reading, search
 * with highlighted matches, links to topics and printing. Each chapter tells the reader whether it is available to them.
 * @returns The page.
 */
export function HelpPage() {
    const { i18n, t } = useLingui();
    const user = useCurrentUser();
    const navigate = useNavigate();
    const location = useLocation();
    const permissions = useQuery(helpPermissionsQuery);
    const root = useRef<HTMLDivElement>(null);
    const toc = useRef<HTMLDetailsElement>(null);
    const [query, setQuery] = useState("");
    const [matched, setMatched] = useState<ReadonlySet<string> | null>(null);
    const [active, setActive] = useState("");
    const [loaded, setLoaded] = useState(false);
    const locale = i18n.locale;
    const Manual = locale === "en" ? EnglishManual : UkrainianManual;
    const searching = normalize(query.trim(), locale).length >= 2;

    const permissionTitles = new Map((permissions.data ?? []).map((permission) => [permission.code, permission.title ?? permission.code]));
    const manual = { matched, permissionTitles, has: (permission: string) => hasPermission(user, permission) };

    /**
     * Searches the topics: the ones whose text contains the query stay visible.
     * @param value The query.
     */
    function search(value: string): void {
        setQuery(value);
        const text = normalize(value.trim(), locale);
        if (text.length < 2) {
            setMatched(null);
            return;
        }

        const topics = root.current?.querySelectorAll<HTMLElement>("[data-help-topic]") ?? [];
        setMatched(new Set([...topics].filter((topic) => normalize(topic.textContent, locale).includes(text)).map((topic) => topic.id)));
    }

    /**
     * Scrolls to a topic or chapter: clears a search that hides it and closes the contents on small screens.
     * @param id The anchor.
     * @param smooth Whether to animate the scroll.
     */
    function scrollToAnchor(id: string, smooth: boolean): void {
        const target = document.getElementById(id);
        if (!target) {
            return;
        }

        if (target.closest("[hidden]")) {
            search("");
        }

        if (toc.current && !window.matchMedia(desktopQuery).matches) {
            toc.current.open = false;
        }

        // A search that hid the target is cleared first: scroll once the topics are shown again.
        requestAnimationFrame(() => target.scrollIntoView({ behavior: smooth ? "smooth" : "auto", block: "start" }));
    }

    /**
     * Links to topics of this page (contents, cross-references, heading anchors) scroll here and update the address.
     * @param event The click.
     */
    function followAnchor(event: MouseEvent<HTMLDivElement>): void {
        const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>("a[href^='#']") : null;
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) {
            return;
        }

        const id = decodeURIComponent(link.hash.slice(1));
        if (!document.getElementById(id)) {
            return;
        }

        event.preventDefault();
        void navigate({ hash: id }, { preventScrollReset: true });
        scrollToAnchor(id, true);
    }

    const onManualReady = useEffectEvent(() => {
        const id = decodeURIComponent(location.hash.slice(1));
        if (id) {
            // The browser could not scroll before the chapters loaded and their real heights were known.
            requestAnimationFrame(() => requestAnimationFrame(() => document.getElementById(id)?.scrollIntoView({ block: "start" })));
        }
    });

    // Opened with an address like /help#topic: scroll there once the chapters are in place.
    useEffect(() => {
        if (loaded) {
            onManualReady();
        }
    }, [loaded]);

    // The table of contents is a sticky column on desktops and a collapsed card on smaller screens.
    useEffect(() => {
        const desktop = window.matchMedia(desktopQuery);
        const sync = () => {
            if (toc.current) {
                toc.current.open = desktop.matches;
            }
        };

        sync();
        desktop.addEventListener("change", sync);
        return () => desktop.removeEventListener("change", sync);
    }, []);

    // Topics skip layout while off screen (content-visibility: auto) and keep the last height they were rendered with:
    // render every topic once (and again after the column width changes) so anchors land exactly.
    useEffect(() => {
        const element = root.current;
        const content = element?.querySelector<HTMLElement>("[data-help-content]");
        if (!loaded || !element || !content) {
            return;
        }

        const measure = () => {
            element.setAttribute("data-help-measuring", "");
            void content.offsetHeight;
            requestAnimationFrame(() => requestAnimationFrame(() => element.removeAttribute("data-help-measuring")));
        };

        let width = content.clientWidth;
        let timer = 0;
        const observer = new ResizeObserver(() => {
            if (content.clientWidth !== width) {
                width = content.clientWidth;
                window.clearTimeout(timer);
                timer = window.setTimeout(measure, 400);
            }
        });

        measure();
        observer.observe(content);
        return () => {
            window.clearTimeout(timer);
            observer.disconnect();
        };
    }, [loaded]);

    // The contents follow the reading: the topic being read is the last one whose heading passed the reading line.
    useEffect(() => {
        if (!loaded) {
            return;
        }

        let scheduled = false;
        const update = () => {
            scheduled = false;
            const topics = [...(root.current?.querySelectorAll<HTMLElement>("[data-help-topic]:not([hidden])") ?? [])];
            let current = topics[0];
            for (const topic of topics) {
                if (topic.getBoundingClientRect().top > readingLine) {
                    break;
                }

                current = topic;
            }

            setActive(current?.id ?? "");
        };
        const schedule = () => {
            if (!scheduled) {
                scheduled = true;
                requestAnimationFrame(update);
            }
        };

        schedule();
        window.addEventListener("scroll", schedule, { passive: true });
        window.addEventListener("resize", schedule, { passive: true });
        return () => {
            window.removeEventListener("scroll", schedule);
            window.removeEventListener("resize", schedule);
        };
    }, [loaded, matched]);

    // Keeps the active entry visible inside the sticky contents without scrolling the page.
    useEffect(() => {
        const box = toc.current;
        const link = box?.querySelector<HTMLElement>(`a[href="#${CSS.escape(active)}"]`);
        if (!box || !link || box.scrollHeight <= box.clientHeight) {
            return;
        }

        const linkBox = link.getBoundingClientRect();
        const tocBox = box.getBoundingClientRect();
        if (linkBox.top < tocBox.top + 48 || linkBox.bottom > tocBox.bottom - 24) {
            box.scrollTop += linkBox.top - tocBox.top - box.clientHeight / 3;
        }
    }, [active]);

    // Highlights the matches in the visible topics (the markup is never changed).
    useEffect(() => {
        const highlights = typeof CSS !== "undefined" && "highlights" in CSS ? CSS.highlights : null;
        highlights?.delete(highlightName);
        const text = normalize(query.trim(), locale);
        if (!highlights || !matched || matched.size === 0) {
            return;
        }

        const ranges: Range[] = [];
        for (const id of matched) {
            const topic = document.getElementById(id);
            if (!topic) {
                continue;
            }

            const walker = document.createTreeWalker(topic, NodeFilter.SHOW_TEXT);
            for (let node = walker.nextNode(); node; node = walker.nextNode()) {
                const value = normalize(node.nodeValue ?? "", locale);
                for (let at = value.indexOf(text); at >= 0; at = value.indexOf(text, at + text.length)) {
                    const range = new Range();
                    range.setStart(node, at);
                    range.setEnd(node, at + text.length);
                    ranges.push(range);
                }
            }
        }

        highlights.set(highlightName, new Highlight(...ranges));
        return () => {
            highlights.delete(highlightName);
        };
    }, [matched, query, locale]);

    const activeChapter = helpChapters.find((chapter) => chapter.topics.some((topic) => topic.id === active))?.id;

    return (
        <ManualContext value={manual}>
            {/* The links themselves are focusable; the page only intercepts their clicks to scroll smoothly. */}
            <div ref={root} className={styles.page} onClick={followAnchor}>
                <PageHeader
                    title={t`Довідка`}
                    subtitle={t`Інструкція користувача Кабінету організаційних документів: усі модулі, можливості та покрокові дії.`}
                    actions={
                        <Button variant="secondary" icon={FileDown} onClick={() => window.print()} className={styles.print}>
                            {t`Друк / PDF`}
                        </Button>
                    }
                />

                <div className={styles.search} role="search">
                    <SearchInput
                        value={query}
                        placeholder={t`Пошук у довідці: наприклад, «підпис», «пароль», «імпорт»`}
                        onSearch={search}
                    />
                    <p className={styles.status} aria-live="polite">
                        {searching && matched
                            ? matched.size === 0
                                ? t`Нічого не знайдено. Спробуйте інше слово.`
                                : t`Знайдено тем: ${matched.size}`
                            : ""}
                    </p>
                </div>

                <div className={styles.layout}>
                    <details ref={toc} className={styles.toc} open>
                        <summary>
                            <Icon icon={List} />
                            {t`Зміст`}
                            <Icon icon={ChevronDown} className={styles.tocChevron} />
                        </summary>
                        <nav aria-label={t`Зміст довідки`}>
                            <ol>
                                {helpChapters.map((chapter, chapterIndex) => (
                                    <li
                                        key={chapter.id}
                                        className={styles.tocChapter}
                                        hidden={matched !== null && !chapter.topics.some((topic) => matched.has(topic.id))}
                                    >
                                        <a href={`#${chapter.id}`} data-active={chapter.id === activeChapter || undefined}>
                                            <span className={styles.tocNumber}>{chapterIndex + 1}.</span>
                                            <span>{t(chapter.title)}</span>
                                        </a>
                                        <ol>
                                            {chapter.topics.map((topic, topicIndex) => (
                                                <li key={topic.id} hidden={matched !== null && !matched.has(topic.id)}>
                                                    <a
                                                        href={`#${topic.id}`}
                                                        data-active={topic.id === active || undefined}
                                                        aria-current={topic.id === active ? "location" : undefined}
                                                    >
                                                        <span className={styles.tocNumber}>
                                                            {chapterIndex + 1}.{topicIndex + 1}
                                                        </span>
                                                        <span>{t(topic.title)}</span>
                                                    </a>
                                                </li>
                                            ))}
                                        </ol>
                                    </li>
                                ))}
                            </ol>
                        </nav>
                    </details>

                    <div className={styles.content} data-help-content>
                        <Suspense fallback={<Spinner block />}>
                            <Manual />
                            <ManualReady onReady={() => setLoaded(true)} />
                        </Suspense>
                    </div>
                </div>
            </div>
        </ManualContext>
    );
}

/**
 * Reports that the lazily loaded chapters are in the page.
 * @param props The callback.
 * @returns Nothing.
 */
function ManualReady({ onReady }: { readonly onReady: () => void }) {
    const ready = useEffectEvent(onReady);
    useEffect(() => {
        ready();
    }, []);

    return null;
}
