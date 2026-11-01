import { useLingui } from "@lingui/react/macro";
import { Paperclip, X } from "lucide-react";
import { useId } from "react";
import { fileAccept, isAllowedFile, maxFiles, maxFileSize, maxTotalSize } from "@/shared/lib/attachments";
import { formatFileSize } from "@/shared/i18n/format";
import { Icon } from "@/shared/ui/Icon";
import { toast } from "@/shared/ui/Toast";
import styles from "./tickets.module.css";

/**
 * Props of {@link FilePicker}.
 */
export interface FilePickerProps {
    /** The chosen files. */
    readonly files: readonly File[];
    /** Called with the new choice. */
    readonly onChange: (files: readonly File[]) => void;
    /** Blocks the choice (while sending). */
    readonly disabled?: boolean;
}

/**
 * Attaches files and screenshots to a request message: a button (or a drop on it) adds files, each chosen file shows
 * as a chip that removes it. Types and sizes are checked before uploading (the server checks them again).
 * @param props The props.
 * @returns The picker.
 */
export function FilePicker({ files, onChange, disabled }: FilePickerProps) {
    const { t } = useLingui();
    const inputId = useId();

    /**
     * Adds files that pass the checks and reports the others.
     * @param added The files to add.
     */
    function add(added: readonly File[]): void {
        const allowed = added.filter((file) => {
            if (!isAllowedFile(file)) {
                toast(t`Файл «${file.name}» має недозволений тип.`, { danger: true });
                return false;
            }

            if (file.size > maxFileSize) {
                toast(t`Файл «${file.name}» більший за ${formatFileSize(maxFileSize)}.`, { danger: true });
                return false;
            }

            return true;
        });
        const next = [...files, ...allowed];
        if (next.length > maxFiles) {
            toast(t`Можна додати не більше ${maxFiles} файлів.`, { danger: true });
            return;
        }

        if (next.reduce((sum, file) => sum + file.size, 0) > maxTotalSize) {
            toast(t`Файли разом не можуть бути більшими за ${formatFileSize(maxTotalSize)}.`, { danger: true });
            return;
        }

        onChange(next);
    }

    return (
        <div
            className={styles.picker}
            onDragOver={(event) => event.preventDefault()}
            onDrop={(event) => {
                event.preventDefault();
                if (!disabled) {
                    add(Array.from(event.dataTransfer.files));
                }
            }}
        >
            <label className={styles.pickerButton} htmlFor={inputId} aria-disabled={disabled || undefined}>
                <Icon icon={Paperclip} />
                <span>{t`Додати файли`}</span>
            </label>
            <input
                id={inputId}
                className={styles.pickerInput}
                type="file"
                multiple
                accept={fileAccept}
                disabled={disabled}
                onChange={(event) => {
                    add(Array.from(event.target.files ?? []));
                    event.target.value = "";
                }}
            />
            {files.length === 0 ? (
                <span
                    className={styles.pickerHint}
                >{t`Знімки екрана, документи, архіви — до ${maxFiles} файлів по ${formatFileSize(maxFileSize)}.`}</span>
            ) : (
                <ul className={styles.chips}>
                    {files.map((file, index) => (
                        <li key={`${file.name}-${file.size}-${index}`} className={styles.chip}>
                            <span className={styles.chipName}>{file.name}</span>
                            <span className={styles.chipSize}>{formatFileSize(file.size)}</span>
                            <button
                                type="button"
                                className={styles.chipRemove}
                                disabled={disabled}
                                aria-label={t`Прибрати файл ${file.name}`}
                                onClick={() => onChange(files.filter((_, at) => at !== index))}
                            >
                                <X aria-hidden="true" />
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
