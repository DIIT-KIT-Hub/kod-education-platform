import React from "react";
import styles from "./Input.module.css";
import Error from "../error/Error";

function Input({
  name,
  type = "text",
  placeholder,
  defaultValue,
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
        defaultValue={defaultValue}
        onChange={onChange}
        maxLength={maxLength}
        className={error ? styles.inputError : ""}
      />

      {error && <Error error={error} />}
    </div>
  );
}

export default Input;
