import React from "react";
import styles from "./page.module.css";
import LoginForm from "@/features/auth/components/LoginForm";

function page() {
  return (
    <section className={styles.page}>
      <h1 className={styles.title}>Вхід до кабінету</h1>
      <p className={styles.subtitle}>
        Увійдіть, щоб працювати з документами вашого закладу
      </p>
      <LoginForm />
    </section>
  );
}

export default page;
