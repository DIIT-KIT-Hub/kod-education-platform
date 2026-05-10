// Imports
import Sidebar from "@/shared/components/sidebar/Sidebar";
import styles from "./layout.module.css";
import { getAuthSystemInfoAsync } from "@/features/auth/actions/auth/actions";
import AuthProvider from "@/shared/stores/auth/AuthProvider";
import { getTranslations } from "next-intl/server";

/**
 * Main authenticated dashboard layout component.
 *
 * This layout:
 * - Fetches authenticated user data from the backend
 * - Loads localized sidebar routes
 * - Provides authentication context via AuthProvider
 * - Wraps all child pages with the dashboard UI structure
 *
 * @async
 * @component
 *
 * @param {Object} props - Component props
 * @param {React.ReactNode} props.children - Nested page content rendered inside the layout
 *
 * @returns {Promise<JSX.Element>} Rendered dashboard layout with sidebar and main content area
 */
async function HomeLayout({ children }) {
  const auth = await getAuthSystemInfoAsync();
  const t = await getTranslations("Dashboard");

  const routes = auth.routes.map((r) => ({
    path: r.path,
    label: t(r.labelKey),
  }));

  return (
    <AuthProvider
      initialAuth={{ role: auth.role, permissions: auth.permissions }}
    >
      <div className={styles.layoutWrapper}>
        <Sidebar routes={routes} />
        <main className={styles.mainContent}>{children}</main>
      </div>
    </AuthProvider>
  );
}

// Layout export
export default HomeLayout;
