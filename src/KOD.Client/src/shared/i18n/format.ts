import { i18n } from "@lingui/core";

/**
 * The time zone of the cabinet: dates are shown in Kyiv time wherever the user is.
 */
const timeZone = "Europe/Kyiv";

/**
 * The formatting locale of the interface language.
 * @returns The BCP 47 tag.
 */
function locale(): string {
    return i18n.locale === "en" ? "en-GB" : "uk-UA";
}

/**
 * Formats a moment as a local date (e.g. «25 жовтня 2026», «25 October 2026»).
 * @param moment The ISO moment.
 * @returns The text, or «—» when empty.
 */
export function formatDate(moment: string | null | undefined): string {
    if (!moment) {
        return "—";
    }

    // Ukrainian dates end with the year abbreviation ("р."); the cabinet writes them without it.
    return new Intl.DateTimeFormat(locale(), { timeZone, day: "numeric", month: "long", year: "numeric" })
        .formatToParts(new Date(moment))
        .filter((part) => part.type !== "literal" || !part.value.includes("р."))
        .map((part) => part.value)
        .join("");
}

/**
 * Formats a moment as a local date and time (e.g. «25.10.2026, 14:05», «25 Oct 2026, 14:05»).
 * @param moment The ISO moment.
 * @returns The text, or «—» when empty.
 */
export function formatDateTime(moment: string | null | undefined): string {
    if (!moment) {
        return "—";
    }

    const options: Intl.DateTimeFormatOptions =
        i18n.locale === "en"
            ? { timeZone, day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23" }
            : { timeZone, day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23" };
    return new Intl.DateTimeFormat(locale(), options).format(new Date(moment));
}

/**
 * Formats a whole number in the interface language.
 * @param value The number.
 * @returns The text.
 */
export function formatNumber(value: number): string {
    return new Intl.NumberFormat(locale()).format(value);
}

/**
 * The hour of a moment in Kyiv time (greetings).
 * @param moment The moment.
 * @returns The hour, 0–23.
 */
export function kyivHour(moment: Date): number {
    return Number(new Intl.DateTimeFormat("en-GB", { timeZone, hour: "2-digit", hourCycle: "h23" }).format(moment));
}

/**
 * Formats a size in bytes («1,4 МБ», «1.4 MB»).
 * @param bytes The size.
 * @returns The text.
 */
export function formatFileSize(bytes: number): string {
    const units = i18n.locale === "en" ? ["B", "KB", "MB"] : ["Б", "КБ", "МБ"];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024;
        unit++;
    }

    return `${new Intl.NumberFormat(locale(), { maximumFractionDigits: unit === 0 ? 0 : 1 }).format(value)} ${units[unit]}`;
}
