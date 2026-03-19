import React from "react";
import styles from "./layout.module.css";
import { getTranslations } from "next-intl/server";
import Additional from "@/features/auth/components/Additional";

async function AuthLayout({ children }) {
  const tLogin = await getTranslations("Auth.Login");
  const tVerification = await getTranslations("Auth.Verification");
  const tGlobal = await getTranslations("Global");

  return (
    <section className={styles.page}>
      <div className={styles.wrapper}>
        {children}
        <Additional
          translations={{
            forgotPassword: tLogin("forgotPassword"),
            verified: tVerification("verified"),
            notVerified: tLogin("notVerified"),
          }}
        />
        <div className={styles.copyright}>
          <p>{tGlobal("copyright")}</p>
        </div>
      </div>
    </section>
  );
}

export default AuthLayout;
