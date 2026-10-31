import { Placeholder } from "@tiptap/extensions";
import { EditorContent, useEditor, useEditorState, type Editor } from "@tiptap/react";
import { StarterKit } from "@tiptap/starter-kit";
import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import {
    Bold,
    Code,
    FileText,
    Italic,
    Link,
    List,
    ListOrdered,
    Paperclip,
    Quote,
    SendHorizontal,
    SquareCode,
    Strikethrough,
    Type,
    Underline,
    Unlink,
    X,
    type LucideIcon,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { describeError } from "@/shared/api/problem";
import { formatFileSize } from "@/shared/i18n/format";
import { sendLive } from "@/shared/realtime/live";
import { useCurrentUser } from "@/shared/session/session";
import { Button } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { fileAccept, isAllowedFile, maxFiles, maxFileSize, maxTotalSize, sendDraft } from "@/features/chats/send";
import styles from "./chats.module.css";

/**
 * How often the "typing" signal is sent while the person types.
 */
const typingInterval = 2500;

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
}

/**
 * A formatting button.
 * @param props The props.
 * @returns The button.
 */
function Tool({ icon, name, run, pressed }: ToolProps) {
    return (
        <button
            type="button"
            className={styles.tool}
            title={name}
            aria-label={name}
            aria-pressed={pressed}
            // Keeps the editor selection: a mouse press would otherwise move the focus to the toolbar first.
            onMouseDown={(event) => event.preventDefault()}
            onClick={run}
        >
            <Icon icon={icon} size="sm" />
        </button>
    );
}

/**
 * Props of {@link Composer}.
 */
export interface ComposerProps {
    /** The chat. */
    readonly chatId: string;
    /** The text of a message being edited; without it the composer writes new messages. */
    readonly editing?: { readonly html: string; readonly save: (html: string) => Promise<void>; readonly cancel: () => void };
}

/**
 * The message editor of a chat, like in Teams: Enter sends, Shift+Enter starts a new line; the "Format" button opens the
 * formatting bar (bold, italic, underline, strike, code, lists, quotes, links), where Enter makes new lines and
 * Ctrl+Enter sends. Files are attached with the clip, by pasting or by dropping them; photos show in the conversation,
 * other files as downloads. While the person types, the other members see it.
 * @param props The props.
 * @returns The editor.
 */
