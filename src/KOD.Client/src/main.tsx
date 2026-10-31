import { I18nProvider } from "@lingui/react";
import { QueryClientProvider } from "@tanstack/react-query";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { router } from "@/app/router";
import { queryClient } from "@/app/queryClient";
import { onUnauthorized } from "@/shared/api/client";
import { activateLocale, i18n } from "@/shared/i18n/i18n";
import { reloadAuthContext } from "@/shared/session/session";

// The source language first, so even a failed start shows translated texts; the start context then switches to the
// user's language before the first page renders.
await activateLocale("uk");

// The sign-in ended while the application was open (signed out elsewhere, blocked, expired): back to the sign-in page.
onUnauthorized(() => {
    void reloadAuthContext(queryClient).then(() => {
        const { pathname, search } = router.state.location;
        void router.navigate(`/account/login?returnUrl=${encodeURIComponent(pathname + search)}`, { replace: true });
    });
});

const container = document.getElementById("root");
if (!container) {
    throw new Error("The page has no #root element.");
}

createRoot(container).render(
    <StrictMode>
        <I18nProvider i18n={i18n}>
            <QueryClientProvider client={queryClient}>
                <RouterProvider router={router} />
            </QueryClientProvider>
        </I18nProvider>
    </StrictMode>,
);
