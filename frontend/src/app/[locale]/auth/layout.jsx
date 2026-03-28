import React, { Suspense } from "react";
import styles from "./layout.module.css";
import { getTranslations } from "next-intl/server";
import Additional from "@/features/auth/components/Additional";

async function AuthLayout({ children }) {
  const tLayout = await getTranslations("AuthLayout.data");

  return (
    <section className={styles.page}>
      <div className={styles.wrapper}>
        {children}

        <Additional
          translations={{
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