export function Composer({ chatId, editing }: ComposerProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const user = useCurrentUser();
    const fileInput = useRef<HTMLInputElement>(null);
    const lastTyping = useRef(0);
    const [expanded, setExpanded] = useState(false);
    const [files, setFiles] = useState<readonly File[]>([]);
    const [linkOpen, setLinkOpen] = useState(false);
    const [link, setLink] = useState("");
    const [saving, setSaving] = useState(false);

    // The latest values for the editor's key handler, which Tiptap keeps from the first render.
    const submitRef = useRef<() => void>(() => undefined);
    const expandedRef = useRef(expanded);
    const filesRef = useRef(files);
    useEffect(() => {
        expandedRef.current = expanded;
        filesRef.current = files;
    });

    const editor = useEditor({
        content: editing?.html ?? "",
        immediatelyRender: true,
        extensions: [
            StarterKit.configure({
                heading: false,
                horizontalRule: false,
                link: {
                    openOnClick: false,
                    autolink: true,
                    defaultProtocol: "https",
                    protocols: ["http", "https", "mailto", "tel"],
                    HTMLAttributes: { rel: "noopener noreferrer nofollow", target: "_blank" },
                },
            }),
            Placeholder.configure({ placeholder: editing ? t`Текст повідомлення` : t`Напишіть повідомлення` }),
        ],
        editorProps: {
            attributes: {
                class: styles.input ?? "",
                spellcheck: "true",
                "aria-label": t`Повідомлення`,
                role: "textbox",
                "aria-multiline": "true",
            },
            handleKeyDown: (_view, event) => {
                if (event.key !== "Enter" || event.isComposing) {
                    return false;
                }

                if (event.ctrlKey || event.metaKey || (!event.shiftKey && !expandedRef.current)) {
                    event.preventDefault();
                    submitRef.current();
                    return true;
                }

                return false;
            },
            handlePaste: (_view, event) => addFiles(Array.from(event.clipboardData?.files ?? [])),
            handleDrop: (_view, event, _slice, moved) => !moved && addFiles(Array.from((event as DragEvent).dataTransfer?.files ?? [])),
        },
        onUpdate: ({ editor: current }) => {
            const now = Date.now();
            if (!editing && !current.isEmpty && now - lastTyping.current > typingInterval) {
                lastTyping.current = now;
                sendLive("Typing", chatId);
            }
        },
    });

    const state = useEditorState({
        editor,
        selector: ({ editor: current }: { editor: Editor }) => ({
            bold: current.isActive("bold"),
            italic: current.isActive("italic"),
            underline: current.isActive("underline"),
            strike: current.isActive("strike"),
            code: current.isActive("code"),
            codeBlock: current.isActive("codeBlock"),
            bulletList: current.isActive("bulletList"),
            orderedList: current.isActive("orderedList"),
            blockquote: current.isActive("blockquote"),
            link: current.isActive("link"),
            empty: current.isEmpty,
        }),
    });

    /**
     * Adds files to the message after checking their type, size and number.
     * @param picked The files.
     * @returns Whether there were files (pasting and dropping them is then handled here).
     */
    function addFiles(picked: readonly File[]): boolean {
        if (picked.length === 0 || editing) {
            return false;
        }

        const refused = picked.find((file) => !isAllowedFile(file));
        const tooLarge = picked.find((file) => file.size > maxFileSize);
        const next = [...filesRef.current, ...picked.filter((file) => isAllowedFile(file) && file.size <= maxFileSize)];
        if (refused) {
            toast(t`Файли такого типу не можна надсилати в чат: ${refused.name}`, { danger: true });
        } else if (tooLarge) {
            toast(t`Файл більший за 25 МБ: ${tooLarge.name}`, { danger: true });
        }

        if (next.length > maxFiles) {
            toast(t`До повідомлення можна додати не більше ${maxFiles} файлів.`, { danger: true });
        } else if (next.reduce((sum, file) => sum + file.size, 0) > maxTotalSize) {
            toast(t`Файли одного повідомлення не можуть бути більшими за 50 МБ разом.`, { danger: true });
        } else {
            setFiles(next);
        }

        return true;
    }

    /**
     * Sends the message (or saves the edited one) and clears the editor at once; a failed message stays in the
     * conversation with a "send again" button.
     */
    function submit(): void {
        const body = editor.isEmpty ? "" : editor.getHTML();
        const text = editor.getText().trim();
        if (editing) {
            if (!text) {
                return;
            }

            setSaving(true);
            void editing
                .save(body)
                .catch((error: unknown) => toast(describeError(error, t`Не вдалося зберегти повідомлення.`), { danger: true }))
                .finally(() => setSaving(false));
            return;
        }

        if (!text && files.length === 0) {
            return;
        }

        const draft = { clientId: crypto.randomUUID(), body: text ? body : "", text, files };
        editor.commands.clearContent(true);
        setFiles([]);
        setLinkOpen(false);
        void sendDraft(queryClient, chatId, { id: user.id, fullName: user.fullName, photoId: user.photoId }, draft).catch(
            (error: unknown) => toast(describeError(error, t`Повідомлення не надіслано.`), { danger: true }),
        );
    }

    useEffect(() => {
        submitRef.current = submit;
    });

    /**
     * Applies the address of the link bar to the selection (an empty address removes the link).
     */
    function applyLink(): void {
        const href = link.trim();
        const chain = editor.chain().focus().extendMarkRange("link");
        if (href) {
            chain.setLink({ href: /^(https?:|mailto:|tel:)/i.test(href) ? href : `https://${href}` }).run();
        } else {
            chain.unsetLink().run();
        }

        setLinkOpen(false);
        setLink("");
    }

    return (
        <div className={styles.composer} data-editing={editing ? "" : undefined}>
            {expanded && (
                <div className={styles.toolbar} role="toolbar" aria-label={t`Форматування`}>
                    <Tool icon={Bold} name={t`Жирний`} pressed={state.bold} run={() => editor.chain().focus().toggleBold().run()} />
                    <Tool icon={Italic} name={t`Курсив`} pressed={state.italic} run={() => editor.chain().focus().toggleItalic().run()} />
                    <Tool
                        icon={Underline}
                        name={t`Підкреслений`}
                        pressed={state.underline}
                        run={() => editor.chain().focus().toggleUnderline().run()}
                    />
                    <Tool
                        icon={Strikethrough}
                        name={t`Закреслений`}
                        pressed={state.strike}
                        run={() => editor.chain().focus().toggleStrike().run()}
                    />
                    <span className={styles.toolGap} />
                    <Tool
                        icon={List}
                        name={t`Маркований список`}
                        pressed={state.bulletList}
                        run={() => editor.chain().focus().toggleBulletList().run()}
                    />
                    <Tool
                        icon={ListOrdered}
                        name={t`Нумерований список`}
                        pressed={state.orderedList}
                        run={() => editor.chain().focus().toggleOrderedList().run()}
                    />
                    <Tool
                        icon={Quote}
                        name={t`Цитата`}
                        pressed={state.blockquote}
                        run={() => editor.chain().focus().toggleBlockquote().run()}
                    />
                    <span className={styles.toolGap} />
                    <Tool icon={Code} name={t`Код у рядку`} pressed={state.code} run={() => editor.chain().focus().toggleCode().run()} />
                    <Tool
                        icon={SquareCode}
                        name={t`Блок коду`}
                        pressed={state.codeBlock}
                        run={() => editor.chain().focus().toggleCodeBlock().run()}
                    />
                    <Tool
                        icon={state.link ? Unlink : Link}
                        name={state.link ? t`Прибрати посилання` : t`Посилання`}
                        pressed={state.link}
                        run={() => {
                            if (state.link) {
                                editor.chain().focus().extendMarkRange("link").unsetLink().run();
                            } else {
                                setLink("");
                                setLinkOpen((open) => !open);
                            }
                        }}
                    />
                </div>
            )}

            {linkOpen && (
                <form
                    className={styles.linkBar}
                    onSubmit={(event) => {
                        event.preventDefault();
                        applyLink();
                    }}
                >
                    <TextInput
                        compact
                        autoFocus
                        value={link}
                        placeholder="https://"
                        aria-label={t`Адреса посилання`}
                        onChange={(event) => setLink(event.target.value)}
                    />
                    <Button type="submit" size="sm">
                        {t`Додати`}
                    </Button>
                    <Button type="button" variant="ghost" size="sm" onClick={() => setLinkOpen(false)}>
                        {t`Скасувати`}
                    </Button>
                </form>
            )}

            {files.length > 0 && (
                <ul className={styles.pendingFiles} aria-label={t`Вкладення`}>
                    {files.map((file, index) => (
                        <li key={`${file.name}-${index}`} className={styles.pendingFile}>
                            <Icon icon={FileText} size="sm" />
                            <span className={styles.fileName}>{file.name}</span>
                            <span className={styles.fileSize}>{formatFileSize(file.size)}</span>
                            <button
                                type="button"
                                className={styles.removeFile}
                                aria-label={t`Прибрати ${file.name}`}
                                onClick={() => setFiles(files.filter((_, i) => i !== index))}
                            >
                                <Icon icon={X} size="sm" />
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            <div className={styles.editorRow}>
                <EditorContent editor={editor} className={styles.editor} />
                <div className={styles.composerActions}>
                    <Tool
                        icon={Type}
                        name={expanded ? t`Сховати форматування` : t`Форматування`}
                        pressed={expanded}
                        run={() => setExpanded(!expanded)}
                    />
                    {!editing && (
                        <>
                            <Tool icon={Paperclip} name={t`Додати файли`} run={() => fileInput.current?.click()} />
                            <input
                                ref={fileInput}
                                type="file"
                                multiple
                                hidden
                                accept={fileAccept}
                                onChange={(event) => {
                                    addFiles(Array.from(event.target.files ?? []));
                                    event.target.value = "";
                                }}
                            />
                        </>
                    )}
                    {editing ? (
                        <>
                            <Button type="button" variant="ghost" size="sm" onClick={editing.cancel}>
                                {t`Скасувати`}
                            </Button>
                            <Button type="button" size="sm" busy={saving} disabled={state.empty} onClick={submit}>
                                {t`Зберегти`}
                            </Button>
                        </>
                    ) : (
                        <Button
                            type="button"
                            size="sm"
                            iconOnly
                            icon={SendHorizontal}
                            disabled={state.empty && files.length === 0}
                            onClick={submit}
                        >
                            {t`Надіслати`}
                        </Button>
                    )}
                </div>
            </div>
            {expanded && !editing && <p className={styles.composerHint}>{t`Ctrl+Enter — надіслати, Enter — новий рядок`}</p>}
        </div>
    );
}
