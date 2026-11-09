import { useLingui } from "@lingui/react/macro";
import type { SpreadsheetFormat } from "@/shared/api/schema";
import type { ButtonProps } from "@/shared/ui/Button";
import { DownloadButton, type DownloadMessages } from "./DownloadButton";

/** The formats of the export, in the order of the buttons. */
const formats: readonly SpreadsheetFormat[] = ["Xlsx", "Csv"];

/**
 * Props of {@link UserExportButtons}.
 */
export interface UserExportButtonsProps {
    /** The export address of a format (with the filters of the list). */
    readonly href: (format: SpreadsheetFormat) => string;
    /** The size of the buttons. */
    readonly size?: ButtonProps["size"];
    /** Whether the first button (XLSX) is the primary one. */
    readonly primaryFirst?: boolean;
}

/**
 * The XLSX and CSV export buttons of the user list: each reports the start and the end of the export with toasts.
 * @param props The props.
 * @returns The buttons.
 */
export function UserExportButtons({ href, size, primaryFirst = false }: UserExportButtonsProps) {
    const { t } = useLingui();
    const messages: DownloadMessages = {
        started: t`Експорт користувачів розпочато. Файл завантажиться, щойно буде готовий.`,
        done: (fileName) => t`Експорт завершено: файл ${fileName} завантажено.`,
        failed: t`Не вдалося експортувати користувачів. Спробуйте ще раз.`,
    };

    return formats.map((format, index) => (
        <DownloadButton
            key={format}
            href={href(format)}
            fileName={`users.${format.toLowerCase()}`}
            messages={messages}
            variant={primaryFirst && index === 0 ? "primary" : "secondary"}
            size={size}
        >
            {format.toUpperCase()}
        </DownloadButton>
    ));
}
