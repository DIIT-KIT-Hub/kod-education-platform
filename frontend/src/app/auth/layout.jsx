"use client";

import React from "react";
import styles from "./layout.module.css";

function AuthLayout({ children }) {
  return (
    <section className={styles.page}>
      <div className={styles.formSide}></div>
      <aside className={styles.aside}>
        <div className={styles.asideInner}>
          <p className={styles.asideEyebrow}>Кабінет освітніх даних</p>
          <h2 className={styles.asideTitle}>Дані, які не губляться</h2>
          <p className={styles.asideText}>
            Єдиний простір для довідок, оголошень і наказів. Кожна дія
            фіксується, кожен доступ — під контролем.
          </p>
        </div>
      </aside>
    </section>
  );
}

export default AuthLayout;
