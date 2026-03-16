"use client";

import React from "react";
import styles from "./Input.module.css";
import { useTranslations } from "next-intl";

function Input({
  name,
  type = "text",
  placeholder,
  value,
  onChange,
  error,
  maxLength,
  t,
}) {
  const tvalidations = useTranslations("Validations");

  return (
    <div className={styles.field}>
      <input
        name={name}
        type={type}
        placeholder={placeholder}
        value={value}
        onChange={onChange}
        maxLength={maxLength}
        className={error ? styles.inputError : ""}
      />

      {error && <p className={styles.error}>{tvalidations(error)}</p>}
    </div>
  );
}

export default Input;
