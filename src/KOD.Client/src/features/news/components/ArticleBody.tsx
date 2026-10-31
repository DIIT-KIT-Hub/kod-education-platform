import { useLingui } from "@lingui/react/macro";
import { ChevronLeft, ChevronRight, X } from "lucide-react";
import { useEffect, useRef, useState, type MouseEvent } from "react";
import { Icon } from "@/shared/ui/Icon";
import styles from "./ArticleBody.module.css";
import prose from "./Prose.module.css";

/**
 * An image of the text as the viewer shows it.
 */
interface ViewerImage {
    /** The main rendition (gallery tiles keep it in data-full). */
    readonly src: string;
    readonly alt: string;
}

/**
 * Props of {@link ArticleBody}.
 */
export interface ArticleBodyProps {
    /** The HTML of the article: sanitized by the server with an allow-list (images only from /media). */
    readonly html: string;
}

/**
 * The text of an article. Clicking an image of the text or a gallery opens a full-screen viewer; arrows, swipes and the
 * buttons go through all images of the article.
 * @param props The props.
 * @returns The text.
 */
export function ArticleBody({ html }: ArticleBodyProps) {
    const body = useRef<HTMLDivElement>(null);
    const [images, setImages] = useState<readonly ViewerImage[]>([]);
    const [index, setIndex] = useState<number | null>(null);

    /**
     * Opens the viewer at the clicked image.
     * @param event The click.
     */
    function open(event: MouseEvent<HTMLDivElement>): void {
        const image = event.target instanceof HTMLImageElement ? event.target : null;
        if (!image || image.closest("a") || !body.current) {
            return;
        }

        const all = Array.from(body.current.querySelectorAll("img"));
        setImages(all.map((item) => ({ src: item.getAttribute("data-full") ?? item.currentSrc ?? item.src, alt: item.alt })));
        setIndex(all.indexOf(image));
    }

    return (
        <>
            {/* The server sanitizes the HTML (IRichTextSanitizer allow-list); this is the only place it is inserted. */}
            <div ref={body} className={prose.prose} onClick={open} dangerouslySetInnerHTML={{ __html: html }} />
            {index !== null && <Lightbox images={images} index={index} onIndex={setIndex} onClose={() => setIndex(null)} />}
        </>
    );
}

/**
 * Props of {@link Lightbox}.
 */
interface LightboxProps {
    readonly images: readonly ViewerImage[];
    readonly index: number;
    readonly onIndex: (index: number) => void;
    readonly onClose: () => void;
}

/**
 * The full-screen image viewer (a modal dialog: Escape closes it, the focus stays inside).
 * @param props The props.
 * @returns The viewer.
 */
function Lightbox({ images, index, onIndex, onClose }: LightboxProps) {
    const { t } = useLingui();
    const dialog = useRef<HTMLDialogElement>(null);
    const startX = useRef(0);
    const image = images[index];
    const single = images.length < 2;
    const counterNumber = index + 1;
    const counterTotal = images.length;

    useEffect(() => {
        dialog.current?.showModal();
    }, []);

    /**
     * Shows another image (wraps around).
     * @param position The position.
     */
    const show = (position: number) => onIndex((position + images.length) % images.length);

    return (
        <dialog
            ref={dialog}
            className={styles.lightbox}
            aria-label={t`Перегляд фото`}
            onClose={onClose}
            onClick={(event) => {
                if (event.target === event.currentTarget) {
                    dialog.current?.close();
                }
            }}
            onKeyDown={(event) => {
                if (event.key === "ArrowLeft") {
                    show(index - 1);
                } else if (event.key === "ArrowRight") {
                    show(index + 1);
                }
            }}
        >
            {image && (
                <img
                    className={styles.image}
                    src={image.src}
                    alt={image.alt}
                    onPointerDown={(event) => {
                        startX.current = event.clientX;
                    }}
                    onPointerUp={(event) => {
                        const distance = event.clientX - startX.current;
                        if (Math.abs(distance) > 50) {
                            show(index + (distance < 0 ? 1 : -1));
                        }
                    }}
                />
            )}
            {!single && <span className={styles.counter}>{t`${counterNumber} / ${counterTotal}`}</span>}
            <button
                type="button"
                className={[styles.button, styles.close].join(" ")}
                aria-label={t`Закрити`}
                title={t`Закрити`}
                onClick={() => dialog.current?.close()}
            >
                <Icon icon={X} />
            </button>
            {!single && (
                <>
                    <button
                        type="button"
                        className={[styles.button, styles.previous].join(" ")}
                        aria-label={t`Попереднє фото`}
                        title={t`Попереднє фото`}
                        onClick={() => show(index - 1)}
                    >
                        <Icon icon={ChevronLeft} />
                    </button>
                    <button
                        type="button"
                        className={[styles.button, styles.next].join(" ")}
                        aria-label={t`Наступне фото`}
                        title={t`Наступне фото`}
                        onClick={() => show(index + 1)}
                    >
                        <Icon icon={ChevronRight} />
                    </button>
                </>
            )}
        </dialog>
    );
}
