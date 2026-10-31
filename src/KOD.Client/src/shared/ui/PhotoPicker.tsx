import { useLingui } from "@lingui/react/macro";
import { Camera, ZoomIn, ZoomOut } from "lucide-react";
import { useEffect, useRef, useState, type KeyboardEvent, type PointerEvent } from "react";
import { imageAccept, shrinkImage } from "@/shared/lib/image";
import { FileDrop } from "./FileDrop";
import { Icon } from "./Icon";
import styles from "./PhotoPicker.module.css";

/**
 * The visible square as fractions of the upright image (the server cuts the same part from the original file).
 */
export interface ImageCrop {
    readonly left: number;
    readonly top: number;
    readonly width: number;
    readonly height: number;
}

/**
 * The chosen photo and its square.
 */
export interface PhotoSelection {
    /** The file to upload (a smaller copy of a large photo). */
    readonly file: File;
    /** The square, or null when the browser cannot preview the format (the server then uses the centre). */
    readonly crop: ImageCrop | null;
}

/**
 * Props of {@link PhotoPicker}.
 */
export interface PhotoPickerProps {
    /** Called with the chosen photo and every change of its square. */
    readonly onChange: (selection: PhotoSelection | null) => void;
    /** Blocks the picker while the photo uploads. */
    readonly disabled?: boolean;
}

/**
 * The position of the image in the round frame.
 */
interface Placement {
    /** The offset of the image's top-left corner in the frame. */
    x: number;
    y: number;
    /** The current scale and the one that just covers the frame. */
    scale: number;
    base: number;
    /** The natural size of the image and the frame size. */
    width: number;
    height: number;
    size: number;
}

/**
 * Chooses a profile photo and its square: the photo is shown in a round frame where it can be dragged (pointer or
 * arrow keys) and zoomed (slider, wheel, + and -). Nothing is uploaded here.
 * @param props The props.
 * @returns The picker.
 */
