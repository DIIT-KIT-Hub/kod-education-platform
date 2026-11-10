import createClient, { type Middleware } from "openapi-fetch";
import { organizationBase } from "./organization";
import { endsSignIn } from "./problem";
import type { paths } from "./schema";

/**
 * The antiforgery token of the current identity (from GET /api/v1/auth/context). The JWTs themselves live in HttpOnly
 * cookies the browser sends on its own; scripts never see them.
 */
let antiforgeryToken = "";

/**
 * Stores the antiforgery token to send with state-changing requests.
 * @param token The token.
 */
export function setAntiforgeryToken(token: string): void {
    antiforgeryToken = token;
}

/**
 * Listeners told when the server answers 401 to a signed-in request (the sign-in ended: signed out elsewhere, blocked,
 * expired).
 */
const unauthorizedListeners = new Set<() => void>();

/**
 * Subscribes to sign-ins that ended while the application was open.
 * @param listener Called on a 401 response.
 * @returns The unsubscribe function.
 */
export function onUnauthorized(listener: () => void): () => void {
    unauthorizedListeners.add(listener);
    return () => unauthorizedListeners.delete(listener);
}

/**
 * Adds the antiforgery header to state-changing requests and reports ended sign-ins.
 */
const security: Middleware = {
    onRequest({ request }) {
        if (request.method !== "GET" && request.method !== "HEAD" && antiforgeryToken) {
            request.headers.set("X-CSRF-TOKEN", antiforgeryToken);
        }

        return request;
    },
    onResponse({ request, response }) {
        if (endsSignIn(response.status, new URL(request.url).pathname)) {
            unauthorizedListeners.forEach((listener) => listener());
        }

        return response;
    },
};

/**
 * The typed REST client (paths and payloads generated from the OpenAPI document by `pnpm api:types`). Requests go to the
 * organization of the page (`/o/{code}/api/v1/…`); a call about another organization passes its own `baseUrl`
 * (`organizationApi`).
 */
export const api = createClient<paths>({ baseUrl: window.location.origin + organizationBase, credentials: "same-origin" });

/**
 * The base address of the API calls about another organization (an item of a list gathered from several).
 * @param code The organization's code.
 * @returns The base address for the `baseUrl` option of a call.
 */
export function organizationApi(code: string): string {
    return `${window.location.origin}/o/${code}`;
}
api.use(security);
