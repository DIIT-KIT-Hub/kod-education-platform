import { useLingui } from "@lingui/react/macro";
import { Download, FileArchive, FileImage, FileText, type LucideIcon } from "lucide-react";
import { useState } from "react";
import type { TicketAttachmentDto } from "@/shared/api/schema";
import { formatFileSize } from "@/shared/i18n/format";
import { ButtonLink } from "@/shared/ui/Button";
import { Dialog } from "@/shared/ui/Dialog";
import { Icon } from "@/shared/ui/Icon";
import styles from "./tickets.module.css";

/**
 * Picks the icon of a file by its media type.
 * @param contentType The media type.
 * @returns The icon.
 */
function fileIcon(contentType: string): LucideIcon {
    if (contentType.startsWith("image/")) {
        return FileImage;
    }

    return /zip|rar|7z/.test(contentType) ? FileArchive : FileText;
}

/**
 * The files of a request message: screenshots as tiles that open in full size, other files as cards to download
 * (files are always downloaded, never opened in the page).
 * @param props The files.
 * @returns The files.
 */
export function TicketAttachments({ items }: { readonly items: readonly TicketAttachmentDto[] }) {
    const { t } = useLingui();
    const [shown, setShown] = useState<TicketAttachmentDto | null>(null);
    const photos = items.filter((item) => item.isImage);
    const files = items.filter((item) => !item.isImage);

    return (
        <>
            {photos.length > 0 && (
                <div className={styles.photos}>
                    {photos.map((photo) => (
                        <button
                            key={photo.id}
                            type="button"
                            className={styles.photo}
                            aria-label={t`Відкрити фото ${photo.fileName}`}
                            onClick={() => setShown(photo)}
                        >
                            <img src={photo.thumbnailUrl ?? photo.url} alt={photo.fileName} loading="lazy" decoding="async" />
                        </button>
                    ))}
                </div>
            )}

            {files.length > 0 && (
                <ul className={styles.files}>
                    {files.map((file) => (
                        <li key={file.id}>
                            <a className={styles.file} href={file.url} download={file.fileName}>
                                <Icon icon={fileIcon(file.contentType)} />
                                <span className={styles.fileText}>
                                    <span className={styles.fileName}>{file.fileName}</span>
                                    <span className={styles.fileSize}>{formatFileSize(file.size)}</span>
                                </span>
                                <Icon icon={Download} size="sm" />
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
