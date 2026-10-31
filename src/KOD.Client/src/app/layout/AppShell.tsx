import { useLingui } from "@lingui/react/macro";
import { ChevronLeft, Menu, X } from "lucide-react";
import { useRef, useState, type FocusEvent, type MouseEvent } from "react";
import { Link, Outlet } from "react-router";
import { toggleSidebar, useSidebarCollapsed } from "@/shared/lib/preferences";
import { useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { BrandMark, ProductName } from "@/shared/ui/Brand";
import { Button } from "@/shared/ui/Button";
import { ThemeToggle } from "@/shared/ui/ThemeToggle";
import styles from "./AppShell.module.css";
import { navigationFor } from "./navigation";
import { NavLinks } from "./NavLinks";
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
 * drawer, desktops (1024px+) a sidebar that collapses to an icon rail.
 * @returns The layout.
 */
export function AppShell() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const items = navigationFor(user);
    const collapsed = useSidebarCollapsed();
    const drawer = useRef<HTMLDivElement>(null);
    const [tip, setTip] = useState<RailTip | null>(null);

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
                    <nav className={styles.nav}>
                        <NavLinks
                            items={items}
                            linkClassName={styles.navLink}
                            activeClassName={styles.active}
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
                    <nav className={styles.nav}>
                        <NavLinks
                            items={items}
                            linkClassName={styles.navLink}
                            activeClassName={styles.active}
                            onNavigate={() => drawer.current?.hidePopover()}
                        />
                    </nav>
                    <div className={styles.drawerFooter}>
                        <UserMenu />
                    </div>
                </div>

                <main id="main" className={styles.main} tabIndex={-1}>
                    <div className={styles.page}>
                        <Outlet />
                    </div>
                </main>

                <nav className={styles.tabbar} aria-label={t`Швидка навігація`}>
                    <NavLinks
                        items={items.filter((item) => item.tabBar)}
                        limit={5}
                        linkClassName={styles.tab}
                        activeClassName={styles.active}
                    />
                </nav>
            </div>

            {tip && (
                <div className={styles.railTip} style={{ left: tip.left, top: tip.top }} aria-hidden="true">
                    {tip.text}
                </div>
            )}
        </>
    );
}
