import { useLingui } from "@lingui/react/macro";
import { ArrowLeft, ArrowRight, ChevronRight, LifeBuoy, SearchX } from "lucide-react";
import { createElement, Suspense, useEffect, useRef, useState, type RefObject } from "react";
import { Link, useLocation, useParams } from "react-router";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { SkeletonText } from "@/shared/ui/Skeleton";
import { ButtonLink } from "@/shared/ui/Button";
import { hasPage, lazyPage } from "../content/pages";
import { findEntry, neighboursOf, topicPath, type HelpEntry } from "../contents";
import { Access } from "../components/Manual";
import manual from "../components/Manual.module.css";
import { headingId } from "../search";
import styles from "./HelpTopicPage.module.css";

/**
 * The content of a page (suspends until its module is loaded).
 * @param props The language and the topic.
 * @returns The content.
 */
function PageContent({ language, id }: { readonly language: string; readonly id: string }) {
    const page = lazyPage(language, id);
    return page ? createElement(page) : null;
}

/**
 * A subheading of the page for "On this page".
 */
interface Heading {
    /** The anchor. */
    readonly id: string;
    /** The text. */
    readonly text: string;
}

/**
 * Runs once the page content is in place: gives the subheadings their anchors, reports them and scrolls to the
 * anchor of the address (a search result or a link to a section).
 * @param props The content element, the page and where to report its headings (a state setter, so it never changes).
 * @returns Nothing visible.
 */
function ContentReady({
    container,
    page,
    onHeadings,
}: {
    readonly container: RefObject<HTMLDivElement | null>;
    readonly page: string;
    readonly onHeadings: (headings: { readonly page: string; readonly items: Heading[] }) => void;
}) {
    const location = useLocation();

    // A passive effect: a layout effect of this child would run before React attaches the ref of the content element
    // around it (a page opened again renders in one commit), and would find no headings.
    useEffect(() => {
        const element = container.current;
        if (!element) {
            return;
        }

        const used = new Set<string>();
        const headings = [...element.querySelectorAll("h2")].map((heading) => {
            let id = headingId(heading.textContent);
            for (let n = 2; used.has(id); n++) {
                id = `${headingId(heading.textContent)}-${n}`;
            }

            used.add(id);
            heading.id = id;
            return { id, text: heading.textContent };
        });
        onHeadings({ page, items: headings });

        const target = decodeURIComponent(location.hash.slice(1));
        if (target) {
            document.getElementById(target)?.scrollIntoView({ block: "start" });
        } else {
            window.scrollTo({ top: 0 });
        }
    }, [container, page, onHeadings, location.hash, location.pathname]);

    return null;
}

/**
 * "On this page": the subheadings of the page, the one being read marked.
 * @param props The headings.
 * @returns The list, or nothing for a page without subheadings.
 */
function OnThisPage({ headings }: { readonly headings: readonly Heading[] }) {
    const { t } = useLingui();
    const [current, setCurrent] = useState<string | null>(null);

    // The heading being read is the last one above the reading line (a little below the sticky header).
    useEffect(() => {
        if (headings.length === 0) {
            return;
        }

        function update(): void {
            const line = 120;
            let reading: string | null = null;
            for (const heading of headings) {
                const element = document.getElementById(heading.id);
                if (element && element.getBoundingClientRect().top <= line) {
                    reading = heading.id;
                }
            }

            setCurrent(reading);
        }

        update();
        window.addEventListener("scroll", update, { passive: true });
        return () => window.removeEventListener("scroll", update);
    }, [headings]);

    if (headings.length === 0) {
        return null;
    }

    return (
        <nav className={styles.toc} aria-label={t`На цій сторінці`}>
            <p className={styles.tocTitle}>{t`На цій сторінці`}</p>
            <ul>
                {headings.map((heading) => (
                    <li key={heading.id}>
                        <a href={`#${heading.id}`} aria-current={heading.id === current ? "location" : undefined}>
                            {heading.text}
                        </a>
                    </li>
                ))}
            </ul>
        </nav>
    );
}

