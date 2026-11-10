import { useLingui } from "@lingui/react/macro";
import { ChevronLeft, Menu, X } from "lucide-react";
import { useEffect, useRef, useState, type FocusEvent, type MouseEvent } from "react";
import { Link, Outlet, useLocation } from "react-router";
import { rememberPage } from "@/shared/lib/lastPage";
import { toggleSidebar, useSidebarCollapsed } from "@/shared/lib/preferences";
import { ChatSync, useUnreadChats } from "@/features/chats/components/ChatSync";
import { NotificationPanel, NotificationTrigger } from "@/features/notifications/components/NotificationPanel";
import { NotificationSync } from "@/features/notifications/components/NotificationSync";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { BrandMark, ProductName } from "@/shared/ui/Brand";
import { Button } from "@/shared/ui/Button";
import { ThemeToggle } from "@/shared/ui/ThemeToggle";
import styles from "./AppShell.module.css";
import { navigationFor } from "./navigation";
import { NavGroups } from "./NavGroups";
import { NavLinks } from "./NavLinks";
import { OrganizationSwitcher } from "./OrganizationSwitcher";
import { UserMenu } from "./UserMenu";

/**
 * A tooltip of the collapsed sidebar.
 */
interface RailTip {
    readonly text: string;
    readonly left: number;
    readonly top: number;
}

/**
 * The layout of the signed-in pages. Mobile-first: phones get a top bar and a bottom tab bar, tablets a top bar and a
 * drawer, desktops (1024px+) a sidebar that collapses to an icon rail. The menu folds into sections (one open at a
 * time); the bell opens the notification panel.
 * @returns The layout.
 */
export function AppShell() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const navigation = navigationFor(user);
    const canChat = hasPermission(user, Permissions.chatsUse);
    const counts = { "/chats": useUnreadChats(canChat) };
    const collapsed = useSidebarCollapsed();
    const drawer = useRef<HTMLDivElement>(null);
    const [tip, setTip] = useState<RailTip | null>(null);
    const location = useLocation();

    // The manual has its own layout; its "back to the cabinet" button returns to the page the user left (with the
    // filters of a list, which live in the history state).
    useEffect(() => {
        rememberPage({ path: location.pathname + location.search + location.hash, state: location.state });
    }, [location]);

    /**
     * Shows the name of a menu entry next to its icon while the sidebar is a rail. The tooltip is fixed to the viewport,
     * so the scrollable sidebar never clips it.
     * @param event The pointer or focus event of the link.
     * @param text The name.
     */
    function hint(event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>, text: string): void {
        if (!collapsed || !window.matchMedia("(min-width: 1024px)").matches) {
            return;
        }

        const box = event.currentTarget.getBoundingClientRect();
        setTip({ text, left: box.right + 12, top: box.top + box.height / 2 });
    }

    return (
        <>
            <a className={styles.skip} href="#main">
                {t`Перейти до змісту`}
            </a>

            <div className={styles.shell}>
                <aside
                    id="app-sidebar"
                    className={styles.sidebar}
                    aria-label={t`Головне меню`}
                    onMouseLeave={() => setTip(null)}
                    onBlur={() => setTip(null)}
                    onScroll={() => setTip(null)}
                >
                    <Link className={styles.brand} to="/">
                        <BrandMark className={styles.brandMark} />
                        <span className={styles.brandText}>
                            <strong>{t`КОД`}</strong>
                            <small>
                                <ProductName />
                            </small>
                        </span>
                    </Link>
                    <OrganizationSwitcher rail={collapsed} />
                    <nav className={styles.nav}>
                        <NavLinks
                            items={[navigation.home]}
                            linkClassName={styles.navLink}
                            activeClassName={styles.active}
                            onHint={hint}
                            onNavigate={() => setTip(null)}
                        />
                        <NotificationTrigger variant="menu" className={styles.navLink} countClassName={styles.count} onHint={hint} />
                        <NavGroups
                            groups={navigation.groups}
                            counts={counts}
                            rail={collapsed}
                            onHint={hint}
                            onNavigate={() => setTip(null)}
                        />
                    </nav>
                    <div className={styles.sidebarFooter}>
                        <UserMenu />
                    </div>
                </aside>

                {/* Collapses the desktop sidebar to an icon rail; outside the aside so its scroll area does not clip the button. */}
                <button
                    type="button"
                    className={styles.sidebarToggle}
                    aria-controls="app-sidebar"
                    aria-expanded={!collapsed}
                    aria-label={collapsed ? t`Розгорнути меню` : t`Згорнути меню`}
                    title={collapsed ? t`Розгорнути меню` : t`Згорнути меню`}
                    onClick={toggleSidebar}
                >
                    <ChevronLeft aria-hidden="true" />
                </button>

                <header className={styles.topbar}>
                    <Link className={styles.brand} to="/">
                        <BrandMark compact className={styles.brandMark} />
                        <strong>{t`КОД`}</strong>
                    </Link>
                    <div className={layout.row}>
                        <NotificationTrigger variant="icon" />
                        <ThemeToggle />
                        <Button variant="ghost" size="sm" iconOnly icon={Menu} popoverTarget="app-drawer">
                            {t`Меню`}
                        </Button>
                    </div>
                </header>

                <div id="app-drawer" ref={drawer} className={styles.drawer} popover="auto">
                    <div className={styles.drawerHeader}>
                        <span className={layout.strong}>{t`Меню`}</span>
                        <Button variant="ghost" size="sm" iconOnly icon={X} popoverTarget="app-drawer" popoverTargetAction="hide">
                            {t`Закрити меню`}
                        </Button>
                    </div>
                    <OrganizationSwitcher />
                    <nav className={styles.nav}>
                        <NavLinks
                            items={[navigation.home]}
                            linkClassName={styles.navLink}
                            activeClassName={styles.active}
                            onNavigate={() => drawer.current?.hidePopover()}
                        />
                        <NavGroups groups={navigation.groups} counts={counts} onNavigate={() => drawer.current?.hidePopover()} />
                    </nav>
                    <div className={styles.drawerFooter}>
                        <UserMenu />
                    </div>
                </div>

                {/* Inside the shell: next to the sidebar it reads the sidebar's current width. */}
                <NotificationPanel />

                <main id="main" className={styles.main} tabIndex={-1}>
                    <div className={styles.page}>
                        <Outlet />
                    </div>
                </main>

                <nav className={styles.tabbar} aria-label={t`Швидка навігація`}>
                    <NavLinks
                        items={navigation.items.filter((item) => item.tabBar)}
                        limit={5}
                        counts={counts}
                        linkClassName={styles.tab}
                        activeClassName={styles.active}
                    />
                </nav>
            </div>

            {canChat && <ChatSync />}
            <NotificationSync />

            {tip && (
                <div className={styles.railTip} style={{ left: tip.left, top: tip.top }} aria-hidden="true">
                    {tip.text}
                </div>
            )}
        </>
    );
}
