import type { LucideIcon } from "lucide-react";
import { useState, type ReactNode } from "react";
import styles from "./FileDrop.module.css";
import { Icon } from "./Icon";

/**
 * Props of {@link FileDrop}.
 */
export interface FileDropProps {
    /** The icon. */
    readonly icon: LucideIcon;
    /** The main line (the chosen file names once chosen). */
    readonly title: ReactNode;
    /** The allowed types and sizes. */
    readonly hint?: ReactNode;
    /** The accepted types. */
    readonly accept?: string;
    /** Allows several files. */
    readonly multiple?: boolean;
    /** Blocks the input. */
    readonly disabled?: boolean;
    /** The input id (a field label may point to it). */
    readonly id?: string;
    /** Called with the chosen files. */
    readonly onFiles: (files: readonly File[]) => void;
}

/**
 * A large area to choose files by click or by dropping them on it.
 * @param props The props.
 * @returns The drop zone.
 */
export function FileDrop({ icon, title, hint, accept, multiple, disabled, id, onFiles }: FileDropProps) {
    const [over, setOver] = useState(false);
    return (
        <label
            className={styles.drop}
            data-dragover={over || undefined}
            onDragEnter={() => setOver(true)}
            onDragLeave={() => setOver(false)}
            onDrop={() => setOver(false)}
        >
            <input
                id={id}
                type="file"
                accept={accept}
                multiple={multiple}
                disabled={disabled}
                onChange={(event) => {
                    onFiles(Array.from(event.target.files ?? []));
                    // The same file can be chosen again after a failed upload.
                    event.target.value = "";
                }}
            />
            <Icon icon={icon} />
            <span className={styles.title}>{title}</span>
            {hint && <span className={styles.hint}>{hint}</span>}
        </label>
    );
}
