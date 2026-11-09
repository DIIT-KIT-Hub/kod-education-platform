import { i18n } from "@lingui/core";
import { msg } from "@lingui/core/macro";
import { isCancelledError, QueryCache } from "@tanstack/react-query";
import { toast } from "@/shared/ui/Toast";
import { ApiError, endsSignIn } from "./problem";

/**
 * How a query reports a failed load: `toast` (the default) shows the reason as a toast; `refusal-is-content` does the
 * same except for an answer the server refused with on purpose (4xx: not found, not yours, cannot be changed), which
 * the page shows as its own state; `quiet` leaves the failure to the page (the sign-in context, whose failure is the
 * route error page).
 */
export type LoadErrorMode = "toast" | "refusal-is-content" | "quiet";

/**
 * The `meta` of the queries (registered with TanStack Query below).
 */
export type QueryMeta = {
    /** How a failed load is reported. */
    readonly loadErrors?: LoadErrorMode;
};

declare module "@tanstack/react-query" {
    interface Register {
        queryMeta: QueryMeta;
    }
}

/** The message when the server could not be reached at all. */
const unreachable = msg`Не вдалося зв'язатися з сервером. Перевірте з'єднання й спробуйте ще раз.`;

/** The message when the server failed without saying why. */
const failed = msg`Не вдалося завантажити дані. Спробуйте ще раз.`;

/**
 * Tells whether the server refused on purpose (the item does not exist, is not the user's, cannot be used now) rather
 * than failed. A 401 is not a refusal: it ends the sign-in or asks to sign in again.
 * @param error The error of a query.
 * @returns True for 4xx answers other than 401.
 */
export function isRefusal(error: unknown): error is ApiError {
    return error instanceof ApiError && error.status >= 400 && error.status < 500 && error.status !== 401;
}

/**
 * The text of a failed load: the server's own message when it sent one, else what went wrong in general.
 * @param error The error of a query.
 * @returns The translated text.
 */
export function loadErrorMessage(error: unknown): string {
    if (error instanceof ApiError) {
        return error.detail || i18n._(failed);
    }

    return i18n._(unreachable);
}

/**
 * Reports a failed load as a toast. A 401 that ended the sign-in is skipped: the API client already leads to the
 * sign-in page (a 401 of a sign-in step carries its own message and is shown).
 * @param error The error of the query.
 * @param meta The query's meta.
 */
export function reportLoadError(error: unknown, meta: QueryMeta | undefined): void {
    const mode = meta?.loadErrors ?? "toast";
    if (mode === "quiet" || isCancelledError(error) || (error instanceof ApiError && endsSignIn(error.status, error.path))) {
        return;
    }

    if (mode === "refusal-is-content" && isRefusal(error)) {
        return;
    }

    toast(loadErrorMessage(error), { tone: "error" });
}

/**
 * The query cache of the application: every query that fails (after its retries) is reported once as a toast, so
 * pages keep their frame instead of showing an error block.
 * @returns The cache.
 */
export function createQueryCache(): QueryCache {
    return new QueryCache({ onError: (error, query) => reportLoadError(error, query.meta) });
}
