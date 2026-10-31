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
    firstPassword: (newPassword: string) => unwrap(api.POST("/api/v1/auth/login/password", { body: { newPassword } })),
    authenticatorKey: () => unwrap(api.GET("/api/v1/auth/login/authenticator")),
    enroll: (code: string, rememberMe: boolean) => unwrap(api.POST("/api/v1/auth/login/authenticator", { body: { code, rememberMe } })),
    logout: () => unwrap(api.POST("/api/v1/auth/logout")),
    changePassword: (currentPassword: string, newPassword: string) =>
        unwrap(api.POST("/api/v1/auth/password", { body: { currentPassword, newPassword } })),
    language: (language: string) => unwrap(api.PUT("/api/v1/auth/language", { body: { language } })),
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
