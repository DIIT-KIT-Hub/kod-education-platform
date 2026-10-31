import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import { BookOpen, FileText, House, Network, Newspaper, Shield, Signature, User, type LucideIcon } from "lucide-react";
import { hasPermission, Permissions, type CurrentUser } from "@/shared/session/session";

/**
 * An entry of the main menu.
 */
export interface NavigationItem {
    /** The application route; entries not moved to the application yet have `href` instead. */
    readonly to?: string;
    /** A page outside the application (full page load). */
    readonly href?: string;
    /** The name. */
    readonly text: MessageDescriptor;
    /** The icon. */
    readonly icon: LucideIcon;
    /** The permissions that reveal the entry; empty means every signed-in user. */
    readonly permissions: readonly string[];
    /** Whether the entry is in the bottom tab bar of phones. */
    readonly tabBar: boolean;
}

/**
 * The main menu in its order.
 */
const items: readonly NavigationItem[] = [
    { to: "/", text: msg`Головна`, icon: House, permissions: [], tabBar: true },
    { to: "/documents", text: msg`Документи`, icon: FileText, permissions: [Permissions.documentsView], tabBar: true },
    { to: "/signing", text: msg`Підписання`, icon: Signature, permissions: [Permissions.signingUse], tabBar: true },
    { href: "/news", text: msg`Новини`, icon: Newspaper, permissions: [Permissions.newsView], tabBar: true },
    { href: "/structure", text: msg`Структура`, icon: Network, permissions: [Permissions.structureView], tabBar: false },
    {
        href: "/admin",
        text: msg`Адміністрування`,
        icon: Shield,
        permissions: [
            Permissions.usersView,
            Permissions.rolesManage,
            Permissions.structureManage,
            Permissions.newsManage,
            Permissions.auditView,
        ],
        tabBar: false,
    },
    { href: "/profile", text: msg`Профіль`, icon: User, permissions: [], tabBar: true },
    { href: "/help", text: msg`Довідка`, icon: BookOpen, permissions: [], tabBar: false },
];

/**
 * The menu entries the user may see.
 * @param user The signed-in user.
 * @returns The entries.
 */
export function navigationFor(user: CurrentUser): readonly NavigationItem[] {
    return items.filter((item) => item.permissions.length === 0 || item.permissions.some((permission) => hasPermission(user, permission)));
}
