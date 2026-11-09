import { useEffect } from "react";
import { useLocation, useNavigate, type Location, type To } from "react-router";

/**
 * Options of a view state change.
 */
export interface ViewParamsOptions {
    /** Replaces the current history entry instead of adding one. */
    readonly replace?: boolean;
    /** Keeps the scroll position. */
    readonly preventScrollReset?: boolean;
}

/**
 * The target of a link with a view of the page (for `<Link {...target}>`).
 */
export interface ViewLink {
    /** The address without a query string. */
    readonly to: To;
    /** The history state carrying the view, if any. */
    readonly state?: Record<string, unknown>;
    /**
     * Always false: a link to another view of the same page adds a history entry, so Back returns to the previous view
     * (`<Link>` would otherwise replace the entry, its address being the current one).
     */
    readonly replace: false;
}

/**
 * The state of a history entry: what other code put there plus the view of the page.
 */
type HistoryState = Record<string, unknown> & { readonly view?: string };

/**
 * Reads the state of a history entry as an object.
 * @param location The location.
 * @returns The state (empty when none).
 */
function historyState(location: Location): HistoryState {
    return typeof location.state === "object" && location.state !== null ? (location.state as HistoryState) : {};
}

/**
 * Splits an application address into the route and the view of the page: the query string goes into the history state
 * (see {@link useViewParams}), so `/documents?scope=AwaitingMe` opens `/documents` with that view. Link components
 * (`Tab`, `ButtonLink`, `StatCard`, breadcrumbs) pass their `to` through it.
 * @param address The address, possibly with a query string and a hash.
 * @returns The link target.
 */
export function viewTarget(address: string): ViewLink {
    const hashAt = address.indexOf("#");
    const hash = hashAt < 0 ? "" : address.slice(hashAt);
    const rest = hashAt < 0 ? address : address.slice(0, hashAt);
    const queryAt = rest.indexOf("?");
    if (queryAt < 0) {
        return { to: address, replace: false };
    }

    // An address that is only a query string ("?scope=…") stays on the current page.
    const to = queryAt === 0 ? { hash } : { pathname: rest.slice(0, queryAt), hash };
    return { to, state: { view: rest.slice(queryAt + 1) }, replace: false };
}

/**
 * The view state of a page (search, filters, sorting, paging) kept in the history entry instead of the address: the
 * address bar shows only the route of the module, while back/forward and a reload still restore the view (the browser
 * keeps `history.state` with each entry). Works like `useSearchParams`. An address that still carries a query string (an
 * older bookmark, a link from another page such as a dashboard counter) is read once and moved into the history state.
 * @returns The parameters, a setter and a builder of link targets.
 */
export function useViewParams() {
    const location = useLocation();
    const navigate = useNavigate();
    const state = historyState(location);
    const params = new URLSearchParams(location.search || state.view || "");

    // Move a query string of the address into the history state; the page already reads it from the address.
    useEffect(() => {
        if (!location.search) {
            return;
        }

        void navigate(
            { pathname: location.pathname, hash: location.hash },
            { replace: true, preventScrollReset: true, state: { ...historyState(location), view: location.search.slice(1) } },
        );
    }, [location, navigate]);

    /**
     * The target of a link to another view of this page.
     * @param next The view parameters.
     * @returns The address and the history state.
     */
    function link(next: URLSearchParams): ViewLink {
        return { to: { pathname: location.pathname }, state: { ...state, view: next.toString() }, replace: false };
    }

    /**
     * Changes the view of the page.
     * @param next The new parameters (as for `useSearchParams`), or a function of the current ones.
     * @param options History options.
     */
    function setParams(
        next: URLSearchParams | Record<string, string> | ((current: URLSearchParams) => URLSearchParams),
        options: ViewParamsOptions = {},
    ): void {
        const value = typeof next === "function" ? next(new URLSearchParams(params)) : new URLSearchParams(next);
        const target = link(value);
        void navigate(target.to, {
            replace: options.replace ?? false,
            preventScrollReset: options.preventScrollReset,
            state: target.state,
        });
    }

    return [params, setParams, link] as const;
}