export function PhotoPicker({ onChange, disabled }: PhotoPickerProps) {
    const { t } = useLingui();
    const frame = useRef<HTMLDivElement>(null);
    const image = useRef<HTMLImageElement>(null);
    const placement = useRef<Placement>({ x: 0, y: 0, scale: 1, base: 1, width: 0, height: 0, size: 0 });
    const [file, setFile] = useState<File | null>(null);
    const [url, setUrl] = useState<string | null>(null);
    const [ready, setReady] = useState(false);
    const [zoom, setZoom] = useState(1);

    useEffect(
        () => () => {
            if (url) {
                URL.revokeObjectURL(url);
            }
        },
        [url],
    );

    // Zooms with the mouse wheel around the pointer. A native listener: React wheel listeners are passive and cannot keep
    // the page from scrolling.
    useEffect(() => {
        const element = frame.current;
        if (!element) {
            return;
        }

        const wheel = (event: globalThis.WheelEvent) => {
            event.preventDefault();
            const box = element.getBoundingClientRect();
            const state = placement.current;
            zoomTo((state.scale / state.base) * (event.deltaY < 0 ? 1.1 : 1 / 1.1), event.clientX - box.left, event.clientY - box.top);
        };
        element.addEventListener("wheel", wheel, { passive: false });
        return () => element.removeEventListener("wheel", wheel);
    });

    /**
     * Keeps the frame covered by the image, draws it and reports the square.
     * @param chosen The file the square belongs to.
     */
    function render(chosen: File): void {
        const state = placement.current;
        const width = state.width * state.scale;
        const height = state.height * state.scale;
        state.x = Math.min(0, Math.max(state.size - width, state.x));
        state.y = Math.min(0, Math.max(state.size - height, state.y));
        if (image.current) {
            image.current.style.width = `${state.width}px`;
            image.current.style.height = `${state.height}px`;
            image.current.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;
        }

        onChange({
            file: chosen,
            crop: { left: -state.x / width, top: -state.y / height, width: state.size / width, height: state.size / height },
        });
    }

    /**
     * Zooms around a point of the frame.
     * @param factor The new zoom (1–4) relative to the size that just covers the frame.
     * @param cx The x of the fixed point in the frame.
     * @param cy The y of the fixed point in the frame.
     */
    function zoomTo(factor: number, cx = placement.current.size / 2, cy = placement.current.size / 2): void {
        const state = placement.current;
        if (!file) {
            return;
        }

        const scale = state.base * Math.min(4, Math.max(1, factor));
        state.x = cx - ((cx - state.x) * scale) / state.scale;
        state.y = cy - ((cy - state.y) * scale) / state.scale;
        state.scale = scale;
        setZoom(scale / state.base);
        render(file);
    }

    /**
     * Takes a chosen file: a smaller copy of a large photo, then the preview.
     * @param files The chosen files.
     */
    async function choose(files: readonly File[]): Promise<void> {
        const chosen = files[0];
        setReady(false);
        if (!chosen || !chosen.type.startsWith("image/")) {
            setFile(null);
            setUrl(null);
            onChange(null);
            return;
        }

        const shrunk = await shrinkImage(chosen, 1600);
        setFile(shrunk);
        setUrl(URL.createObjectURL(shrunk));
        // Until the preview loads (or when the browser cannot show the format) the server uses the centre.
        onChange({ file: shrunk, crop: null });
    }

    /**
     * Places the loaded preview so it just covers the frame, centred.
     */
    function place(): void {
        const element = image.current;
        if (!element || !frame.current || !file) {
            return;
        }

        const state = placement.current;
        state.size = frame.current.clientWidth;
        state.width = element.naturalWidth;
        state.height = element.naturalHeight;
        state.base = Math.max(state.size / state.width, state.size / state.height);
        state.scale = state.base;
        state.x = (state.size - state.width * state.scale) / 2;
        state.y = (state.size - state.height * state.scale) / 2;
        setZoom(1);
        setReady(true);
        render(file);
    }

    /**
     * Drags the photo with the pointer.
     * @param event The pointer down event.
     */
    function drag(event: PointerEvent<HTMLDivElement>): void {
        const target = event.currentTarget;
        const chosen = file;
        if (!chosen) {
            return;
        }

        target.setPointerCapture(event.pointerId);
        const state = placement.current;
        const start = { x: event.clientX - state.x, y: event.clientY - state.y };
        const move = (next: globalThis.PointerEvent) => {
            state.x = next.clientX - start.x;
            state.y = next.clientY - start.y;
            render(chosen);
        };
        const stop = () => {
            target.removeEventListener("pointermove", move);
            target.removeEventListener("pointerup", stop);
            target.removeEventListener("pointercancel", stop);
        };
        target.addEventListener("pointermove", move);
        target.addEventListener("pointerup", stop);
        target.addEventListener("pointercancel", stop);
    }

    /**
     * Moves (arrows) or zooms (+ and -) the photo with the keyboard.
     * @param event The key event.
     */
    function key(event: KeyboardEvent<HTMLDivElement>): void {
        const steps: Record<string, readonly [number, number]> = {
            ArrowLeft: [8, 0],
            ArrowRight: [-8, 0],
            ArrowUp: [0, 8],
            ArrowDown: [0, -8],
        };
        const step = steps[event.key];
        const state = placement.current;
        if (step && file) {
            event.preventDefault();
            state.x += step[0];
            state.y += step[1];
            render(file);
        } else if (event.key === "+" || event.key === "-") {
            event.preventDefault();
            zoomTo(state.scale / state.base + (event.key === "+" ? 0.1 : -0.1));
        }
    }

    return (
        <div className={styles.picker}>
            {url && (
                <div className={styles.stage} hidden={!ready}>
                    <div
                        ref={frame}
                        className={styles.frame}
                        tabIndex={0}
                        role="img"
                        aria-label={t`Попередній перегляд фото`}
                        onPointerDown={drag}
                        onKeyDown={key}
                    >
                        <img
                            ref={image}
                            src={url}
                            alt=""
                            draggable={false}
                            onLoad={place}
                            onError={() => {
                                setReady(false);
                                setUrl(null);
                            }}
                        />
                    </div>
                    <label className={styles.zoom}>
                        <Icon icon={ZoomOut} size="sm" />
                        <input
                            type="range"
                            min={1}
                            max={4}
                            step={0.01}
                            value={zoom}
                            aria-label={t`Масштаб`}
                            onChange={(event) => zoomTo(Number(event.target.value))}
                        />
                        <Icon icon={ZoomIn} size="sm" />
                    </label>
                    <p className={styles.help}>{t`Перетягніть фото та змініть масштаб, щоб обличчя було в колі.`}</p>
                </div>
            )}
            <FileDrop
                icon={Camera}
                accept={imageAccept}
                disabled={disabled}
                title={file?.name ?? t`Оберіть або перетягніть фото`}
                hint={t`JPEG, PNG, WebP, AVIF або GIF до 15 МБ. Фото буде стиснуто, метадані (зокрема геолокацію) видалено.`}
                onFiles={(files) => void choose(files)}
            />
        </div>
    );
}
