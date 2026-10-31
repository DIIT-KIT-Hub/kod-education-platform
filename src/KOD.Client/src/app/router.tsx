import { createBrowserRouter } from "react-router";
import { authContextQuery } from "@/shared/session/session";
import { Spinner } from "@/shared/ui/Spinner";
import { basename, requireAnonymous, requirePasswordSet, requireReadyUser, requireUser } from "./guards";
import { AppShell } from "./layout/AppShell";
import { NotFoundPage } from "./pages/NotFoundPage";
import { RouteError } from "./pages/RouteError";
import { queryClient } from "./queryClient";
import { RootLayout } from "./RootLayout";

/**
 * The routes. Pages load lazily: every feature is its own chunk. The sign-in pages share one layout; the cabinet pages
 * are inside the application shell.
 */
export const router = createBrowserRouter(
    [
        {
            Component: RootLayout,
            ErrorBoundary: RouteError,
            HydrateFallback: () => <Spinner block />,
            loader: async () => {
                await queryClient.ensureQueryData(authContextQuery);
                return null;
            },
            children: [
                {
                    path: "account",
                    lazy: async () => ({ Component: (await import("@/features/auth/components/AuthLayout")).AuthLayout }),
                    children: [
                        {
                            loader: requireAnonymous,
                            children: [
                                {
                                    path: "login",
                                    lazy: async () => ({ Component: (await import("@/features/auth/pages/LoginPage")).LoginPage }),
                                },
                                {
                                    path: "login-2fa",
                                    lazy: async () => ({ Component: (await import("@/features/auth/pages/TwoFactorPage")).TwoFactorPage }),
                                },
                                {
                                    path: "login-recovery",
                                    lazy: async () => ({ Component: (await import("@/features/auth/pages/RecoveryPage")).RecoveryPage }),
                                },
                                {
                                    path: "setup-authenticator",
                                    lazy: async () => ({
                                        Component: (await import("@/features/auth/pages/SetupAuthenticatorPage")).SetupAuthenticatorPage,
                                    }),
                                },
                                {
                                    path: "lockout",
                                    lazy: async () => ({ Component: (await import("@/features/auth/pages/LockoutPage")).LockoutPage }),
                                },
                            ],
                        },
                        {
                            path: "change-password",
                            loader: requireUser,
                            lazy: async () => ({
                                Component: (await import("@/features/auth/pages/ChangePasswordPage")).ChangePasswordPage,
                            }),
                        },
                        {
                            path: "photo",
                            loader: requirePasswordSet,
                            lazy: async () => ({ Component: (await import("@/features/auth/pages/PhotoPage")).PhotoPage }),
                        },
                    ],
                },
                {
                    Component: AppShell,
                    loader: requireReadyUser,
                    children: [
                        { index: true, lazy: async () => ({ Component: (await import("@/features/home/HomePage")).HomePage }) },
                        { path: "*", Component: NotFoundPage },
                    ],
                },
            ],
        },
    ],
    { basename },
);
