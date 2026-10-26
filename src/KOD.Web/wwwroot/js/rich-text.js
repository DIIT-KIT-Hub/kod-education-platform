/**
 * The rich text editor of news articles (ES module over Tiptap, loaded by the RichTextEditor component).
 * The toolbar is server-rendered markup: buttons carry data-command and get aria-pressed / disabled from the editor
 * state. Images are uploaded to the REST API right away (photos are shrunk first), inserted as uploaded media and
 * attached to the article when it is saved. A gallery is a block of images shown as tiles that open full size.
 * The server sanitizes the HTML again, so the editor only has to produce the markup it allows.
 */

import { Editor, Node, mergeAttributes, StarterKit, Image, TextAlign, TableKit, Placeholder, CharacterCount } from "../lib/tiptap/tiptap.js";
import { shrinkImage } from "./image-shrink.js";

/** The longest side of a photo before upload (the server keeps up to 1600 px). */
const UPLOAD_SIDE = 2400;

/** The maximum number of text characters. */
const TEXT_LIMIT = 50000;

/**
 * Translates an interface text.
 * @param {string} text The Ukrainian source text.
 * @param {...(string|number)} values The values of {0}, {1}…
 * @returns {string} The text in the interface language.
 */
function t(text, ...values) {
    const translated = window.kodDs ? window.kodDs.t(text) : text;
    return translated.replace(/\{(\d+)\}/g, (_, index) => String(values[Number(index)] ?? ""));
}

/**
 * Returns the antiforgery token for REST calls.
 * @returns {string} The token.
 */
function csrfToken() {
    return document.querySelector('meta[name="csrf-token"]')?.getAttribute("content") || "";
}

