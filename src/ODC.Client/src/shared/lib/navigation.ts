/**
 * Accepts a return address only when it is a path of this application (no other origin, no protocol-relative or
 * backslash tricks), so a crafted sign-in link cannot send the user elsewhere.
 * @param value The return address from the query string.
 * @param fallback The address used otherwise.
 * @returns A safe application path.
 */
export function safeReturnUrl(value: string | null | undefined, fallback = "/"): string {
    // Browsers drop tabs and line breaks from addresses and read "\" as "/": "/\t/evil.example" or "/\evil.example"
    // would become the protocol-relative "//evil.example". Control characters are never part of an application path.
    // eslint-disable-next-line no-control-regex
    if (!value || /[\u0000-\u001f\u007f]/u.test(value)) {
        return fallback;
    }

    const normalized = value.replaceAll("\\", "/");
    if (!normalized.startsWith("/") || normalized.startsWith("//")) {
        return fallback;
    }

    return value;
}
