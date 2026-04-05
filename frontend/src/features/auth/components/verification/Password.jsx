import React, { useState } from "react";
import styles from "./Password.module.css";
import Input from "@/shared/components/input/Input";

function Password({ state, t }) {
  const [password, setPassword] = useState(state.value);

  const rules = [
    t.password.validations.uppercase,
    t.password.validations.digit,
    t.password.validations.allowedChars,
    t.password.validations.length,
  ];

  return (
    <>
      <p>{t.title}</p>
      <Input
        name="password"
        type="password"
        placeholder={t.password.placeholder}
        defaultValue={password}
        onChange={(e) => setPassword(e.target.value)}
        maxLength={32}
        error={state.error}
      />
      <div className={styles.rules}>
        <p>{t.password.validations.title}</p>
        <ul>
          {rules.map((text, index) => (
            <li key={index}>{text}</li>
          ))}
        </ul>
      </div>
    </>
  );
}

export default Password;
