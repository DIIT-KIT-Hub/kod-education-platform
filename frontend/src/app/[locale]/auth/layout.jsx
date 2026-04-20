// Imports
import styles from "./layout.module.css";
import { getTranslations } from "next-intl/server";
import Additional from "@/features/auth/components/Additional";

/**
 * Authentication layout component.
 *
 * Provides a consistent UI wrapper for all authentication-related pages.
 *
 * Responsibilities:
 * - Loads localized translations for auth layout
 * - Wraps auth pages with consistent structure and styling
 * - Displays additional navigation links (login/verification/forgot password)
 * - Renders copyright section
 *
 * This is a server component using next-intl for translations.
 *
 * @param {Object} props - Layout props
 * @param {React.ReactNode} props.children - Auth page content (login, verification, etc.)
 *
 * @returns {JSX.Element} Auth page layout wrapper
 */
async function AuthLayout({ children }) {
  const tLayout = await getTranslations("AuthLayout");

  return (
    <section className={styles.page}>
      <div className={styles.wrapper}>
        {children}

        <Additional
          t={{
            forgotPassword: tLayout("forgotPassword"),
            verified: tLayout("verified"),
            notVerified: tLayout("notVerified"),
          }}
        />

        <div className={styles.copyright}>
          <p>{tLayout("copyright")}</p>
        </div>
      </div>
    </section>
  );
}

export default AuthLayout;
