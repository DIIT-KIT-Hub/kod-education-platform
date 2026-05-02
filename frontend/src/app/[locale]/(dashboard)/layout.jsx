import Sidebar from "@/shared/components/sidebar/Sidebar";
import styles from "./layout.module.css";
import { getAuthSystemInfoAsync } from "@/features/auth/actions/auth/actions";
import AuthProvider from "@/shared/stores/auth/AuthProvider";
import { getTranslations } from "next-intl/server";

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

export default HomeLayout;
