import { afterEach, describe, expect, it } from "vitest";
import { authContextQuery, Permissions } from "@/shared/session/session";
import { makeAuthContext, makeUser, type Schemas } from "@/test/factories";
import { requireActivation, requireAnonymous, requirePermission, requireReadyUser, requireUser } from "./guards";
import { queryClient } from "./queryClient";

/**
 * Puts the auth context of a user (or a visitor) into the app's query cache.
 * @param user The user, or null for a visitor.
 */
function signedIn(user: Schemas["CurrentUserResponse"] | null) {
    queryClient.setQueryData(authContextQuery.queryKey, makeAuthContext(user));
}

/**
 * The loader arguments for an address.
 * @param path The requested path with its query.
 * @returns The arguments.
 */
function args(path: string) {
    return { request: new Request(`https://localhost${path}`), params: {}, context: undefined } as unknown as Parameters<
        typeof requireUser
    >[0];
}

/**
 * Runs a guard and returns where it redirected to.
 * @param run The guard call.
 * @returns The redirect address, or null when the guard let the route through.
 */
async function redirectOf(run: () => Promise<null>): Promise<string | null> {
    try {
        await run();
        return null;
    } catch (thrown) {
        expect(thrown).toBeInstanceOf(Response);
        const response = thrown as Response;
        expect(response.status).toBe(302);
        return response.headers.get("Location");
    }
}

afterEach(() => queryClient.clear());

describe("requireReadyUser", () => {
    it("lets a signed-in user with a photo through", async () => {
        signedIn(makeUser());

        expect(await redirectOf(() => requireReadyUser(args("/documents")))).toBeNull();
    });

    it("sends a visitor to the sign-in page with the address to come back to", async () => {
        signedIn(null);

        expect(await redirectOf(() => requireReadyUser(args("/documents/42?tab=route")))).toBe(
            "/account/login?returnUrl=%2Fdocuments%2F42%3Ftab%3Droute",
        );
    });

    it("does not add a return address for the home page", async () => {
        signedIn(null);

        expect(await redirectOf(() => requireReadyUser(args("/")))).toBe("/account/login");
    });

    it("sends a user without a photo to the photo step first", async () => {
        signedIn(makeUser({ photoId: null }));

        expect(await redirectOf(() => requireReadyUser(args("/chats")))).toBe("/account/photo?returnUrl=%2Fchats");
    });
});

describe("requireUser", () => {
    it("lets a user without a photo through (the photo page itself)", async () => {
        signedIn(makeUser({ photoId: null }));

        expect(await redirectOf(() => requireUser(args("/account/photo")))).toBeNull();
    });

    it("sends a visitor to sign in", async () => {
        signedIn(null);

        expect(await redirectOf(() => requireUser(args("/account/password")))).toBe("/account/login?returnUrl=%2Faccount%2Fpassword");
    });
});

describe("requireAnonymous and requireActivation", () => {
    it("let a visitor through", async () => {
        signedIn(null);

        expect(await redirectOf(() => requireAnonymous())).toBeNull();
        expect(await redirectOf(() => requireActivation())).toBeNull();
    });

    it("send a signed-in user home", async () => {
        signedIn(makeUser());

        expect(await redirectOf(() => requireAnonymous())).toBe("/");
        expect(await redirectOf(() => requireActivation())).toBe("/");
    });

    it("send a signed-in user without a photo from the activation to the photo step", async () => {
        signedIn(makeUser({ photoId: null }));

        expect(await redirectOf(() => requireActivation())).toBe("/account/photo");
    });
});

describe("requirePermission", () => {
    /**
     * Runs a permission guard and returns the status it refused with.
     * @param run The guard call.
     * @returns The status, or null when the guard let the route through.
     */
    async function refusalOf(run: () => Promise<null>): Promise<number | null> {
        try {
            await run();
            return null;
        } catch (thrown) {
            return (thrown as { init: { status: number } }).init.status;
        }
    }

    it("lets a user with any of the permissions through", async () => {
        signedIn(makeUser({ permissions: [Permissions.rolesManage] }));

        expect(await refusalOf(requirePermission(Permissions.usersView, Permissions.rolesManage))).toBeNull();
    });

    it("answers 403 to a user without them", async () => {
        signedIn(makeUser({ permissions: [Permissions.chatsUse] }));

        expect(await refusalOf(requirePermission(Permissions.usersView, Permissions.rolesManage))).toBe(403);
    });

    it("answers 403 to a visitor", async () => {
        signedIn(null);

        expect(await refusalOf(requirePermission(Permissions.usersView))).toBe(403);
    });
});
