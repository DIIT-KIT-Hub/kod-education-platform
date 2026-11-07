import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { ImageCrop } from "@/shared/ui/PhotoPicker";

/**
 * The calls of the sign-in flow and the account pages.
 */
export const authApi = {
    login: (email: string, password: string, rememberMe: boolean) =>
        unwrap(api.POST("/api/v1/auth/login", { body: { email, password, rememberMe } })),
    pending: () => unwrap(api.GET("/api/v1/auth/login/pending")),
    twoFactor: (code: string, rememberMe: boolean) => unwrap(api.POST("/api/v1/auth/login/two-factor", { body: { code, rememberMe } })),
    recovery: (code: string) => unwrap(api.POST("/api/v1/auth/login/recovery", { body: { code, rememberMe: false } })),
    authenticatorKey: () => unwrap(api.GET("/api/v1/auth/login/authenticator")),
    enroll: (code: string, rememberMe: boolean) => unwrap(api.POST("/api/v1/auth/login/authenticator", { body: { code, rememberMe } })),
    logout: () => unwrap(api.POST("/api/v1/auth/logout")),
    changePassword: (currentPassword: string, newPassword: string) =>
        unwrap(api.POST("/api/v1/auth/password", { body: { currentPassword, newPassword } })),
    language: (language: string) => unwrap(api.PUT("/api/v1/auth/language", { body: { language } })),
};

/**
 * The calls of the account activation (a new account, or after an administrator reset the password): the e-mail, the
 * one-time code from it and the first password. The activation is bound to this browser by an HttpOnly cookie.
 */
export const activationApi = {
    status: () => unwrap(api.GET("/api/v1/auth/activation")),
    start: (email: string) => unwrap(api.POST("/api/v1/auth/activation", { body: { email } })),
    verify: (code: string) => unwrap(api.POST("/api/v1/auth/activation/code", { body: { code } })),
    password: (newPassword: string) => unwrap(api.POST("/api/v1/auth/activation/password", { body: { newPassword } })),
    cancel: () => unwrap(api.DELETE("/api/v1/auth/activation")),
};

/**
 * Uploads a new profile photo with its square (the server cuts it, stores AVIF and re-issues this browser's tokens).
 * @param file The photo.
 * @param crop The square as fractions of the image, or null for the centre.
 * @returns The identifier of the stored photo.
 */
export function uploadPhoto(file: File, crop: ImageCrop | null): Promise<string> {
    const form = new FormData();
    form.append("file", file);
    if (crop) {
        form.append("cropLeft", String(crop.left));
        form.append("cropTop", String(crop.top));
        form.append("cropWidth", String(crop.width));
        form.append("cropHeight", String(crop.height));
    }

    return unwrap(api.PUT("/api/v1/profile/photo", { body: {}, bodySerializer: () => form }));
}
