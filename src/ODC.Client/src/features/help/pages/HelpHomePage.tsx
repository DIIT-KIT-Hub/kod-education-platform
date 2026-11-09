import { useLingui } from "@lingui/react/macro";
import { ArrowRight, Search } from "lucide-react";
import { Link, Navigate, useLocation, useOutletContext } from "react-router";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { Icon } from "@/shared/ui/Icon";
import { Access } from "../components/Manual";
import { findEntry, helpChapters, topicPath } from "../contents";
import styles from "./HelpHomePage.module.css";

/**
 * What the help layout gives its pages.
 */
interface HelpOutlet {
    /** Opens the search dialog. */
    readonly openSearch: () => void;
}

/**
 * The pages to start with.
 */
const startHere = [
    "intro-interface",
    "account-activation",
    "documents-create",
    "documents-sign",
    "chats-start",
    "requests-new",
    "faq-common",
];

/**
 * The start page of the manual: the search and a card for every chapter. The former one-page manual linked to topics
 * with an anchor (`/help#documents-list`); such an address opens that page.
 * @returns The page.
 */
export function HelpHomePage() {
    const { t } = useLingui();
    const location = useLocation();
    const openSearch = useOutletContext<HelpOutlet | undefined>()?.openSearch;
    const anchor = decodeURIComponent(location.hash.slice(1));

    if (anchor && findEntry(anchor)) {
        return <Navigate to={topicPath(anchor)} replace />;
    }

    const quick = startHere.map(findEntry).filter((entry) => entry !== undefined);

    return (
        <div className={styles.page}>
            <DocumentTitle>{t`Довідка`}</DocumentTitle>
            <section className={styles.hero} data-inverse>
                <p className={styles.eyebrow}>{t`Інструкція користувача`}</p>
                <h1 className={styles.title}>{t`Довідка Кабінету організаційних документів`}</h1>
                <p className={styles.lead}>
                    {t`Покрокові інструкції до кожного розділу кабінету: що де знаходиться, як виконати дію і що робити, якщо щось пішло не так.`}
                </p>
                <button type="button" className={styles.search} onClick={openSearch}>
                    <Icon icon={Search} />
                    <span>{t`Що Ви хочете зробити? Наприклад: підписати документ`}</span>
                    <kbd>Ctrl K</kbd>
                </button>
            </section>

            <section aria-labelledby="help-start">
                <h2 id="help-start" className={styles.sectionTitle}>
                    {t`З чого почати`}
                </h2>
                <ul className={styles.quick}>
                    {quick.map(({ chapter, topic }) => (
                        <li key={topic.id}>
                            <Link to={topicPath(topic.id)} className={styles.quickLink}>
                                <Icon icon={chapter.icon} />
                                <span>{t(topic.title)}</span>
                                <Icon icon={ArrowRight} size="sm" className={styles.arrow} />
                            </Link>
                        </li>
                    ))}
                </ul>
            </section>

            <section aria-labelledby="help-chapters">
                <h2 id="help-chapters" className={styles.sectionTitle}>
                    {t`Розділи довідки`}
                </h2>
                <ul className={styles.chapters}>
                    {helpChapters.map((chapter, index) => (
                        <li key={chapter.id} className={styles.chapter}>
                            <div className={styles.chapterHead}>
                                <span className={styles.chapterIcon}>
                                    <Icon icon={chapter.icon} size="lg" />
                                </span>
                                <span className={styles.chapterNumber}>{t`Розділ ${index + 1}`}</span>
                            </div>
                            <h3 className={styles.chapterTitle}>
                                <Link to={topicPath(chapter.topics[0]?.id ?? "")}>{t(chapter.title)}</Link>
                            </h3>
                            <p className={styles.chapterSummary}>{t(chapter.summary)}</p>
                            <Access permissions={chapter.permissions} />
                            <ul className={styles.chapterTopics}>
                                {chapter.topics.map((topic) => (
                                    <li key={topic.id}>
                                        <Link to={topicPath(topic.id)}>{t(topic.title)}</Link>
                                    </li>
                                ))}
                            </ul>
                        </li>
                    ))}
                </ul>
            </section>
        </div>
    );
}
