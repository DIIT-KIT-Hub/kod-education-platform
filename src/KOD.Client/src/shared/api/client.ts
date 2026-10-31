import createClient, { type Middleware } from "openapi-fetch";
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
        if (response.status === 401 && !new URL(request.url).pathname.startsWith("/api/v1/auth/")) {
            unauthorizedListeners.forEach((listener) => listener());
        }

        return response;
    },
};

/**
 * The typed REST client (paths and payloads generated from the OpenAPI document by `pnpm api:types`).
 */
export const api = createClient<paths>({ baseUrl: window.location.origin, credentials: "same-origin" });
api.use(security);
