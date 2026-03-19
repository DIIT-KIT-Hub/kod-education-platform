import React from "react";
import styles from "./Input.module.css";

function Input({
  name,
  type = "text",
  placeholder,
  value,
  onChange,
  error,
  maxLength,
}) {
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

      {error && <p className={styles.error}>{error}</p>}
    </div>
  );
}

export default Input;
