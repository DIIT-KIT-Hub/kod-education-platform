import { createBrowserRouter } from "react-router";
import { organizationBase } from "@/shared/api/organization";
import { authContextQuery, Permissions } from "@/shared/session/session";
import { SkeletonPage } from "@/shared/ui/Skeleton";
import { requireAnonymous, requireActivation, requirePermission, requireReadyUser, requireUser } from "./guards";
import { AppShell } from "./layout/AppShell";
import { adminPermissions } from "./layout/navigation";
import { NotFoundPage } from "./pages/NotFoundPage";
import { RouteError } from "./pages/RouteError";
import { queryClient } from "./queryClient";
import { RootLayout } from "./RootLayout";

/**
 * The routes. Pages load lazily: every feature is its own chunk. The sign-in pages share one layout; the cabinet pages
 * are inside the application shell. The base of every address is the page's organization (`/o/{code}`).
 */
export const router = createBrowserRouter(
    [
        {
            Component: RootLayout,
            ErrorBoundary: RouteError,
            HydrateFallback: () => <SkeletonPage screen />,
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
                            path: "activate",
                            loader: requireActivation,
                            lazy: async () => ({ Component: (await import("@/features/auth/pages/ActivationPage")).ActivationPage }),
                        },
                        {
                            path: "photo",
                            loader: requireUser,
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
                                    path: "chats",
                                    loader: requirePermission(Permissions.chatsUse),
                                    children: [
                                        {
                                            // One page for the list and the open chat: switching chats keeps the list.
                                            path: ":chatId?",
                                            lazy: async () => ({ Component: (await import("@/features/chats/pages/ChatsPage")).ChatsPage }),
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
                                    path: "requests",
                                    children: [
                                        {
                                            index: true,
                                            lazy: async () => ({
                                                Component: (await import("@/features/tickets/pages/TicketsPage")).TicketsPage,
                                            }),
                                        },
                                        {
                                            path: "new",
                                            lazy: async () => ({
                                                Component: (await import("@/features/tickets/pages/NewTicketPage")).NewTicketPage,
                                            }),
                                        },
                                        {
                                            path: ":id",
                                            lazy: async () => ({
                                                Component: (await import("@/features/tickets/pages/TicketPage")).TicketPage,
                                            }),
                                        },
                                    ],
                                },
                                {
                                    path: "notifications",
                                    lazy: async () => ({
                                        Component: (await import("@/features/notifications/pages/NotificationsPage")).NotificationsPage,
                                    }),
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
                                {
                                    path: "admin",
                                    children: [
                                        {
                                            index: true,
                                            loader: requirePermission(...adminPermissions),
                                            lazy: async () => ({ Component: (await import("@/features/admin/pages/AdminPage")).AdminPage }),
                                        },
                                        {
                                            path: "users",
                                            loader: requirePermission(Permissions.usersView),
                                            lazy: async () => ({ Component: (await import("@/features/admin/pages/UsersPage")).UsersPage }),
                                        },
                                        {
                                            path: "users/new",
                                            loader: requirePermission(Permissions.usersManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UserEditorPage")).UserEditorPage,
                                            }),
                                        },
                                        {
                                            path: "users/import",
                                            loader: requirePermission(Permissions.usersImport, Permissions.usersExport),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UsersImportPage")).UsersImportPage,
                                            }),
                                        },
                                        {
                                            path: "users/:id",
                                            loader: requirePermission(Permissions.usersView),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UserEditorPage")).UserEditorPage,
                                            }),
                                        },
                                        {
                                            path: "roles",
                                            loader: requirePermission(Permissions.rolesManage),
                                            lazy: async () => ({ Component: (await import("@/features/admin/pages/RolesPage")).RolesPage }),
                                        },
                                        {
                                            path: "roles/new",
                                            loader: requirePermission(Permissions.rolesManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/RoleEditorPage")).RoleEditorPage,
                                            }),
                                        },
                                        {
                                            path: "roles/:id",
                                            loader: requirePermission(Permissions.rolesManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/RoleEditorPage")).RoleEditorPage,
                                            }),
                                        },
                                        {
                                            path: "units",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({ Component: (await import("@/features/admin/pages/UnitsPage")).UnitsPage }),
                                        },
                                        {
                                            path: "units/new",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UnitEditorPage")).UnitEditorPage,
                                            }),
                                        },
                                        {
                                            path: "units/:id",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UnitEditorPage")).UnitEditorPage,
                                            }),
                                        },
                                        {
                                            path: "unit-types",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UnitTypesPage")).UnitTypesPage,
                                            }),
                                        },
                                        {
                                            path: "unit-types/new",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UnitTypeEditorPage")).UnitTypeEditorPage,
                                            }),
                                        },
                                        {
                                            path: "unit-types/:id",
                                            loader: requirePermission(Permissions.structureManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/admin/pages/UnitTypeEditorPage")).UnitTypeEditorPage,
                                            }),
                                        },
                                        {
                                            path: "requests",
                                            loader: requirePermission(Permissions.ticketsManage),
                                            lazy: async () => ({
                                                Component: (await import("@/features/tickets/pages/TicketsAdminPage")).TicketsAdminPage,
                                            }),
                                        },
                                        {
                                            path: "audit",
                                            loader: requirePermission(Permissions.auditView),
                                            lazy: async () => ({ Component: (await import("@/features/admin/pages/AuditPage")).AuditPage }),
                                        },
                                    ],
                                },
                                { path: "*", Component: NotFoundPage },
                            ],
                        },
                    ],
                },
                {
                    // The manual: a page of the cabinet for signed-in users with a layout of its own (no menu of the cabinet).
                    path: "help",
                    loader: requireReadyUser,
                    lazy: async () => ({ Component: (await import("@/features/help/components/HelpLayout")).HelpLayout }),
                    children: [
                        {
                            index: true,
                            lazy: async () => ({ Component: (await import("@/features/help/pages/HelpHomePage")).HelpHomePage }),
                        },
                        {
                            path: ":topicId",
                            lazy: async () => ({ Component: (await import("@/features/help/pages/HelpTopicPage")).HelpTopicPage }),
                        },
                    ],
                },
            ],
        },
    ],
    // An organization's pages live under /o/{code}/ (the default organization at the root).
    { basename: organizationBase || "/" },
);
