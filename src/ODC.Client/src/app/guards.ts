import { data, redirect, type LoaderFunctionArgs } from "react-router";
import { authContextQuery, hasPermission } from "@/shared/session/session";
import { queryClient } from "./queryClient";

/**
 * The address of the requested page, for `returnUrl`.
 * @param request The navigation request.
 * @returns The path with the query string.
 */
function requestedPath(request: Request): string {
    const url = new URL(request.url);
    return url.pathname + url.search;
}

/**
 * An address with the page to come back to.
 * @param path The page to go to.
 * @param request The navigation request.
 * @returns The address.
 */
function withReturn(path: string, request: Request): string {
    const back = requestedPath(request);
    return back === "/" ? path : `${path}?returnUrl=${encodeURIComponent(back)}`;
}

/**
 * The pages of the cabinet: a signed-in user with a profile photo.
 * @param args The loader arguments.
 * @returns Nothing; redirects otherwise.
 */
export async function requireReadyUser({ request }: LoaderFunctionArgs): Promise<null> {
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (!user) {
        throw redirect(withReturn("/account/login", request));
    }

    if (!user.photoId) {
        throw redirect(withReturn("/account/photo", request));
    }

    return null;
}

/**
 * Account pages of a signed-in user (password, photo).
 * @param args The loader arguments.
 * @returns Nothing; redirects to the sign-in otherwise.
 */
export async function requireUser({ request }: LoaderFunctionArgs): Promise<null> {
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (!user) {
        throw redirect(withReturn("/account/login", request));
    }

    return null;
}

/**
 * The account activation: anonymous visitors (and the sign-in it starts); a signed-in user continues in the cabinet,
 * or on the photo page while the photo is missing.
 * @returns Nothing; redirects a signed-in user.
 */
export async function requireActivation(): Promise<null> {
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (user) {
        throw redirect(user.photoId ? "/" : "/account/photo");
    }

    return null;
}

/**
 * The sign-in steps: a signed-in user goes to the cabinet.
 * @returns Nothing; redirects a signed-in user.
 */
export async function requireAnonymous(): Promise<null> {
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (user) {
        throw redirect("/");
    }

    return null;
}

/**
 * A page that needs one of the permissions: without them the route shows "access restricted" (403).
 * @param permissions The permission codes (any of them is enough).
 * @returns The loader.
 */
export function requirePermission(...permissions: string[]) {
    return async (): Promise<null> => {
        const { user } = await queryClient.ensureQueryData(authContextQuery);
        if (!user || !permissions.some((permission) => hasPermission(user, permission))) {
            throw data(null, { status: 403 });
        }

        return null;
    };
}
