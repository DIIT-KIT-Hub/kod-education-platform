import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";

/**
 * The keys of the profile queries.
 */
export const profileKeys = {
    all: ["profile"] as const,
};

/**
 * The profile of the signed-in user: positions, modules, permissions and security state.
 */
export const profileQuery = queryOptions({
    queryKey: profileKeys.all,
    queryFn: () => unwrap(api.GET("/api/v1/profile")),
});

/**
 * Changes of the profile.
 */
export const profileApi = {
    phone: (phoneNumber: string | null) => unwrap(api.PATCH("/api/v1/profile", { body: { phoneNumber } })),
    language: (language: string) => unwrap(api.PUT("/api/v1/auth/language", { body: { language } })),
    recoveryCodes: (code: string) => unwrap(api.POST("/api/v1/profile/recovery-codes", { body: { code } })),
    signOutEverywhere: () => unwrap(api.POST("/api/v1/profile/sign-out-everywhere")),
};
