import type { QueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import { safeReturnUrl } from "@/shared/lib/navigation";
import { reloadAuthContext } from "@/shared/session/session";

/**
 * The page the user wanted before signing in (`returnUrl` in the query string), checked to be a path of this
 * application.
 * @returns The path.
 */
export function useReturnUrl(): string {
    const [params] = useSearchParams();
    return safeReturnUrl(params.get("returnUrl"));
}

/**
 * Whether the user asked to stay signed in on this device (`rememberMe=1`, carried through the sign-in steps).
 * @returns True when asked.
 */
export function useRememberMe(): boolean {
    const [params] = useSearchParams();
    return params.get("rememberMe") === "1";
}

/**
 * Builds the address of the next sign-in step, keeping the return address and the "remember me" choice.
 * @param path The step.
 * @param returnUrl The page the user wanted.
 * @param rememberMe Whether to stay signed in.
 * @returns The address.
 */
export function flowUrl(path: string, returnUrl: string, rememberMe: boolean): string {
    const query = new URLSearchParams();
    if (returnUrl !== "/") {
        query.set("returnUrl", returnUrl);
    }

    if (rememberMe) {
        query.set("rememberMe", "1");
    }

    const search = query.toString();
    return search ? `${path}?${search}` : path;
}

/**
 * Finishes a sign-in: loads the new context (user, language, antiforgery token of the signed-in identity).
 * @param queryClient The query client.
 */
export async function completeSignIn(queryClient: QueryClient): Promise<void> {
    await reloadAuthContext(queryClient);
}
