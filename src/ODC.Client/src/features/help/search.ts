/**
 * The searchable text of one manual page.
 */
export interface SearchDocument {
    /** The topic. */
    readonly id: string;
    /** The title of the chapter. */
    readonly chapter: string;
    /** The title of the page. */
    readonly title: string;
    /** The subheadings with their anchors. */
    readonly headings: readonly { readonly id: string; readonly text: string }[];
    /** The whole text of the page. */
    readonly text: string;
}

/**
 * A page that matches a search, with the place to show.
 */
export interface SearchHit {
    /** The topic. */
    readonly id: string;
    /** The title of the chapter. */
    readonly chapter: string;
    /** The title of the page. */
    readonly title: string;
    /** The first subheading that matches, if any (the result leads there). */
    readonly heading?: { readonly id: string; readonly text: string };
    /** The text around the first match. */
    readonly snippet: { readonly before: string; readonly match: string; readonly after: string } | null;
    /** The relevance (higher first). */
    readonly score: number;
}

/**
 * How many characters of context a snippet shows on each side of the match.
 */
const contextLength = 70;

/**
 * Brings text to a comparable form: lower case, one kind of apostrophe, single spaces.
 * @param text The text.
 * @param locale The language.
 * @returns The normalized text (the same length as the input, so positions stay valid).
 */
export function normalize(text: string, locale: string): string {
    return text.toLocaleLowerCase(locale).replace(/[’ʼ`]/gu, "'").replace(/\s/gu, " ");
}

/**
 * Splits a query into the words every result must contain.
 * @param query The query.
 * @param locale The language.
 * @returns The words (at least two letters each).
 */
export function termsOf(query: string, locale: string): string[] {
    return normalize(query, locale)
        .split(" ")
        .map((term) => term.trim())
        .filter((term) => term.length >= 2);
}

/**
 * Builds a text fragment around the first occurrence of a word.
 * @param text The original text.
 * @param normalized The normalized text.
 * @param term The word.
 * @returns The fragment, or null when the text lacks the word.
 */
function snippetOf(text: string, normalized: string, term: string): SearchHit["snippet"] {
    const index = normalized.indexOf(term);
    if (index < 0) {
        return null;
    }

    const start = Math.max(0, index - contextLength);
    const end = Math.min(text.length, index + term.length + contextLength);
    return {
        before: (start > 0 ? "…" : "") + text.slice(start, index).trimStart(),
        match: text.slice(index, index + term.length),
        after: text.slice(index + term.length, end).trimEnd() + (end < text.length ? "…" : ""),
    };
}

/**
 * Finds the pages that contain every word of the query, best first: a match in the title counts most, then in a
 * subheading, then in the text.
 * @param documents The pages.
 * @param query The query.
 * @param locale The language.
 * @param limit The most results to return.
 * @returns The results.
 */
export function searchManual(documents: readonly SearchDocument[], query: string, locale: string, limit = 30): SearchHit[] {
    const terms = termsOf(query, locale);
    if (terms.length === 0) {
        return [];
    }

    const hits: SearchHit[] = [];
    for (const document of documents) {
        const title = normalize(document.title, locale);
        const chapter = normalize(document.chapter, locale);
        const text = normalize(document.text, locale);
        const headings = document.headings.map((heading) => ({ ...heading, normalized: normalize(heading.text, locale) }));
        const everywhere = `${title} ${chapter} ${headings.map((heading) => heading.normalized).join(" ")} ${text}`;
        if (!terms.every((term) => everywhere.includes(term))) {
            continue;
        }

        let score = 0;
        for (const term of terms) {
            if (title.includes(term)) {
                score += 10;
            }

            if (headings.some((heading) => heading.normalized.includes(term))) {
                score += 5;
            }

            if (chapter.includes(term)) {
                score += 2;
            }

            if (text.includes(term)) {
                score += 1;
            }
        }

        if (terms.every((term) => title.includes(term))) {
            score += 50;
        }

        const heading =
            headings.find((item) => terms.every((term) => item.normalized.includes(term))) ??
            headings.find((item) => item.normalized.includes(terms[0] ?? ""));
        hits.push({
            id: document.id,
            chapter: document.chapter,
            title: document.title,
            heading: heading ? { id: heading.id, text: heading.text } : undefined,
            snippet: snippetOf(document.text, text, terms[0] ?? ""),
            score,
        });
    }

    return hits.sort((a, b) => b.score - a.score).slice(0, limit);
}

/**
 * The anchor of a subheading: its text in lower case with dashes, letters of any alphabet kept.
 * @param text The heading.
 * @returns The anchor.
 */
export function headingId(text: string): string {
    return (
        text
            .toLocaleLowerCase()
            .replace(/[^\p{L}\p{N}]+/gu, "-")
            .replace(/^-+|-+$/gu, "") || "section"
    );
}
