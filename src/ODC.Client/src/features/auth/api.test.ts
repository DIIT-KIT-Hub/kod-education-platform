import { beforeEach, describe, expect, it } from "vitest";
import { setAntiforgeryToken } from "@/shared/api/client";
import { ApiError } from "@/shared/api/problem";
import { mockApi, problem } from "@/test/server";
import { activationApi, authApi, uploadPhoto } from "./api";

beforeEach(() => setAntiforgeryToken("csrf-test-token"));

describe("authApi", () => {
    it("sends the recovery code without remember me", async () => {
        const calls = mockApi("post", "/api/v1/auth/login/recovery", 204);

        await authApi.recovery("ABCD-1234");

        expect(calls[0]?.body).toEqual({ code: "ABCD-1234", rememberMe: false });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("signs out with the antiforgery token", async () => {
        const calls = mockApi("post", "/api/v1/auth/logout", 204);

        await authApi.logout();

        expect(calls).toHaveLength(1);
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("saves the language", async () => {
        const calls = mockApi("put", "/api/v1/auth/language", 204);

        await authApi.language("en");

        expect(calls[0]?.body).toEqual({ language: "en" });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("reads the pending sign-in without the antiforgery header", async () => {
        const calls = mockApi("get", "/api/v1/auth/login/pending", { twoFactorEnabled: true });

        await expect(authApi.pending()).resolves.toEqual({ twoFactorEnabled: true });
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBeNull();
    });

    it("turns a problem into an ApiError", async () => {
        mockApi("post", "/api/v1/auth/password", () => problem(400, "password_mismatch", "Невірний поточний пароль."));

        const failure = await authApi.changePassword("old", "new-password-1").catch((error: unknown) => error);

        expect(failure).toBeInstanceOf(ApiError);
        expect(failure).toMatchObject({ status: 400, code: "password_mismatch", detail: "Невірний поточний пароль." });
    });
});

describe("activationApi", () => {
    it("forgets the activation of this browser", async () => {
        const calls = mockApi("delete", "/api/v1/auth/activation", 204);

        await activationApi.cancel();

        expect(calls).toHaveLength(1);
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("sends the e-mail, the code and the password", async () => {
        const start = mockApi("post", "/api/v1/auth/activation", { stage: "Code", email: "a@odc.local", resendAfterSeconds: 60 });
        const verify = mockApi("post", "/api/v1/auth/activation/code", { firstName: "Олена", hasAuthenticator: false, hasPhoto: false });
        const password = mockApi("post", "/api/v1/auth/activation/password", { next: "setup" });

        await activationApi.start("a@odc.local");
        await activationApi.verify("123456");
        await activationApi.password("Correct-horse-battery-1");

        expect(start[0]?.body).toEqual({ email: "a@odc.local" });
        expect(verify[0]?.body).toEqual({ code: "123456" });
        expect(password[0]?.body).toEqual({ newPassword: "Correct-horse-battery-1" });
    });
});

describe("uploadPhoto", () => {
    it("sends the file with its square", async () => {
        const calls = mockApi("put", "/api/v1/profile/photo", "photo-id");
        const file = new File(["png"], "me.png", { type: "image/png" });

        await expect(uploadPhoto(file, { left: 0.1, top: 0.2, width: 0.5, height: 0.25 })).resolves.toBe("photo-id");

        const form = calls[0]?.body as FormData;
        expect(form).toBeInstanceOf(FormData);
        expect((form.get("file") as File).name).toBe("me.png");
        expect(form.get("cropLeft")).toBe("0.1");
        expect(form.get("cropTop")).toBe("0.2");
        expect(form.get("cropWidth")).toBe("0.5");
        expect(form.get("cropHeight")).toBe("0.25");
        expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
    });

    it("sends only the file when there is no square", async () => {
        const calls = mockApi("put", "/api/v1/profile/photo", "photo-id");

        await uploadPhoto(new File(["png"], "me.png", { type: "image/png" }), null);

        const form = calls[0]?.body as FormData;
        expect([...form.keys()]).toEqual(["file"]);
    });
});
