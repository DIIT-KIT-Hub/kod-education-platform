import { useLingui } from "@lingui/react/macro";
import { Download, FileArchive, FileAudio, FileImage, FileText, FileVideo, type LucideIcon } from "lucide-react";
import { useState } from "react";
import type { ChatAttachmentDto } from "@/shared/api/schema";
import { formatFileSize } from "@/shared/i18n/format";
import { ButtonLink } from "@/shared/ui/Button";
import { Dialog } from "@/shared/ui/Dialog";
import { Icon } from "@/shared/ui/Icon";
import styles from "./chats.module.css";

/**
 * Picks the icon of a file by its media type.
 * @param contentType The media type.
 * @returns The icon.
 */
function fileIcon(contentType: string): LucideIcon {
    if (contentType.startsWith("image/")) {
        return FileImage;
    }

    if (contentType.startsWith("audio/")) {
        return FileAudio;
    }

    if (contentType.startsWith("video/")) {
        return FileVideo;
    }

    return /zip|rar|7z/.test(contentType) ? FileArchive : FileText;
}

/**
 * Props of {@link Attachments}.
 */
export interface AttachmentsProps {
    /** The attachments of a message. */
    readonly items: readonly ChatAttachmentDto[];
}

/**
 * The attachments of a message: photos as tiles that open in full size, other files as cards to download (files are
 * always downloaded, never opened in the page).
 * @param props The props.
 * @returns The attachments.
 */
export function Attachments({ items }: AttachmentsProps) {
    const { t } = useLingui();
    const [shown, setShown] = useState<ChatAttachmentDto | null>(null);
    const photos = items.filter((item) => item.kind === "Image");
    const files = items.filter((item) => item.kind === "File");

    return (
        <>
            {photos.length > 0 && (
                <div className={styles.photos} data-count={Math.min(photos.length, 4)}>
                    {photos.map((photo) => (
                        <button
                            key={photo.id}
                            type="button"
                            className={styles.photo}
                            aria-label={t`Відкрити фото ${photo.fileName}`}
                            onClick={() => setShown(photo)}
                        >
                            <img
                                src={photo.thumbnailUrl ?? photo.url}
                                alt={photo.fileName}
                                width={photo.width ?? undefined}
                                height={photo.height ?? undefined}
                                loading="lazy"
                                decoding="async"
                            />
                        </button>
                    ))}
                </div>
            )}

            {files.length > 0 && (
                <ul className={styles.files}>
                    {files.map((file) => (
                        <li key={file.id}>
                            <a className={styles.file} href={file.url} download={file.fileName}>
                                <span className={styles.fileIcon}>
                                    <Icon icon={fileIcon(file.contentType)} />
                                </span>
                                <span className={styles.fileText}>
                                    <span className={styles.fileName}>{file.fileName}</span>
                                    <span className={styles.fileSize}>{formatFileSize(file.size)}</span>
                                </span>
                                <Icon icon={Download} size="sm" className={styles.fileDownload} />
                            </a>
                        </li>
                    ))}
                </ul>
            )}

            <Dialog
                open={shown !== null}
                onClose={() => setShown(null)}
                title={shown?.fileName ?? ""}
                wide
                actions={
                    shown && (
                        <ButtonLink href={shown.url} download={shown.fileName} variant="secondary" icon={Download}>
                            {t`Завантажити`}
                        </ButtonLink>
                    )
                }
            >
                {shown && (
                    <img
                        className={styles.viewerImage}
                        src={shown.url}
                        alt={shown.fileName}
                        width={shown.width ?? undefined}
                        height={shown.height ?? undefined}
                    />
                )}
            </Dialog>
        </>
    );
}
