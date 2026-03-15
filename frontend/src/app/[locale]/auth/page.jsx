import React from "react";
import styles from "./page.module.css";

import Form from "@/features/auth/components/Form";

function page() {
  return (
    <section className={styles.page}>
      <Form />
    </section>
  );
}

export default page;
