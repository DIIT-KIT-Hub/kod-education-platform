/**
 * The organization of the page. Every organization has a database of its own; the server serves its pages and API under
 * `/o/{code}/…` (a plain address is the user's default organization). The application reads the code from the address
 * once at start: the router uses it as its base, the API and the live connection go to the same organization, and
 * switching organizations loads the application again at the other address (a fresh cache for another database).
 */

/**
 * An organization code in an address (latin letters, digits and dashes, up to 32).
 */
const codePattern = /^\/o\/([A-Za-z0-9-]{1,32})(?=\/|$)/;

/**
 * Reads the organization code from an address.
 * @param pathname The path of the address.
 * @returns The code, or null for the default organization.
 */
export function organizationCodeOf(pathname: string): string | null {
    return codePattern.exec(pathname)?.[1] ?? null;
}

/**
 * The code of the page's organization in its address, or null for the user's default organization.
 */
export const pageOrganizationCode = organizationCodeOf(window.location.pathname);

/**
 * The base of the page's addresses: `/o/{code}`, or empty for the default organization.
 */
export const organizationBase = pageOrganizationCode ? `/o/${pageOrganizationCode}` : "";

/**
 * The address of a path in an organization.
 * @param code The organization's code (null: the page's organization).
 * @param path A path of the application or the API (`/documents/…`, `/api/v1/…`).
 * @returns The address.
 */
export function inOrganization(code: string | null | undefined, path: string): string {
    return code ? `/o/${code}${path}` : `${organizationBase}${path}`;
}

/**
 * The address of an API path in the page's organization (links and downloads the browser fetches itself).
 * @param path The API path (`/api/v1/…`).
 * @returns The address.
 */
export function apiUrl(path: string): string {
    return `${organizationBase}${path}`;
}

/**
 * Whether something of another organization is shown (it opens with a full load of that organization's address).
 * @param code The organization's code of the item.
 * @param current The code of the page's organization (from the auth context).
 * @returns True when the item lives elsewhere.
 */
export function isElsewhere(code: string | null | undefined, current: string | null | undefined): boolean {
    return !!code && !!current && code.toLowerCase() !== current.toLowerCase();
}

/**
 * Where a link to an item of a list gathered from several organizations points: a route of the application for an item
 * of the page's organization, a full address of the other organization otherwise.
 * @param code The organization's code of the item.
 * @param current The code of the page's organization.
 * @param path The application path (`/documents/…`).
 * @returns `to` for a router link or `href` for a plain one.
 */
export function linkTo(code: string | null | undefined, current: string | null | undefined, path: string): { to?: string; href?: string } {
    return isElsewhere(code, current) ? { href: `/o/${code}${path}` } : { to: path };
}

/**
 * Opens a page of another organization: a full load at its address (the application starts again there).
 * @param code The organization's code.
 * @param path The application path (`/documents/…`).
 */
export function openInOrganization(code: string, path: string): void {
    window.location.assign(`/o/${code}${path}`);
}
