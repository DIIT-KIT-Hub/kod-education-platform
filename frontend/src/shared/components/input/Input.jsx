import React from "react";
import styles from "./Input.module.css";
import Error from "../error/Error";

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

      {error && <Error error={error} />}
    </div>
  );
}

export default Input;