/** An image with its full-size rendition (gallery tiles show the thumbnail) and its intrinsic size. */
const MediaImage = Image.extend({
    addAttributes() {
        return {
            ...this.parent?.(),
            full: {
                default: null,
                parseHTML: (element) => element.getAttribute("data-full"),
                renderHTML: (attributes) => (attributes.full ? { "data-full": attributes.full } : {}),
            },
            width: {
                default: null,
                parseHTML: (element) => element.getAttribute("width"),
                renderHTML: (attributes) => (attributes.width ? { width: attributes.width } : {}),
            },
            height: {
                default: null,
                parseHTML: (element) => element.getAttribute("height"),
                renderHTML: (attributes) => (attributes.height ? { height: attributes.height } : {}),
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

    parseHTML() {
        return [{ tag: 'div[data-type="gallery"]' }];
    },

    renderHTML({ HTMLAttributes }) {
        return ["div", mergeAttributes(HTMLAttributes, { "data-type": "gallery" }), 0];
    },
});

/**
 * Uploads one image.
 * @param {string} url The upload endpoint.
 * @param {File} file The image.
 * @returns {Promise<{id: string, url: string, thumbnailUrl: string, width: number, height: number}>} The stored image.
 */
async function upload(url, file) {
    const body = new FormData();
    body.append("file", await shrinkImage(file, UPLOAD_SIDE));
    const response = await fetch(url, { method: "POST", body, credentials: "same-origin", headers: { "X-CSRF-TOKEN": csrfToken() } });
    if (!response.ok) {
        const problem = await response.json().catch(() => null);
        throw new Error(problem?.detail || t("Не вдалося завантажити фото «{0}».", file.name));
    }

    return response.json();
}

/**
 * Creates the editor in a RichTextEditor host.
 * @param {HTMLElement} host The component root: toolbar buttons [data-command], [data-rte-content], [data-rte-file],
 *   [data-rte-status], [data-rte-count], [data-rte-linkbar] and the data-upload-url / data-placeholder attributes.
 * @param {string} html The starting content.
 * @returns {{getContent: () => Uint8Array, focus: () => void, dispose: () => void}} The handle used by .NET.
 */
export function createEditor(host, html) {
    const content = /** @type {HTMLElement} */ (host.querySelector("[data-rte-content]"));
    const fileInput = /** @type {HTMLInputElement} */ (host.querySelector("[data-rte-file]"));
    const status = /** @type {HTMLElement} */ (host.querySelector("[data-rte-status]"));
    const count = /** @type {HTMLElement} */ (host.querySelector("[data-rte-count]"));
    const linkbar = /** @type {HTMLElement} */ (host.querySelector("[data-rte-linkbar]"));
    const linkInput = /** @type {HTMLInputElement} */ (linkbar.querySelector("input"));
    const uploadUrl = host.getAttribute("data-upload-url") || "/api/v1/media";

    /** What the file picker inserts: "image" (into the text) or "gallery". */
    let pickMode = "image";
    let uploading = 0;

    const editor = new Editor({
        element: content,
        content: html || "",
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
            Placeholder.configure({ placeholder: host.getAttribute("data-placeholder") || "" }),
            CharacterCount.configure({ limit: TEXT_LIMIT }),
        ],
        editorProps: {
            attributes: { class: "prose rte__prose", spellcheck: "true", "aria-label": host.getAttribute("data-label") || "", role: "textbox", "aria-multiline": "true" },
            handlePaste: (_view, event) => insertFiles(Array.from(event.clipboardData?.files || []), "image"),
            handleDrop: (_view, event, _slice, moved) => !moved && insertFiles(Array.from(/** @type {DragEvent} */ (event).dataTransfer?.files || []), "image"),
        },
        onTransaction: refresh,
    });

    /**
     * Shows a status message (uploads and errors).
     * @param {string} text The message; empty to clear.
     * @param {boolean} [isError] Whether it is an error.
     */
    function setStatus(text, isError = false) {
        status.textContent = text;
        status.toggleAttribute("data-error", isError);
    }

    /**
     * Uploads images and inserts them into the text or a gallery (an existing gallery around the cursor grows).
     * @param {File[]} files The chosen files.
     * @param {string} mode "image" or "gallery".
     * @returns {boolean} Whether the files were taken over (images only).
     */
    function insertFiles(files, mode) {
        const images = files.filter((file) => file.type.startsWith("image/"));
        if (images.length === 0) {
            return false;
        }

        uploading += images.length;
        setStatus(t("Завантаження фото… ({0})", uploading));
        Promise.allSettled(images.map((file) => upload(uploadUrl, file))).then((results) => {
            uploading -= images.length;
            const stored = results.filter((result) => result.status === "fulfilled").map((result) => result.value);
            const failed = results.find((result) => result.status === "rejected");
            setStatus(failed ? failed.reason.message : (uploading > 0 ? t("Завантаження фото… ({0})", uploading) : ""), Boolean(failed));
            if (stored.length === 0) {
                return;
            }

            // Inserted after the selection, followed by a paragraph that takes the cursor: a selected image would
            // otherwise be replaced by whatever is inserted next.
            const at = editor.state.selection.to;
            const paragraph = { type: "paragraph" };
            if (mode === "gallery") {
                const tiles = stored.map((image) => ({ type: "image", attrs: { src: image.thumbnailUrl, full: image.url, width: image.width, height: image.height, alt: "" } }));
                const gallery = galleryAtCursor();
                editor.chain().focus().insertContentAt(gallery ? gallery.end : at, gallery ? tiles : [{ type: "gallery", content: tiles }, paragraph]).run();
            } else {
                const nodes = stored.map((image) => ({ type: "image", attrs: { src: image.url, width: image.width, height: image.height, alt: "" } }));
                editor.chain().focus().insertContentAt(at, [...nodes, paragraph]).run();
            }
        });

        return true;
    }

    /**
     * Finds the gallery that contains the cursor.
     * @returns {{end: number}|null} The position right before the end of its content.
     */
    function galleryAtCursor() {
        const { $from } = editor.state.selection;
        for (let depth = $from.depth; depth >= 0; depth--) {
            if ($from.node(depth).type.name === "gallery") {
                return { end: $from.end(depth) };
            }
        }

        const node = /** @type {any} */ (editor.state.selection).node;
        return node && node.type.name === "gallery" ? { end: editor.state.selection.from + node.nodeSize - 1 } : null;
    }

    /** The commands of the toolbar buttons. */
    const commands = {
        paragraph: () => editor.chain().focus().setParagraph().run(),
        h2: () => editor.chain().focus().toggleHeading({ level: 2 }).run(),
        h3: () => editor.chain().focus().toggleHeading({ level: 3 }).run(),
        h4: () => editor.chain().focus().toggleHeading({ level: 4 }).run(),
        bold: () => editor.chain().focus().toggleBold().run(),
        italic: () => editor.chain().focus().toggleItalic().run(),
        underline: () => editor.chain().focus().toggleUnderline().run(),
        strike: () => editor.chain().focus().toggleStrike().run(),
        code: () => editor.chain().focus().toggleCode().run(),
        bulletList: () => editor.chain().focus().toggleBulletList().run(),
        orderedList: () => editor.chain().focus().toggleOrderedList().run(),
        blockquote: () => editor.chain().focus().toggleBlockquote().run(),
        rule: () => editor.chain().focus().setHorizontalRule().run(),
        alignLeft: () => editor.chain().focus().setTextAlign("left").run(),
        alignCenter: () => editor.chain().focus().setTextAlign("center").run(),
        alignRight: () => editor.chain().focus().setTextAlign("right").run(),
        link: () => openLinkbar(),
        unlink: () => editor.chain().focus().extendMarkRange("link").unsetLink().run(),
        image: () => pick("image"),
        gallery: () => pick("gallery"),
        table: () => editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run(),
        rowAfter: () => editor.chain().focus().addRowAfter().run(),
        columnAfter: () => editor.chain().focus().addColumnAfter().run(),
        deleteRow: () => editor.chain().focus().deleteRow().run(),
        deleteColumn: () => editor.chain().focus().deleteColumn().run(),
        deleteTable: () => editor.chain().focus().deleteTable().run(),
        undo: () => editor.chain().focus().undo().run(),
        redo: () => editor.chain().focus().redo().run(),
    };

    /** Whether a toolbar command is active at the cursor. */
    const active = {
        paragraph: () => editor.isActive("paragraph"),
        h2: () => editor.isActive("heading", { level: 2 }),
        h3: () => editor.isActive("heading", { level: 3 }),
        h4: () => editor.isActive("heading", { level: 4 }),
        bold: () => editor.isActive("bold"),
        italic: () => editor.isActive("italic"),
        underline: () => editor.isActive("underline"),
        strike: () => editor.isActive("strike"),
        code: () => editor.isActive("code"),
        bulletList: () => editor.isActive("bulletList"),
        orderedList: () => editor.isActive("orderedList"),
        blockquote: () => editor.isActive("blockquote"),
        alignLeft: () => editor.isActive({ textAlign: "left" }),
        alignCenter: () => editor.isActive({ textAlign: "center" }),
        alignRight: () => editor.isActive({ textAlign: "right" }),
        link: () => editor.isActive("link"),
    };

    /**
     * Opens the file picker.
     * @param {string} mode "image" or "gallery".
     */
    function pick(mode) {
        pickMode = mode;
        fileInput.value = "";
        fileInput.click();
    }

    /**
     * Shows the link bar with the current address.
     */
    function openLinkbar() {
        linkbar.hidden = false;
        linkInput.value = editor.getAttributes("link").href || "";
        linkInput.focus();
        linkInput.select();
    }

    /**
     * Applies the address of the link bar to the selection (an empty address removes the link).
     */
    function applyLink() {
        const href = linkInput.value.trim();
        const chain = editor.chain().focus().extendMarkRange("link");
        if (href) {
            chain.setLink({ href: /^(https?:|mailto:|tel:|\/)/i.test(href) ? href : `https://${href}` }).run();
        } else {
            chain.unsetLink().run();
        }

        linkbar.hidden = true;
    }

    /**
     * Updates the toolbar (pressed and disabled buttons, table tools) and the character counter.
     */
    function refresh() {
        host.querySelectorAll("[data-command]").forEach((button) => {
            const name = /** @type {keyof typeof commands} */ (button.getAttribute("data-command"));
            const isActive = active[/** @type {keyof typeof active} */ (name)];
            if (isActive) {
                button.setAttribute("aria-pressed", String(isActive()));
            }
        });

        host.querySelectorAll('[data-command="undo"]').forEach((button) => { /** @type {HTMLButtonElement} */ (button).disabled = !editor.can().undo(); });
        host.querySelectorAll('[data-command="redo"]').forEach((button) => { /** @type {HTMLButtonElement} */ (button).disabled = !editor.can().redo(); });
        host.querySelectorAll("[data-rte-table-tools]").forEach((tools) => { /** @type {HTMLElement} */ (tools).hidden = !editor.isActive("table"); });
        count.textContent = t("{0} / {1} символів", editor.storage.characterCount.characters(), TEXT_LIMIT);
    }

    const onClick = (/** @type {MouseEvent} */ event) => {
        const button = event.target instanceof Element ? event.target.closest("[data-command]") : null;
        if (button && host.contains(button)) {
            event.preventDefault();
            commands[/** @type {keyof typeof commands} */ (button.getAttribute("data-command"))]?.();
        }
    };
    host.addEventListener("click", onClick);

    // Buttons keep the editor selection: a mouse press would otherwise move the focus to the toolbar first.
    host.querySelector(".rte__toolbar")?.addEventListener("mousedown", (event) => {
        if (event.target instanceof Element && event.target.closest("[data-command]")) {
            event.preventDefault();
        }
    });

    fileInput.addEventListener("change", () => insertFiles(Array.from(fileInput.files || []), pickMode));
    linkbar.querySelector("[data-link-apply]")?.addEventListener("click", applyLink);
    linkbar.querySelector("[data-link-cancel]")?.addEventListener("click", () => { linkbar.hidden = true; editor.commands.focus(); });
    linkInput.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            applyLink();
        } else if (event.key === "Escape") {
            linkbar.hidden = true;
            editor.commands.focus();
        }
    });

    host.setAttribute("data-ready", "");
    refresh();

    return {
        getContent: () => new TextEncoder().encode(editor.isEmpty ? "" : editor.getHTML()),
        isUploading: () => uploading > 0,
        focus: () => editor.commands.focus(),
        dispose: () => {
            host.removeEventListener("click", onClick);
            editor.destroy();
        },
    };
}