/**
 * A link to the previous or the next page.
 * @param props The page and the direction.
 * @returns The link.
 */
function Neighbour({ entry, direction }: { readonly entry: HelpEntry; readonly direction: "previous" | "next" }) {
    const { t } = useLingui();
    return (
        <Link to={topicPath(entry.topic.id)} className={styles.neighbour} data-direction={direction}>
            <span className={styles.neighbourLabel}>
                {direction === "previous" && <Icon icon={ArrowLeft} size="sm" />}
                {direction === "previous" ? t`Попередня сторінка` : t`Наступна сторінка`}
                {direction === "next" && <Icon icon={ArrowRight} size="sm" />}
            </span>
            <strong>{t(entry.topic.title)}</strong>
            <span className={styles.neighbourChapter}>{t(entry.chapter.title)}</span>
        </Link>
    );
}

/**
 * One page of the manual.
 * @returns The page.
 */
export function HelpTopicPage() {
    const { t, i18n } = useLingui();
    const { topicId = "" } = useParams();
    const content = useRef<HTMLDivElement>(null);
    const [headings, setHeadings] = useState<{ readonly page: string; readonly items: Heading[] }>({ page: "", items: [] });
    const entry = findEntry(topicId);
    const language = i18n.locale === "en" ? "en" : "uk";

    if (!entry || !hasPage(language, topicId)) {
        return (
            <div className={styles.missing}>
                <DocumentTitle>{t`Сторінку довідки не знайдено`}</DocumentTitle>
                <EmptyState
                    icon={SearchX}
                    title={t`Такої сторінки в довідці немає`}
                    text={t`Можливо, посилання застаріло. Скористайтеся пошуком або змістом ліворуч.`}
                >
                    <ButtonLink to="/help" variant="secondary">
                        {t`До огляду довідки`}
                    </ButtonLink>
                </EmptyState>
            </div>
        );
    }

    const { chapter, topic } = entry;
    const { previous, next } = neighboursOf(topicId);
    const permissions = topic.permissions.length > 0 ? topic.permissions : chapter.permissions;
    const pageHeadings = headings.page === `${language}/${topicId}` ? headings.items : [];

    return (
        <div className={styles.layout}>
            <article className={styles.article}>
                <DocumentTitle>{`${t(topic.title)} · ${t`Довідка`}`}</DocumentTitle>
                <nav aria-label={t`Навігаційний ланцюжок`} className={styles.breadcrumbs}>
                    <Link to="/help">{t`Довідка`}</Link>
                    <Icon icon={ChevronRight} size="sm" />
                    <span>{t(chapter.title)}</span>
                </nav>
                <header className={styles.header}>
                    <h1 className={styles.title}>{t(topic.title)}</h1>
                    <Access permissions={permissions} />
                </header>

                <div ref={content} className={manual.prose}>
                    <Suspense fallback={<SkeletonText lines={8} />}>
                        <PageContent language={language} id={topicId} />
                        <ContentReady container={content} page={`${language}/${topicId}`} onHeadings={setHeadings} />
                    </Suspense>
                </div>

                <aside className={styles.support}>
                    <Icon icon={LifeBuoy} size="lg" />
                    <div>
                        <strong>{t`Не знайшли відповіді?`}</strong>
                        <p>{t`Створіть звернення — його побачать ті, хто опрацьовує звернення, і відповідять у кабінеті.`}</p>
                    </div>
                    <ButtonLink to="/requests/new" variant="secondary" size="sm">
                        {t`Створити звернення`}
                    </ButtonLink>
                </aside>

                <nav className={styles.neighbours} aria-label={t`Сусідні сторінки`}>
                    {previous ? <Neighbour entry={previous} direction="previous" /> : <span />}
                    {next && <Neighbour entry={next} direction="next" />}
                </nav>
            </article>
            <OnThisPage key={topicId} headings={pageHeadings} />
        </div>
    );
}
