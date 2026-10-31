/**
 * Accepts a return address only when it is a path of this application (no other origin, no protocol-relative or
 * backslash tricks), so a crafted sign-in link cannot send the user elsewhere.
 * @param value The return address from the query string.
 * @param fallback The address used otherwise.
 * @returns A safe application path.
 */
export function safeReturnUrl(value: string | null | undefined, fallback = "/"): string {
    if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) {
        return fallback;
    }

    return value;
}
