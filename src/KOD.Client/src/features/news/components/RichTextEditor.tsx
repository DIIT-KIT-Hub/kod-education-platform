import { mergeAttributes, Node } from "@tiptap/core";
import { Image } from "@tiptap/extension-image";
import { TableKit } from "@tiptap/extension-table";
import { TextAlign } from "@tiptap/extension-text-align";
import { CharacterCount, Placeholder } from "@tiptap/extensions";
import { EditorContent, useEditor, useEditorState, type Editor } from "@tiptap/react";
import { StarterKit } from "@tiptap/starter-kit";
import { useLingui } from "@lingui/react/macro";
import {
    AlignCenter,
    AlignLeft,
    AlignRight,
    Bold,
    Code,
    Heading2,
    Heading3,
    Heading4,
    Images,
    Image as ImageIcon,
    Italic,
    Link,
    List,
    ListOrdered,
    Minus,
    Pilcrow,
    Plus,
    Quote,
    Redo2,
    Strikethrough,
    Table,
    Trash2,
    Underline,
    Undo2,
    Unlink,
    type LucideIcon,
} from "lucide-react";
import { useRef, useState } from "react";
import { describeError } from "@/shared/api/problem";
import { imageAccept } from "@/shared/lib/image";
import { Button } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { TextInput } from "@/shared/ui/TextInput";
import { uploadImage } from "../api";
import prose from "./Prose.module.css";
import styles from "./RichTextEditor.module.css";

/** The maximum number of text characters. */
const textLimit = 50_000;

/** An image with its full-size rendition (gallery tiles show the thumbnail) and its intrinsic size. */
const MediaImage = Image.extend({
    addAttributes() {
        return {
            ...this.parent?.(),
            full: {
                default: null,
                parseHTML: (element: HTMLElement) => element.getAttribute("data-full"),
                renderHTML: (attributes: Record<string, unknown>) => (attributes.full ? { "data-full": attributes.full } : {}),
            },
            width: {
                default: null,
                parseHTML: (element: HTMLElement) => element.getAttribute("width"),
                renderHTML: (attributes: Record<string, unknown>) => (attributes.width ? { width: attributes.width } : {}),
            },
            height: {
                default: null,
                parseHTML: (element: HTMLElement) => element.getAttribute("height"),
                renderHTML: (attributes: Record<string, unknown>) => (attributes.height ? { height: attributes.height } : {}),
            },
        };
    },
});

/** A gallery: a block of images shown as tiles. */
const Gallery = Node.create({
    name: "gallery",
    group: "block",
    content: "image+",
    draggable: true,
    isolating: true,
    parseHTML: () => [{ tag: 'div[data-type="gallery"]' }],
    renderHTML: ({ HTMLAttributes }) => ["div", mergeAttributes(HTMLAttributes, { "data-type": "gallery" }), 0],
});

/**
 * Props of {@link Tool}.
 */
interface ToolProps {
    /** The icon. */
    readonly icon: LucideIcon;
    /** The name (tooltip and accessible name). */
    readonly name: string;
    /** The command. */
    readonly run: () => void;
    /** Whether the format is active at the cursor. */
    readonly pressed?: boolean;
    /** Whether the command is unavailable. */
    readonly disabled?: boolean;
}

/**
 * A toolbar button of the editor.
 * @param props The props.
 * @returns The button.
 */
function Tool({ icon, name, run, pressed, disabled }: ToolProps) {
    return (
        <button
            type="button"
            className={styles.tool}
            title={name}
            aria-label={name}
            aria-pressed={pressed}
            disabled={disabled}
            // Keeps the editor selection: a mouse press would otherwise move the focus to the toolbar first.
            onMouseDown={(event) => event.preventDefault()}
            onClick={run}
        >
            <Icon icon={icon} />
        </button>
    );
}

/**
 * Props of {@link RichTextEditor}.
 */
export interface RichTextEditorProps {
    /** The starting HTML. */
    readonly initialHtml: string;
    /** Called with the HTML after every change ("" when empty). */
    readonly onChange: (html: string) => void;
    /** Called with the number of images still uploading (the form waits for them). */
    readonly onUploadingChange: (count: number) => void;
    /** The accessible name of the text area. */
    readonly label: string;
    /** The placeholder of an empty text. */
    readonly placeholder: string;
    /** Marks the text as invalid. */
    readonly invalid?: boolean;
}

