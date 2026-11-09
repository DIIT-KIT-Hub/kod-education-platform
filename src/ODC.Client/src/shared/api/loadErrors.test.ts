import { CancelledError, QueryClient } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import * as toasts from "@/shared/ui/Toast";
import { createQueryCache, isRefusal, loadErrorMessage, reportLoadError } from "./loadErrors";
import { ApiError } from "./problem";

/**
 * Watches the toasts shown during a test.
 * @returns The spy on {@link toasts.toast}.
 */
function watchToasts() {
    return vi.spyOn(toasts, "toast");
}

describe("loadErrorMessage", () => {
    it("uses the server's own message", () => {
        expect(loadErrorMessage(new ApiError(500, "server_error", "Сервер перевантажено."))).toBe("Сервер перевантажено.");
    });

    it("falls back to a general message for an answer without a message", () => {
        expect(loadErrorMessage(new ApiError(502, "502", ""))).toBe("Не вдалося завантажити дані. Спробуйте ще раз.");
    });

    it("says the server cannot be reached for a network failure", () => {
        expect(loadErrorMessage(new TypeError("Failed to fetch"))).toBe(
            "Не вдалося зв'язатися з сервером. Перевірте з'єднання й спробуйте ще раз.",
        );
    });
});

describe("isRefusal", () => {
    it.each([400, 403, 404, 409, 422])("treats %i as a refusal", (status) => {
        expect(isRefusal(new ApiError(status, "x", ""))).toBe(true);
    });

    it.each([401, 500, 503])("does not treat %i as a refusal", (status) => {
        expect(isRefusal(new ApiError(status, "x", ""))).toBe(false);
    });

    it("does not treat a network failure as a refusal", () => {
        expect(isRefusal(new TypeError("Failed to fetch"))).toBe(false);
    });
});

describe("reportLoadError", () => {
    it("shows the reason as an error toast by default", () => {
        const toast = watchToasts();

        reportLoadError(new ApiError(500, "server_error", "Сервер перевантажено."), undefined);

        expect(toast).toHaveBeenCalledWith("Сервер перевантажено.", { tone: "error" });
        toast.mockRestore();
    });

    it("stays quiet when the page reports the failure itself", () => {
        const toast = watchToasts();

        reportLoadError(new TypeError("Failed to fetch"), { loadErrors: "quiet" });

        expect(toast).not.toHaveBeenCalled();
        toast.mockRestore();
    });

    it("leaves a refusal to a page that shows it as content but still reports failures", () => {
        const toast = watchToasts();

        reportLoadError(new ApiError(404, "not_found", "Не знайдено."), { loadErrors: "refusal-is-content" });
        expect(toast).not.toHaveBeenCalled();

        reportLoadError(new ApiError(500, "server_error", "Збій."), { loadErrors: "refusal-is-content" });
        expect(toast).toHaveBeenCalledWith("Збій.", { tone: "error" });
        toast.mockRestore();
    });

    it("skips a 401 that ended the sign-in (the client leads to the sign-in page)", () => {
        const toast = watchToasts();

        reportLoadError(new ApiError(401, "401", "", "/api/v1/documents"), undefined);
        reportLoadError(new ApiError(401, "401", ""), undefined);

        expect(toast).not.toHaveBeenCalled();
        toast.mockRestore();
    });

    it("shows a 401 of a sign-in step, which carries its own message", () => {
        const toast = watchToasts();

        reportLoadError(new ApiError(401, "no_pending_sign_in", "Почніть вхід спочатку.", "/api/v1/auth/login/pending"), undefined);

        expect(toast).toHaveBeenCalledWith("Почніть вхід спочатку.", { tone: "error" });
        toast.mockRestore();
    });

    it("ignores a cancelled load", () => {
        const toast = watchToasts();

        reportLoadError(new CancelledError(), undefined);

        expect(toast).not.toHaveBeenCalled();
        toast.mockRestore();
    });
});

describe("createQueryCache", () => {
    it("reports a failed query once, after its retries, with the query's meta", async () => {
        const toast = watchToasts();
        const client = new QueryClient({ queryCache: createQueryCache(), defaultOptions: { queries: { retry: 1, retryDelay: 0 } } });
        const load = vi.fn(() => Promise.reject(new ApiError(503, "unavailable", "Сервіс недоступний.")));

        await expect(client.fetchQuery({ queryKey: ["failing"], queryFn: load })).rejects.toBeInstanceOf(ApiError);

        expect(load).toHaveBeenCalledTimes(2);
        expect(toast).toHaveBeenCalledTimes(1);
        expect(toast).toHaveBeenCalledWith("Сервіс недоступний.", { tone: "error" });

        await expect(
            client.fetchQuery({
                queryKey: ["missing"],
                queryFn: () => Promise.reject(new ApiError(404, "x", "")),
                meta: { loadErrors: "refusal-is-content" },
                retry: false,
            }),
        ).rejects.toBeInstanceOf(ApiError);
        expect(toast).toHaveBeenCalledTimes(1);
        toast.mockRestore();
        client.clear();
    });
});
