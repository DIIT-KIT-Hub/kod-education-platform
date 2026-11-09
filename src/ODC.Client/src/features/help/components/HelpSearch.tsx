import { useLingui } from "@lingui/react/macro";
import { CornerDownLeft, FileText, Search, SearchX } from "lucide-react";
import { useEffect, useLayoutEffect, useRef, useState, type ComponentType, type KeyboardEvent } from "react";
import { useNavigate } from "react-router";
import { Icon } from "@/shared/ui/Icon";
import { SkeletonText } from "@/shared/ui/Skeleton";
import { loadPages } from "../content/pages";
import { helpEntries, topicPath } from "../contents";
import { headingId, searchManual, type SearchDocument, type SearchHit } from "../search";
import styles from "./HelpSearch.module.css";

/**
 * Props of the search dialog.
 */
interface HelpSearchProps {
    /** Whether the dialog is open. */
    readonly open: boolean;
    /** Closes it. */
    readonly onClose: () => void;
}

/**
 * The elements that start a new line of text.
 */
const blocks = "p, li, h2, h3, td, th, dt, dd, summary, figcaption, div, aside, tr";

/**
 * Reads the text of a page with a space between blocks (textContent glues the last word of a paragraph to the first
 * word of the next one).
 * @param element The page.
 * @returns The text.
 */
export function readableText(element: HTMLElement): string {
    const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
    let text = "";
    let block: Element | null = null;
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
        const parent = node.parentElement?.closest(blocks) ?? null;
        text += (parent !== block ? " " : "") + (node.nodeValue ?? "");
        block = parent;
    }

    return text.replace(/\s+/gu, " ").trim();
}

/**
 * Renders every page once, hidden, and reads its text into the search index (one source for the pages and the search).
 * @param props The pages and where to put the index.
 * @returns The hidden pages.
 */
function Indexer({
    pages,
    locale,
    onIndexed,
}: {
    readonly pages: ReadonlyMap<string, ComponentType>;
    readonly locale: string;
    readonly onIndexed: (index: { readonly locale: string; readonly documents: SearchDocument[] }) => void;
}) {
    const { t } = useLingui();
    const root = useRef<HTMLDivElement>(null);

    useLayoutEffect(() => {
        const documents = helpEntries.flatMap(({ chapter, topic }) => {
            const element = root.current?.querySelector<HTMLElement>(`[data-search-page="${topic.id}"]`);
            if (!element) {
                return [];
            }

            const headings = [...element.querySelectorAll("h2")].map((heading) => ({
                id: headingId(heading.textContent),
                text: heading.textContent,
            }));
            return [
                {
                    id: topic.id,
                    chapter: t(chapter.title),
                    title: t(topic.title),
                    headings,
                    text: readableText(element),
                },
            ];
        });
        onIndexed({ locale, documents });
    }, [t, locale, onIndexed]);

    return (
        <div ref={root} hidden>
            {[...pages].map(([id, Content]) => (
                <div key={id} data-search-page={id}>
                    <Content />
                </div>
            ))}
        </div>
    );
}

/**
 * The search of the manual (opened with the header button, Ctrl + K or "/"): every word of the query must appear on a
 * page; titles count most. Arrows move through the results, Enter opens one, Escape closes.
 * @param props Whether it is open.
 * @returns The dialog.
 */
