import { createBrowserRouter } from "react-router";
import { authContextQuery, Permissions } from "@/shared/session/session";
import { Spinner } from "@/shared/ui/Spinner";
import { basename, requireAnonymous, requirePasswordSet, requirePermission, requireReadyUser, requireUser } from "./guards";
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
                        {
                            // Errors and forbidden pages show inside the shell, so the menu stays.
                            ErrorBoundary: RouteError,
                            children: [
                                { index: true, lazy: async () => ({ Component: (await import("@/features/home/HomePage")).HomePage }) },
                                {
                                    path: "documents",
                                    children: [
                                        {
                                            index: true,
                                            loader: requirePermission(Permissions.documentsView),
                                            lazy: async () => ({
                                                Component: (await import("@/features/documents/pages/DocumentsPage")).DocumentsPage,
                                            }),
                                        },
                                        {
                                            path: "new",
                                            loader: requirePermission(Permissions.documentsCreate),
                                            lazy: async () => ({
                                                Component: (await import("@/features/documents/pages/DocumentEditorPage"))
                                                    .DocumentEditorPage,
                                            }),
                                        },
                                        {
                                            path: ":id",
                                            loader: requirePermission(Permissions.documentsView),
                                            lazy: async () => ({
                                                Component: (await import("@/features/documents/pages/DocumentPage")).DocumentPage,
                                            }),
                                        },
                                        {
                                            path: ":id/edit",
                                            loader: requirePermission(Permissions.documentsCreate),
                                            lazy: async () => ({
                                                Component: (await import("@/features/documents/pages/DocumentEditorPage"))
                                                    .DocumentEditorPage,
                                            }),
                                        },
                                    ],
                                },
                                {
                                    path: "news",
                                    loader: requirePermission(Permissions.newsView),
                                    children: [
                                        {
                                            index: true,
                                            lazy: async () => ({ Component: (await import("@/features/news/pages/NewsPage")).NewsPage }),
                                        },
                                        {
                                            path: "manage",
                                            loader: requirePermission(Permissions.newsManage),
                                            children: [
                                                {
                                                    index: true,
                                                    lazy: async () => ({
                                                        Component: (await import("@/features/news/pages/NewsManagePage")).NewsManagePage,
                                                    }),
                                                },
                                                {
                                                    path: "new",
                                                    lazy: async () => ({
                                                        Component: (await import("@/features/news/pages/NewsEditorPage")).NewsEditorPage,
                                                    }),
                                                },
                                                {
                                                    path: ":id",
                                                    lazy: async () => ({
                                                        Component: (await import("@/features/news/pages/NewsEditorPage")).NewsEditorPage,
                                                    }),
                                                },
                                            ],
                                        },
                                        {
                                            path: ":slug",
                                            lazy: async () => ({
                                                Component: (await import("@/features/news/pages/ArticlePage")).ArticlePage,
                                            }),
                                        },
                                    ],
                                },
                                {
                                    path: "signing",
                                    loader: requirePermission(Permissions.signingUse),
                                    children: [
                                        {
                                            index: true,
                                            lazy: async () => ({
                                                Component: (await import("@/features/signing/pages/SigningPages")).SignFilesPage,
                                            }),
                                        },
                                        {
                                            path: "verify",
                                            lazy: async () => ({
                                                Component: (await import("@/features/signing/pages/SigningPages")).VerifySignaturePage,
                                            }),
                                        },
                                    ],
                                },
                                {
                                    path: "structure",
                                    loader: requirePermission(Permissions.structureView),
                                    children: [
                                        {
                                            index: true,
                                            lazy: async () => ({
                                                Component: (await import("@/features/structure/pages/StructurePage")).StructurePage,
                                            }),
                                        },
                                        {
                                            path: ":id",
                                            lazy: async () => ({
                                                Component: (await import("@/features/structure/pages/UnitPage")).UnitPage,
                                            }),
                                        },
                                    ],
                                },
                                {
                                    path: "profile",
                                    children: [
                                        {
                                            index: true,
                                            lazy: async () => ({
                                                Component: (await import("@/features/profile/pages/ProfilePage")).ProfilePage,
                                            }),
                                        },
                                        {
                                            path: "security",
                                            lazy: async () => ({
                                                Component: (await import("@/features/profile/pages/SecurityPage")).SecurityPage,
                                            }),
                                        },
                                    ],
                                },
                                { path: "*", Component: NotFoundPage },
                            ],
                        },
                    ],
                },
            ],
        },
    ],
    { basename },
);
