import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import {
    BookOpen,
    Briefcase,
    Building2,
    FileText,
    House,
    LifeBuoy,
    MessagesSquare,
    MessageCircle,
    Network,
    Newspaper,
    Shield,
    Signature,
    User,
    UserRound,
    type LucideIcon,
} from "lucide-react";
import { hasPermission, Permissions, type CurrentUser } from "@/shared/session/session";

/**
 * An entry of the main menu.
 */
export interface NavigationItem {
    /** The application route. */
    readonly to: string;
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
 * A section of the main menu: entries that belong together, folded under one heading.
 */
export interface NavigationGroup {
    /** A stable key. */
    readonly id: string;
    /** The heading. */
    readonly text: MessageDescriptor;
    /** The icon of the heading. */
    readonly icon: LucideIcon;
    /** The entries the user may see. */
    readonly items: readonly NavigationItem[];
}

/**
 * The main menu of a user: the home page and the sections.
 */
export interface Navigation {
    /** The home page (always first, outside the sections). */
    readonly home: NavigationItem;
    /** The sections with at least one entry, in their order. */
    readonly groups: readonly NavigationGroup[];
    /** Every entry in the menu order (the tab bar of phones takes its entries from here). */
    readonly items: readonly NavigationItem[];
}

/**
 * The permissions that open the administration (any of them).
 */
export const adminPermissions: readonly string[] = [
    Permissions.usersView,
    Permissions.usersImport,
    Permissions.usersExport,
    Permissions.rolesManage,
    Permissions.structureManage,
    Permissions.newsManage,
    Permissions.auditView,
    Permissions.ticketsManage,
    Permissions.administrationAll,
];

/**
 * The home page.
 */
const home: NavigationItem = { to: "/", text: msg`Головна`, icon: House, permissions: [], tabBar: true };

/**
 * The sections of the main menu in their order.
 */
const groups: readonly NavigationGroup[] = [
    {
        id: "work",
        text: msg`Документообіг`,
        icon: Briefcase,
        items: [
            { to: "/documents", text: msg`Документи`, icon: FileText, permissions: [Permissions.documentsView], tabBar: true },
            { to: "/signing", text: msg`Підписання`, icon: Signature, permissions: [Permissions.signingUse], tabBar: false },
        ],
    },
    {
        id: "communication",
        text: msg`Комунікації`,
        icon: MessageCircle,
        items: [
            { to: "/chats", text: msg`Чати`, icon: MessagesSquare, permissions: [Permissions.chatsUse], tabBar: true },
            { to: "/news", text: msg`Новини`, icon: Newspaper, permissions: [Permissions.newsView], tabBar: true },
            { to: "/requests", text: msg`Звернення`, icon: LifeBuoy, permissions: [], tabBar: false },
        ],
    },
    {
        id: "organization",
        text: msg`Організація`,
        icon: Building2,
        items: [
            { to: "/structure", text: msg`Структура`, icon: Network, permissions: [Permissions.structureView], tabBar: false },
            { to: "/admin", text: msg`Адміністрування`, icon: Shield, permissions: adminPermissions, tabBar: false },
        ],
    },
    {
        id: "personal",
        text: msg`Особисте`,
        icon: UserRound,
        items: [
            { to: "/profile", text: msg`Профіль`, icon: User, permissions: [], tabBar: true },
            { to: "/help", text: msg`Довідка`, icon: BookOpen, permissions: [], tabBar: false },
        ],
    },
];

/**
 * Checks whether a menu entry is the page at an address (the home page only exactly, others with their subpages).
 * @param to The route of the entry.
 * @param pathname The address.
 * @returns The answer.
 */
export function isCurrent(to: string, pathname: string): boolean {
    return to === "/" ? pathname === "/" : pathname === to || pathname.startsWith(`${to}/`);
}

/**
 * The main menu of a user: only the entries their permissions reveal and the sections that keep any.
 * @param user The signed-in user.
 * @returns The menu.
 */
export function navigationFor(user: CurrentUser): Navigation {
    const allowed = (item: NavigationItem) =>
        item.permissions.length === 0 || item.permissions.some((permission) => hasPermission(user, permission));
    const visible = groups.map((group) => ({ ...group, items: group.items.filter(allowed) })).filter((group) => group.items.length > 0);
    return { home, groups: visible, items: [home, ...visible.flatMap((group) => group.items)] };
}