/**
 * The editor of article texts (Tiptap): headings, lists, quotes, tables, links, alignment, images in the text and
 * galleries. Images are uploaded at once (pasted, dropped or chosen; large photos are shrunk first) and inserted as
 * uploaded media. The server sanitizes the HTML again, so the editor only has to produce the markup it allows.
 * @param props The props.
 * @returns The editor.
 */
export function RichTextEditor({ initialHtml, onChange, onUploadingChange, label, placeholder, invalid }: RichTextEditorProps) {
    const { t } = useLingui();
    const fileInput = useRef<HTMLInputElement>(null);
    const pickMode = useRef<"image" | "gallery">("image");
    const uploading = useRef(0);
    const [status, setStatus] = useState<{ readonly text: string; readonly error: boolean } | null>(null);
    const [linkOpen, setLinkOpen] = useState(false);
    const [link, setLink] = useState("");

    const editor = useEditor({
        content: initialHtml,
        immediatelyRender: true,
        extensions: [
            StarterKit.configure({
                heading: { levels: [2, 3, 4] },
                link: {
                    openOnClick: false,
                    autolink: true,
                    defaultProtocol: "https",
                    protocols: ["http", "https", "mailto", "tel"],
                    HTMLAttributes: { rel: "noopener noreferrer nofollow", target: null },
                },
            }),
            MediaImage.configure({ inline: false, allowBase64: false }),
            Gallery,
            TextAlign.configure({ types: ["heading", "paragraph"] }),
            TableKit.configure({ table: { resizable: false } }),
            Placeholder.configure({ placeholder }),
            CharacterCount.configure({ limit: textLimit }),
        ],
        editorProps: {
            attributes: {
                class: `${prose.prose} ${styles.prose}`,
                spellcheck: "true",
                "aria-label": label,
                role: "textbox",
                "aria-multiline": "true",
            },
            handlePaste: (_view, event) => insertFiles(Array.from(event.clipboardData?.files ?? []), "image"),
            handleDrop: (_view, event, _slice, moved) =>
                !moved && insertFiles(Array.from((event as DragEvent).dataTransfer?.files ?? []), "image"),
        },
        onUpdate: ({ editor: current }) => onChange(current.isEmpty ? "" : current.getHTML()),
    });

    const state = useEditorState({
        editor,
        selector: ({ editor: current }: { editor: Editor }) => ({
            paragraph: current.isActive("paragraph"),
            h2: current.isActive("heading", { level: 2 }),
            h3: current.isActive("heading", { level: 3 }),
            h4: current.isActive("heading", { level: 4 }),
            bold: current.isActive("bold"),
            italic: current.isActive("italic"),
            underline: current.isActive("underline"),
            strike: current.isActive("strike"),
            code: current.isActive("code"),
            bulletList: current.isActive("bulletList"),
            orderedList: current.isActive("orderedList"),
            blockquote: current.isActive("blockquote"),
            alignLeft: current.isActive({ textAlign: "left" }),
            alignCenter: current.isActive({ textAlign: "center" }),
            alignRight: current.isActive({ textAlign: "right" }),
            link: current.isActive("link"),
            table: current.isActive("table"),
            canUndo: current.can().undo(),
            canRedo: current.can().redo(),
            characters: current.storage.characterCount.characters() as number,
        }),
    });

    /**
     * Updates the number of uploads in progress.
     * @param delta The change.
     */
    function track(delta: number): void {
        uploading.current += delta;
        onUploadingChange(uploading.current);
    }

    /**
     * Uploads images and inserts them into the text or a gallery (the gallery around the cursor grows).
     * @param files The chosen files.
     * @param mode "image" or "gallery".
     * @returns Whether the files were taken over (images only).
     */
    function insertFiles(files: readonly File[], mode: "image" | "gallery"): boolean {
        const images = files.filter((file) => file.type.startsWith("image/"));
        if (images.length === 0) {
            return false;
        }

        track(images.length);
        const count = uploading.current;
        setStatus({ text: t`Завантаження фото… (${count})`, error: false });
        void Promise.allSettled(images.map(uploadImage)).then((results) => {
            track(-images.length);
            const stored = results.flatMap((result) => (result.status === "fulfilled" ? [result.value] : []));
            const failed = results.find((result) => result.status === "rejected");
            const left = uploading.current;
            setStatus(
                failed
                    ? { text: describeError(failed.reason, t`Не вдалося завантажити фото.`), error: true }
                    : left > 0
                      ? { text: t`Завантаження фото… (${left})`, error: false }
                      : null,
            );
            if (stored.length === 0) {
                return;
            }

            // Inserted after the selection, followed by a paragraph that takes the cursor: a selected image would
            // otherwise be replaced by whatever is inserted next.
            const at = editor.state.selection.to;
            const paragraph = { type: "paragraph" };
            if (mode === "gallery") {
                const tiles = stored.map((image) => ({
                    type: "image",
                    attrs: { src: image.thumbnailUrl, full: image.url, width: image.width, height: image.height, alt: "" },
                }));
                const gallery = galleryAtCursor();
                editor
                    .chain()
                    .focus()
                    .insertContentAt(gallery ?? at, gallery !== null ? tiles : [{ type: "gallery", content: tiles }, paragraph])
                    .run();
            } else {
                const nodes = stored.map((image) => ({
                    type: "image",
                    attrs: { src: image.url, width: image.width, height: image.height, alt: "" },
                }));
                editor
                    .chain()
                    .focus()
                    .insertContentAt(at, [...nodes, paragraph])
                    .run();
            }
        });

        return true;
    }

    /**
     * Finds the gallery around the cursor.
     * @returns The position right before the end of its content, or null.
     */
    function galleryAtCursor(): number | null {
        const { $from } = editor.state.selection;
        for (let depth = $from.depth; depth >= 0; depth--) {
            if ($from.node(depth).type.name === "gallery") {
                return $from.end(depth);
            }
        }

        const node = (editor.state.selection as { node?: { type: { name: string }; nodeSize: number } }).node;
        return node?.type.name === "gallery" ? editor.state.selection.from + node.nodeSize - 1 : null;
    }

    /**
     * Opens the file picker.
     * @param mode What the chosen images become.
     */
    function pick(mode: "image" | "gallery"): void {
        pickMode.current = mode;
        if (fileInput.current) {
            fileInput.current.value = "";
            fileInput.current.click();
        }
    }

    /**
     * Applies the address of the link bar to the selection (an empty address removes the link).
     */
    function applyLink(): void {
        const href = link.trim();
        const chain = editor.chain().focus().extendMarkRange("link");
        if (href) {
            chain.setLink({ href: /^(https?:|mailto:|tel:|\/)/i.test(href) ? href : `https://${href}` }).run();
        } else {
            chain.unsetLink().run();
        }

        setLinkOpen(false);
    }

    const chain = () => editor.chain().focus();
    const characters = state.characters;

    return (
        <div className={styles.editor} data-invalid={invalid || undefined}>
            <div className={styles.toolbar} role="toolbar" aria-label={t`Форматування тексту`}>
                <div className={styles.group}>
                    <Tool icon={Pilcrow} name={t`Звичайний текст`} pressed={state.paragraph} run={() => chain().setParagraph().run()} />
                    <Tool icon={Heading2} name={t`Заголовок`} pressed={state.h2} run={() => chain().toggleHeading({ level: 2 }).run()} />
                    <Tool icon={Heading3} name={t`Підзаголовок`} pressed={state.h3} run={() => chain().toggleHeading({ level: 3 }).run()} />
                    <Tool
                        icon={Heading4}
                        name={t`Малий заголовок`}
                        pressed={state.h4}
                        run={() => chain().toggleHeading({ level: 4 }).run()}
                    />
                </div>
                <div className={styles.group}>
                    <Tool icon={Bold} name={t`Жирний (Ctrl+B)`} pressed={state.bold} run={() => chain().toggleBold().run()} />
                    <Tool icon={Italic} name={t`Курсив (Ctrl+I)`} pressed={state.italic} run={() => chain().toggleItalic().run()} />
                    <Tool
                        icon={Underline}
                        name={t`Підкреслений (Ctrl+U)`}
                        pressed={state.underline}
                        run={() => chain().toggleUnderline().run()}
                    />
                    <Tool icon={Strikethrough} name={t`Закреслений`} pressed={state.strike} run={() => chain().toggleStrike().run()} />
                    <Tool icon={Code} name={t`Код`} pressed={state.code} run={() => chain().toggleCode().run()} />
                </div>
                <div className={styles.group}>
                    <Tool icon={List} name={t`Маркований список`} pressed={state.bulletList} run={() => chain().toggleBulletList().run()} />
                    <Tool
                        icon={ListOrdered}
                        name={t`Нумерований список`}
                        pressed={state.orderedList}
                        run={() => chain().toggleOrderedList().run()}
                    />
                    <Tool icon={Quote} name={t`Цитата`} pressed={state.blockquote} run={() => chain().toggleBlockquote().run()} />
                    <Tool icon={Minus} name={t`Розділювач`} run={() => chain().setHorizontalRule().run()} />
                </div>
                <div className={styles.group}>
                    <Tool
                        icon={AlignLeft}
                        name={t`По лівому краю`}
                        pressed={state.alignLeft}
                        run={() => chain().setTextAlign("left").run()}
                    />
                    <Tool
                        icon={AlignCenter}
                        name={t`По центру`}
                        pressed={state.alignCenter}
                        run={() => chain().setTextAlign("center").run()}
                    />
                    <Tool
                        icon={AlignRight}
                        name={t`По правому краю`}
                        pressed={state.alignRight}
                        run={() => chain().setTextAlign("right").run()}
                    />
                </div>
                <div className={styles.group}>
                    <Tool
                        icon={Link}
                        name={t`Посилання`}
                        pressed={state.link}
                        run={() => {
                            setLink((editor.getAttributes("link").href as string | undefined) ?? "");
                            setLinkOpen(true);
                        }}
                    />
                    <Tool icon={Unlink} name={t`Прибрати посилання`} run={() => chain().extendMarkRange("link").unsetLink().run()} />
                </div>
                <div className={styles.group}>
                    <Tool icon={ImageIcon} name={t`Фото в тексті`} run={() => pick("image")} />
                    <Tool icon={Images} name={t`Галерея фото`} run={() => pick("gallery")} />
                    <Tool icon={Table} name={t`Таблиця`} run={() => chain().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()} />
                </div>
                <div className={[styles.group, styles.end].join(" ")}>
                    <Tool icon={Undo2} name={t`Скасувати (Ctrl+Z)`} disabled={!state.canUndo} run={() => chain().undo().run()} />
                    <Tool icon={Redo2} name={t`Повторити (Ctrl+Shift+Z)`} disabled={!state.canRedo} run={() => chain().redo().run()} />
                </div>
            </div>

            {state.table && (
                <div className={styles.tools}>
                    <span className={styles.toolsLabel}>{t`Таблиця:`}</span>
                    <Button variant="ghost" size="sm" icon={Plus} onClick={() => chain().addRowAfter().run()}>
                        {t`Рядок`}
                    </Button>
                    <Button variant="ghost" size="sm" icon={Plus} onClick={() => chain().addColumnAfter().run()}>
                        {t`Стовпець`}
                    </Button>
                    <Button variant="ghost" size="sm" icon={Minus} onClick={() => chain().deleteRow().run()}>
                        {t`Рядок`}
                    </Button>
                    <Button variant="ghost" size="sm" icon={Minus} onClick={() => chain().deleteColumn().run()}>
                        {t`Стовпець`}
                    </Button>
                    <Button variant="ghost" size="sm" icon={Trash2} onClick={() => chain().deleteTable().run()}>
                        {t`Видалити таблицю`}
                    </Button>
                </div>
            )}

            {linkOpen && (
                <div className={styles.tools}>
                    <Icon icon={Link} size="sm" />
                    <TextInput
                        compact
                        type="url"
                        className={styles.linkInput}
                        value={link}
                        placeholder="https://…"
                        aria-label={t`Адреса посилання`}
                        autoFocus
                        onChange={(event) => setLink(event.target.value)}
                        onKeyDown={(event) => {
                            if (event.key === "Enter") {
                                event.preventDefault();
                                applyLink();
                            } else if (event.key === "Escape") {
                                setLinkOpen(false);
                                editor.commands.focus();
                            }
                        }}
                    />
                    <Button variant="secondary" size="sm" onClick={applyLink}>
                        {t`Застосувати`}
                    </Button>
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                            setLinkOpen(false);
                            editor.commands.focus();
                        }}
                    >
                        {t`Скасувати`}
                    </Button>
                </div>
            )}

            <EditorContent editor={editor} className={styles.content} />

            <div className={styles.footer}>
                <span className={styles.status} data-error={status?.error || undefined} role="status">
                    {status?.text}
                </span>
                <span>{t`${characters} / ${textLimit} символів`}</span>
            </div>

            <input
                ref={fileInput}
                type="file"
                accept={imageAccept}
                multiple
                hidden
                onChange={(event) => insertFiles(Array.from(event.target.files ?? []), pickMode.current)}
            />
        </div>
    );
}