export function HelpSearch({ open, onClose }: HelpSearchProps) {
    const { t, i18n } = useLingui();
    const navigate = useNavigate();
    const dialog = useRef<HTMLDialogElement>(null);
    const input = useRef<HTMLInputElement>(null);
    const [query, setQuery] = useState("");
    const [active, setActive] = useState(0);
    const [pages, setPages] = useState<{ readonly locale: string; readonly pages: ReadonlyMap<string, ComponentType> } | null>(null);
    const [index, setIndex] = useState<{ readonly locale: string; readonly documents: SearchDocument[] } | null>(null);
    const locale = i18n.locale;
    const ready = index?.locale === locale;
    const hits: SearchHit[] = ready ? searchManual(index.documents, query, locale) : [];
    const selected = Math.min(active, Math.max(0, hits.length - 1));

    useEffect(() => {
        const element = dialog.current;
        if (open && element && !element.open) {
            element.showModal();
            input.current?.focus();
        } else if (!open && element?.open) {
            element.close();
        }
    }, [open]);

    // The pages load the first time the search opens (and again in another language).
    useEffect(() => {
        if (!open || pages?.locale === locale) {
            return;
        }

        let cancelled = false;
        void loadPages(
            locale,
            helpEntries.map((entry) => entry.topic.id),
        ).then((loaded) => {
            if (!cancelled) {
                setPages({ locale, pages: loaded });
            }
        });
        return () => {
            cancelled = true;
        };
    }, [open, locale, pages?.locale]);

    /**
     * Opens a result.
     * @param hit The result.
     */
    function choose(hit: SearchHit): void {
        onClose();
        setQuery("");
        void navigate(topicPath(hit.id) + (hit.heading ? `#${hit.heading.id}` : ""));
    }

    /**
     * Moves through the results with the keyboard.
     * @param event The key.
     */
    function onKeyDown(event: KeyboardEvent<HTMLInputElement>): void {
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            const step = event.key === "ArrowDown" ? 1 : -1;
            setActive((selected + step + hits.length) % Math.max(1, hits.length));
        } else if (event.key === "Enter" && hits[selected]) {
            event.preventDefault();
            choose(hits[selected]);
        }
    }

    return (
        <dialog
            ref={dialog}
            className={styles.dialog}
            aria-label={t`Пошук у довідці`}
            onCancel={(event) => {
                event.preventDefault();
                onClose();
            }}
            onClick={(event) => {
                if (event.target === event.currentTarget) {
                    onClose();
                }
            }}
        >
            <div className={styles.panel}>
                <label className={styles.field}>
                    <Icon icon={Search} />
                    <span className={styles.label}>{t`Пошук у довідці`}</span>
                    <input
                        ref={input}
                        type="search"
                        value={query}
                        placeholder={t`Наприклад: підписати документ`}
                        onChange={(event) => {
                            setQuery(event.target.value);
                            setActive(0);
                        }}
                        onKeyDown={onKeyDown}
                        role="combobox"
                        aria-expanded={hits.length > 0}
                        aria-controls="help-search-results"
                        aria-activedescendant={hits[selected] ? `help-hit-${hits[selected].id}` : undefined}
                        autoComplete="off"
                        spellCheck={false}
                    />
                    <kbd className={styles.key}>Esc</kbd>
                </label>

                <div className={styles.results}>
                    {open && !ready && query.trim().length >= 2 && <SkeletonText lines={4} />}
                    {ready && query.trim().length >= 2 && hits.length === 0 && (
                        <p className={styles.empty}>
                            <Icon icon={SearchX} size="lg" />
                            {t`Нічого не знайдено. Спробуйте інші слова або коротший запит.`}
                        </p>
                    )}
                    {query.trim().length < 2 && (
                        <p
                            className={styles.hint}
                        >{t`Введіть щонайменше два символи. Шукаємо в назвах, підзаголовках і тексті всіх сторінок.`}</p>
                    )}
                    {hits.length > 0 && (
                        <ul id="help-search-results" role="listbox" className={styles.list} aria-label={t`Результати пошуку`}>
                            {hits.map((hit, position) => (
                                <li
                                    key={hit.id}
                                    id={`help-hit-${hit.id}`}
                                    role="option"
                                    aria-selected={position === selected}
                                    className={styles.hit}
                                    onMouseEnter={() => setActive(position)}
                                    onClick={() => choose(hit)}
                                >
                                    <Icon icon={FileText} />
                                    <span className={styles.hitText}>
                                        <span className={styles.hitPath}>
                                            {hit.chapter}
                                            {hit.heading && ` › ${hit.heading.text}`}
                                        </span>
                                        <strong className={styles.hitTitle}>{hit.title}</strong>
                                        {hit.snippet && (
                                            <span className={styles.snippet}>
                                                {hit.snippet.before}
                                                <mark>{hit.snippet.match}</mark>
                                                {hit.snippet.after}
                                            </span>
                                        )}
                                    </span>
                                    {position === selected && <Icon icon={CornerDownLeft} className={styles.enter} />}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            </div>
            {pages?.locale === locale && !ready && <Indexer pages={pages.pages} locale={locale} onIndexed={setIndex} />}
        </dialog>
    );
}
