import React from "react";
import Image from "next/image";
import styles from "./page.module.css";
import { useTranslations } from "next-intl";

function page() {
  const t = useTranslations("Login");

  return (
    <section className={styles.page}>
      <div className={styles.wrapper}>
        <h1>{t("title")}</h1>
        <form action="" className={styles.form}>
          <input type="text" placeholder={t("email")} />
          <input type="password" placeholder={t("password")} />
          <button type="submit">{t("login")}</button>
        </form>
        <div className={styles.sso}>
          <button>
            <Image
              src="/assets/auth/outlook.svg"
              alt="Outlook SSO"
              width={24}
              height={24}
            />
            {t("sso")}
          </button>
        </div>
        <div className={styles.forgotPassword}>
          <p>{t("forgotPassword")}</p>
        </div>
        <div className={styles.copyright}>
          <p>{t("copyright")}</p>
        </div>
      </div>
    </section>
  );
}

export default page;
