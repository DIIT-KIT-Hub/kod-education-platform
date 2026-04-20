// CSR
"use client";

// Imports
import styles from "./Additional.module.css";
import { Link } from "@/i18n/routing";
import { usePathname } from "next/navigation";

/**
 * Additional authentication links component.
 *
 * Displays contextual navigation links based on the current auth route:
 * - Shows "Forgot password" link only on non-verification pages
 * - Toggles between login and verification links depending on current path
 *
 * Uses pathname to determine current authentication step.
 *
 * @param {Object} props - Component props
 * @param {Object} props.t - Translation object containing localized strings
 * @param {string} props.t.forgotPassword - Text for "Forgot password" link
 * @param {string} props.t.verified - Text for login link (when on verification page)
 * @param {string} props.t.notVerified - Text for verification link (when on login page)
 *
 * @returns {JSX.Element} Rendered additional auth navigation links
 */
function Additional({ t }) {
  const pathname = usePathname();
  const isVerificationPage = pathname.includes("/verification");

  return (
    <div className={styles.additional}>
      {!isVerificationPage && (
        <>
          <Link href="/auth/forgotpassword">{t.forgotPassword}</Link>
          <span>|</span>
        </>
      )}
      <Link href={isVerificationPage ? "/auth/login" : "/auth/verification"}>
        {isVerificationPage ? t.verified : t.notVerified}
      </Link>
    </div>
  );
}

// Component export
export default Additional;
