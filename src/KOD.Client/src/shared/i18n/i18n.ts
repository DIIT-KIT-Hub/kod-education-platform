import { i18n, type Messages } from "@lingui/core";

/**
 * The interface languages. Ukrainian is the source language: its texts are the message ids.
 */
export const locales = ["uk", "en"] as const;

/**
 * An interface language.
 */
export type Locale = (typeof locales)[number];

/**
 * The language used when nothing else is known.
 */
export const defaultLocale: Locale = "uk";

/**
 * The native names of the languages (shown in the language switch).
 */
export const localeNames: Record<Locale, string> = { uk: "Українська", en: "English" };

/**
 * Loads the compiled catalog of a language on demand (the Lingui Vite plugin compiles the .po file on import).
 */
const catalogs: Record<Locale, () => Promise<{ messages: Messages }>> = {
    uk: () => import("@/locales/uk/messages.po"),
    en: () => import("@/locales/en/messages.po"),
};

/**
 * Narrows a value to a supported language.
 * @param value A language code from the server, the browser or a cookie.
 * @returns The language, or the default one.
 */
export function toLocale(value: string | null | undefined): Locale {
    return locales.find((locale) => locale === value) ?? defaultLocale;
}

/**
 * Activates a language: loads its catalog and marks the document.
 * @param locale The language.
 */
export async function activateLocale(locale: Locale): Promise<void> {
    const { messages } = await catalogs[locale]();
    i18n.load(locale, messages);
    i18n.activate(locale);
    document.documentElement.lang = locale;
}

export { i18n };
