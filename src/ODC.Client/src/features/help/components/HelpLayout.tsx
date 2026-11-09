import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, BookOpen, Menu, Search, X } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router";
import { lastPage } from "@/shared/lib/lastPage";
import { hasPermission, useCurrentUser } from "@/shared/session/session";
import { BrandMark } from "@/shared/ui/Brand";
import { Button } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { ThemeToggle } from "@/shared/ui/ThemeToggle";
import { helpPermissionsQuery } from "../api";
import styles from "./HelpLayout.module.css";
import { HelpNav } from "./HelpNav";
import { HelpSearch } from "./HelpSearch";
import { ManualContext } from "./manualContext";

/**
 * The manual as a site of its own inside the cabinet: its own header (search, back to the cabinet), the chapters on the
 * left and one page at a time. Only signed-in users reach it (the route guard), and the badges tell what the reader
 * may use.
 * @returns The layout.
 */
export function HelpLayout() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const navigate = useNavigate();
    const location = useLocation();
    const permissions = useQuery(helpPermissionsQuery);
    const [menu, setMenu] = useState<string | null>(null);
    const [searching, setSearching] = useState(false);
    const menuOpen = menu === location.pathname;

    const permissionTitles = new Map((permissions.data ?? []).map((permission) => [permission.code, permission.title ?? permission.code]));
    const manual = { permissionTitles, has: (permission: string) => hasPermission(user, permission) };

    // Ctrl + K (⌘ + K) or "/" opens the search from anywhere in the manual.
    useEffect(() => {
        function onKey(event: KeyboardEvent): void {
            const typing =
                event.target instanceof HTMLElement &&
                (event.target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(event.target.tagName));
            if ((event.key.toLowerCase() === "k" && (event.ctrlKey || event.metaKey)) || (event.key === "/" && !typing)) {
                event.preventDefault();
                setSearching(true);
            }
        }

        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, []);

    /**
     * Leaves the manual for the page of the cabinet the user came from.
     */
    function backToCabinet(): void {
        const page = lastPage();
        void navigate(page.path, { state: page.state });
    }

    return (
        <ManualContext.Provider value={manual}>
            <div className={styles.layout}>
                <a className={styles.skip} href="#help-content">
                    {t`Перейти до тексту`}
                </a>
                <header className={styles.header}>
                    <div className={styles.headerStart}>
                        <Button
                            variant="ghost"
                            size="sm"
                            iconOnly
                            icon={menuOpen ? X : Menu}
                            className={styles.menuButton}
                            onClick={() => setMenu(menuOpen ? null : location.pathname)}
                            aria-expanded={menuOpen}
                            aria-controls="help-nav"
                        >
                            {t`Зміст довідки`}
                        </Button>
                        <Link to="/help" className={styles.brand}>
                            <BrandMark compact />
                            <span className={styles.brandText}>
                                <strong>{t`КОД`}</strong>
                                <span>
                                    <Icon icon={BookOpen} size="sm" />
                                    {t`Довідка`}
                                </span>
                            </span>
                        </Link>
                    </div>
                    <button type="button" className={styles.searchButton} onClick={() => setSearching(true)}>
                        <Icon icon={Search} />
                        <span className={styles.searchText}>{t`Пошук у довідці…`}</span>
                        <kbd className={styles.searchKey}>Ctrl K</kbd>
                    </button>
                    <div className={styles.headerEnd}>
                        <ThemeToggle />
                        <Button variant="secondary" size="sm" icon={ArrowLeft} onClick={backToCabinet} className={styles.back}>
                            <span className={styles.backText}>{t`Повернутися до кабінету`}</span>
                        </Button>
                    </div>
                </header>
                <div className={styles.body}>
                    <aside id="help-nav" className={styles.nav} data-open={menuOpen || undefined} aria-label={t`Зміст довідки`}>
                        <HelpNav />
                    </aside>
                    {menuOpen && <div className={styles.scrim} onClick={() => setMenu(null)} aria-hidden="true" />}
                    <main id="help-content" className={styles.main} tabIndex={-1}>
                        <Outlet context={{ openSearch: () => setSearching(true) }} />
                    </main>
                </div>
            </div>
            <HelpSearch open={searching} onClose={() => setSearching(false)} />
        </ManualContext.Provider>
    );
}
