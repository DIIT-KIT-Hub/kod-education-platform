import { redirect, type LoaderFunctionArgs } from "react-router";
import { authContextQuery } from "@/shared/session/session";
import { queryClient } from "./queryClient";

/**
 * The base path of the application (`/app`); router paths are relative to it.
 */
export const basename = import.meta.env.BASE_URL.replace(/\/$/, "");

/**
 * The address of the requested page relative to the application, for `returnUrl`.
 * @param request The navigation request.
 * @returns The path with the query string.
 */
function requestedPath(request: Request): string {
    const url = new URL(request.url);
    const path = url.pathname.startsWith(basename) ? url.pathname.slice(basename.length) || "/" : url.pathname;
    return path + url.search;
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
 * The pages of the cabinet: a signed-in user with their own password and a profile photo.
 * @param args The loader arguments.
 * @returns Nothing; redirects otherwise.
 */
export async function requireReadyUser({ request }: LoaderFunctionArgs): Promise<null> {
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (!user) {
        throw redirect(withReturn("/account/login", request));
    }

    if (user.mustChangePassword) {
        throw redirect(withReturn("/account/change-password", request));
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
 * The photo page: the temporary password is replaced first.
 * @param args The loader arguments.
 * @returns Nothing; redirects otherwise.
 */
export async function requirePasswordSet(args: LoaderFunctionArgs): Promise<null> {
    await requireUser(args);
    const { user } = await queryClient.ensureQueryData(authContextQuery);
    if (user?.mustChangePassword) {
        throw redirect(withReturn("/account/change-password", args.request));
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
