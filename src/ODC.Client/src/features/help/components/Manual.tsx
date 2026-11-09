import { useLingui } from "@lingui/react/macro";
import { CircleCheck, Info, Lightbulb, Lock, TriangleAlert, Users, X, ZoomIn, type LucideIcon } from "lucide-react";
import { useRef, type ReactNode } from "react";
import { Link } from "react-router";
import { Badge } from "@/shared/ui/Badge";
import { Icon } from "@/shared/ui/Icon";
import { topicPath } from "../contents";
import styles from "./Manual.module.css";
import { useManual } from "./manualContext";

/**
 * Props of the content building blocks.
 */
interface ContentProps {
    /** The content. */
    readonly children?: ReactNode;
}

/**
 * Props of the access badge.
 */
interface AccessProps {
    /** Permissions of which one is needed; empty for everyone. */
    readonly permissions: readonly string[];
}

/**
 * Tells who may use a feature: everyone, the reader (they hold one of the permissions) or the permissions they lack.
 * @param props The permissions.
 * @returns The badge.
 */
export function Access({ permissions }: AccessProps) {
    const { t } = useLingui();
    const { permissionTitles, has } = useManual();

    if (permissions.length === 0) {
        return (
            <Badge tone="dashed" icon={Users} className={styles.badge}>
                {t`Для всіх користувачів`}
            </Badge>
        );
    }

    if (permissions.some(has)) {
        return (
            <Badge tone="inverse" icon={CircleCheck} className={styles.badge}>
                {t`Доступно Вам`}
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
            <Badge tone="outline" icon={Lock} className={styles.badge}>
                {t`Потрібен дозвіл: ${required}`}
            </Badge>
        </span>
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
 * A key or a key combination (Ctrl + K).
 * @param props The key.
 * @returns The key.
 */
export function Kbd({ children }: ContentProps) {
    return <kbd className={styles.kbd}>{children}</kbd>;
}

/**
 * A value the reader types or sees literally (an address, a file name).
 * @param props The value.
 * @returns The value.
 */
export function Code({ children }: ContentProps) {
    return <code className={styles.code}>{children}</code>;
}

/**
 * A link to another page of the manual.
 * @param props The topic and the link text.
 * @returns The link.
 */
export function Ref({ to, children }: ContentProps & { readonly to: string }) {
    return <Link to={topicPath(to)}>{children}</Link>;
}

/**
 * Numbered step-by-step instructions.
 * @param props The steps (list items).
 * @returns The list.
 */
export function Steps({ children }: ContentProps) {
    return <ol className={styles.steps}>{children}</ol>;
}

/**
 * A table that scrolls sideways on narrow screens.
 * @param props The rows.
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
 * A list of questions with folding answers (`details` elements).
 * @param props The questions.
 * @returns The list.
 */
export function Faq({ children }: ContentProps) {
    return <div className={styles.faq}>{children}</div>;
}

/**
 * Secondary text.
 * @param props The text.
 * @returns The text.
 */
export function Muted({ children }: ContentProps) {
    return <span className={styles.muted}>{children}</span>;
}

/**
 * The kinds of callouts.
 */
type CalloutKind = "tip" | "note" | "warning";

/**
 * The icon of each callout kind.
 */
const calloutIcons: Record<CalloutKind, LucideIcon> = { tip: Lightbulb, note: Info, warning: TriangleAlert };

/**
 * A highlighted remark next to the text.
 * @param props The kind, an optional title and the text.
 * @returns The callout.
 */
function Callout({ kind, title, children }: ContentProps & { readonly kind: CalloutKind; readonly title?: string }) {
    const { t } = useLingui();
    const titles: Record<CalloutKind, string> = { tip: t`Порада`, note: t`Зверніть увагу`, warning: t`Важливо` };
    return (
        <aside className={styles.callout} data-kind={kind}>
            <Icon icon={calloutIcons[kind]} />
            <div>
                <strong className={styles.calloutTitle}>{title ?? titles[kind]}</strong>
                <div className={styles.calloutBody}>{children}</div>
            </div>
        </aside>
    );
}

/**
 * A tip that saves the reader time.
 * @param props An optional title and the text.
 * @returns The callout.
 */
export function Tip({ title, children }: ContentProps & { readonly title?: string }) {
    return (
        <Callout kind="tip" title={title}>
            {children}
        </Callout>
    );
}

/**
 * Something worth noticing.
 * @param props An optional title and the text.
 * @returns The callout.
 */
export function Note({ title, children }: ContentProps & { readonly title?: string }) {
    return (
        <Callout kind="note" title={title}>
            {children}
        </Callout>
    );
}

/**
 * A warning: an action that cannot be undone or a mistake that is easy to make.
 * @param props An optional title and the text.
 * @returns The callout.
 */
export function Warning({ title, children }: ContentProps & { readonly title?: string }) {
    return (
        <Callout kind="warning" title={title}>
            {children}
        </Callout>
    );
}

/**
 * Props of a screenshot.
 */
interface FigureProps {
    /** The image (imported from `images/<language>`). */
    readonly src: string;
    /** What the image shows (for screen readers and when it cannot load). */
    readonly alt: string;
    /** The caption under the image. */
    readonly caption?: ReactNode;
}

/**
 * A screenshot of the cabinet with a caption; a click opens it in full size.
 * @param props The image, its description and the caption.
 * @returns The figure.
 */
export function Figure({ src, alt, caption }: FigureProps) {
    const { t } = useLingui();
    const viewer = useRef<HTMLDialogElement>(null);

    return (
        <figure className={styles.figure}>
            <button
                type="button"
                className={styles.figureButton}
                onClick={() => viewer.current?.showModal()}
                aria-label={t`Збільшити зображення: ${alt}`}
            >
                <img src={src} alt={alt} loading="lazy" decoding="async" className={styles.figureImage} />
                <span className={styles.figureZoom} aria-hidden="true">
                    <Icon icon={ZoomIn} />
                </span>
            </button>
            {caption && <figcaption className={styles.figureCaption}>{caption}</figcaption>}
            <dialog
                ref={viewer}
                className={styles.viewer}
                aria-label={alt}
                onClick={(event) => {
                    if (event.target === event.currentTarget || event.target instanceof HTMLImageElement) {
                        viewer.current?.close();
                    }
                }}
            >
                <button type="button" className={styles.viewerClose} onClick={() => viewer.current?.close()} aria-label={t`Закрити`}>
                    <Icon icon={X} />
                </button>
                <img src={src} alt={alt} className={styles.viewerImage} />
            </dialog>
        </figure>
    );
}
