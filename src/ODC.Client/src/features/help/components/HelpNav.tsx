import { useLingui } from "@lingui/react/macro";
import { ChevronRight, Lock } from "lucide-react";
import { useState } from "react";
import { NavLink, useParams } from "react-router";
import { Icon } from "@/shared/ui/Icon";
import { helpChapters, topicPath, type HelpChapter } from "../contents";
import styles from "./HelpLayout.module.css";
import { useManual } from "./manualContext";

/**
 * The chapters of the manual with their pages. The chapter of the open page is unfolded; the reader may fold and
 * unfold the others. Pages of features the reader cannot use show a lock (they stay readable).
 * @returns The navigation.
 */
export function HelpNav() {
    const { t } = useLingui();
    const { topicId } = useParams();
    const { has } = useManual();
    const [toggled, setToggled] = useState<ReadonlyMap<string, boolean>>(new Map());

    /**
     * Tells whether a chapter is unfolded: the reader's choice, else open when it holds the current page.
     * @param chapter The chapter.
     * @returns Whether its pages show.
     */
    function isOpen(chapter: HelpChapter): boolean {
        return toggled.get(chapter.id) ?? chapter.topics.some((topic) => topic.id === topicId);
    }

    /**
     * Folds or unfolds a chapter.
     * @param chapter The chapter.
     */
    function toggle(chapter: HelpChapter): void {
        setToggled(new Map(toggled).set(chapter.id, !isOpen(chapter)));
    }

    return (
        <nav className={styles.navInner}>
            <NavLink to="/help" end className={styles.navHome}>
                {t`Огляд довідки`}
            </NavLink>
            <ul className={styles.chapters}>
                {helpChapters.map((chapter) => {
                    const open = isOpen(chapter);
                    return (
                        <li key={chapter.id}>
                            <button
                                type="button"
                                className={styles.chapterButton}
                                aria-expanded={open}
                                aria-controls={`help-nav-${chapter.id}`}
                                onClick={() => toggle(chapter)}
                            >
                                <Icon icon={chapter.icon} />
                                <span>{t(chapter.title)}</span>
                                <Icon icon={ChevronRight} className={styles.chevron} />
                            </button>
                            <ul id={`help-nav-${chapter.id}`} className={styles.topics} hidden={!open}>
                                {chapter.topics.map((topic) => {
                                    const locked = topic.permissions.length > 0 && !topic.permissions.some(has);
                                    return (
                                        <li key={topic.id}>
                                            <NavLink to={topicPath(topic.id)} className={styles.topicLink}>
                                                <span>{t(topic.title)}</span>
                                                {locked && <Icon icon={Lock} size="sm" label={t`Потрібен дозвіл`} />}
                                            </NavLink>
                                        </li>
                                    );
                                })}
                            </ul>
                        </li>
                    );
                })}
            </ul>
        </nav>
    );
}
