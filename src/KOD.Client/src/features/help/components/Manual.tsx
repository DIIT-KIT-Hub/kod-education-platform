import { useLingui } from "@lingui/react/macro";
import { CircleCheck, Link2, Lock, Users } from "lucide-react";
import type { ReactNode } from "react";
import { Badge } from "@/shared/ui/Badge";
import { Icon } from "@/shared/ui/Icon";
import { chapterOf, topicOf } from "../contents";
import styles from "./Manual.module.css";
import { useManual } from "./manualContext";

/**
 * Props of the manual building blocks that wrap content.
 */
interface ContentProps {
    readonly children?: ReactNode;
}

/**
 * Props of {@link Access}.
 */
interface AccessProps {
    /** Permissions of which one is needed; empty for everyone. */
    readonly permissions: readonly string[];
    /** The badge stands on an inverse surface (the chapter header). */
    readonly onInverse?: boolean;
}

/**
 * Tells the reader whether a module or feature is available to them.
 * @param props The permissions.
 * @returns The badge.
 */
export function Access({ permissions, onInverse = false }: AccessProps) {
    const { t } = useLingui();
    const { permissionTitles, has } = useManual();
    const className = onInverse ? styles.headerBadge : styles.badge;

    if (permissions.length === 0) {
        return (
            <Badge tone="dashed" icon={Users} className={className}>
                {t`Для всіх користувачів`}
            </Badge>
        );
    }

    if (permissions.some(has)) {
        return (
            <Badge tone="inverse" icon={CircleCheck} className={onInverse ? styles.headerBadgeOn : styles.badge}>
                {t`Доступно вам`}
            </Badge>
        );
    }

    const names = permissions.map((code) => permissionTitles.get(code) ?? code);
    const quoted = names.map((name) => t`«${name}»`);
    const required =
        quoted.length === 1
            ? quoted[0]
            : quoted.length === 2
              ? t`${quoted[0]} або ${quoted[1]}`
              : t`${quoted[0]}, ${quoted[1]} або один з інших (${quoted.length - 2})`;
    return (
        <span title={names.join(", ")}>
            <Badge tone="outline" icon={Lock} className={className}>
                {t`Потрібен дозвіл: ${required}`}
            </Badge>
        </span>
    );
}

/**
 * A chapter: one module of the cabinet, with its header and access badge.
 * @param props The anchor and the topics.
 * @returns The section.
 */
export function Chapter({ id, children }: ContentProps & { readonly id: string }) {
    const { t } = useLingui();
    const { matched } = useManual();
    const { chapter, number } = chapterOf(id);
    const hidden = matched !== null && !chapter.topics.some((topic) => matched.has(topic.id));

    return (
        <section className={styles.chapter} id={id} aria-labelledby={`${id}-title`} hidden={hidden}>
            <header className={styles.chapterHeader} data-inverse>
                <span className={styles.chapterIcon}>
                    <Icon icon={chapter.icon} size="lg" />
                </span>
                <div className={styles.chapterHeading}>
                    <span className={styles.chapterNumber}>{t`Розділ ${number}`}</span>
                    <h2 id={`${id}-title`} className={styles.chapterTitle}>
                        {t(chapter.title)}
                    </h2>
                    <p className={styles.chapterSummary}>{t(chapter.summary)}</p>
                    <div>
                        <Access permissions={chapter.permissions} onInverse />
                    </div>
                </div>
            </header>
            {children}
        </section>
    );
}

/**
 * A topic: a subsection with its own anchor, a link to it and the access badge of the described feature.
 * @param props The anchor and the text.
 * @returns The section.
 */
export function Topic({ id, children }: ContentProps & { readonly id: string }) {
    const { t } = useLingui();
    const { matched } = useManual();
    const topic = topicOf(id);
    const title = t(topic.title);

    return (
        <section
            className={styles.topic}
            id={id}
            aria-labelledby={`${id}-title`}
            hidden={matched !== null && !matched.has(id)}
            data-help-topic
        >
            <h3 id={`${id}-title`} className={styles.topicTitle}>
                <a className={styles.anchor} href={`#${id}`} aria-label={t`Посилання на розділ «${title}»`}>
                    <Icon icon={Link2} />
                </a>
                <span>{title}</span>
            </h3>
            {topic.permissions.length > 0 && (
                <div>
                    <Access permissions={topic.permissions} />
                </div>
            )}
            <div className={styles.prose}>{children}</div>
        </section>
    );
}

/**
 * An interface label (button, field, menu item) written exactly as it appears on screen.
 * @param props The label.
 * @returns The label.
 */
export function Ui({ children }: ContentProps) {
    return <span className={styles.ui}>{children}</span>;
}

/**
 * A code, file name or value to type.
 * @param props The text.
 * @returns The code.
 */
export function Code({ children }: ContentProps) {
    return <code className={styles.code}>{children}</code>;
}

/**
 * A link to another topic of the manual.
 * @param props The topic anchor and the link text.
 * @returns The link.
 */
export function Ref({ to, children }: ContentProps & { readonly to: string }) {
    return <a href={`#${to}`}>{children}</a>;
}

/**
 * Numbered step-by-step instructions.
 * @param props The steps (`li`).
 * @returns The list.
 */
export function Steps({ children }: ContentProps) {
    return <ol className={styles.steps}>{children}</ol>;
}

/**
 * A reference table that scrolls sideways inside its frame on narrow screens.
 * @param props The table content (`thead`, `tbody`).
 * @returns The table.
 */
export function Table({ children }: ContentProps) {
    return (
        <div className={styles.tableWrap}>
            <table className={styles.table}>{children}</table>
        </div>
    );
}

/**
 * A list of questions (`details` with a `summary` and a `div` answer).
 * @param props The questions.
 * @returns The list.
 */
export function Faq({ children }: ContentProps) {
    return <div className={styles.faq}>{children}</div>;
}

/**
 * Muted text.
 * @param props The text.
 * @returns The text.
 */
export function Muted({ children }: ContentProps) {
    return <span className={styles.muted}>{children}</span>;
}
