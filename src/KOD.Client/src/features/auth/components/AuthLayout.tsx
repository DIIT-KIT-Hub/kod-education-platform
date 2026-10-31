import { Trans, useLingui } from "@lingui/react/macro";
import { Outlet } from "react-router";
import { BrandMark } from "@/shared/ui/Brand";
import { ThemeToggle } from "@/shared/ui/ThemeToggle";
import hero1200 from "../assets/hero-1200.avif";
import hero800 from "../assets/hero-800.avif";
import styles from "./AuthLayout.module.css";
import { LanguageSwitch } from "./LanguageSwitch";

/**
 * The layout of the sign-in pages: a photo panel with the product name on wide screens and the form card.
 * @returns The layout.
 */
export function AuthLayout() {
    const { t } = useLingui();
    return (
        <div className={styles.shell}>
            <section className={styles.hero} aria-hidden="true">
                {/* The photo is requested only where the panel is visible (≥ 1024px); smaller screens get a 1×1 placeholder. */}
                <picture>
                    <source media="(min-width: 1024px)" type="image/avif" srcSet={`${hero800} 800w, ${hero1200} 1200w`} sizes="50vw" />
                    <img
                        className={styles.photo}
                        src="data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEAAAAALAAAAAABAAEAAAIBAAA="
                        alt=""
                        width={1200}
                        height={1334}
                        fetchPriority="high"
                        decoding="async"
                    />
                </picture>
                <div className={styles.heroBrand}>
                    <BrandMark className={styles.heroMark} />
                    <strong>{t`КОД`}</strong>
                </div>
                <div className={styles.heroText}>
                    <h2>
                        <Trans>
                            Кабінет
                            <br />
                            організаційних документів
                        </Trans>
                    </h2>
                    <p>{t`Електронний документообіг для будь-якої організації: структура, маршрути погодження та кваліфікований електронний підпис.`}</p>
                </div>
            </section>
            <main className={styles.main}>
                <div className={styles.card}>
                    <div className={styles.mobileBrand}>
                        <BrandMark compact /> {t`КОД`}
                    </div>
                    <Outlet />
                </div>
                <div className={styles.tools}>
                    <LanguageSwitch />
                    <ThemeToggle labelled />
                </div>
            </main>
        </div>
    );
}
