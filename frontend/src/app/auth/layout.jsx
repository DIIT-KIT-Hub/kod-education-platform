"use client";

import React from "react";
import styles from "./layout.module.css";

function AuthLayout({ children }) {
  return (
    <section className={styles.page}>
      <section className={styles.formSide}>{children}</section>
      <aside className={styles.aside}>
        <div className={styles.asideInner}>
          <p className={styles.asideEyebrow}>Кабінет освітніх даних</p>
          <h2 className={styles.asideTitle}>Порядок у документах без паперу</h2>
          <p className={styles.asideText}>
            Довідки, накази й оголошення в одному просторі. Підписуйте
            електронно, знаходьте за секунди.
          </p>
        </div>
      </aside>
    </section>
  );
}

export default AuthLayout;
