import React from "react";
import styles from "./Password.module.css";
import Input from "@/shared/components/input/Input";

function Password({ translations, password, handleChange }) {
  const rules = [
    {
      key: "uppercase",
      validate: (pw) => /[A-Z]/.test(pw),
      text: translations.passwordRules.uppercase,
    },
    {
      key: "digit",
      validate: (pw) => /[0-9]/.test(pw),
      text: translations.passwordRules.digit,
    },
    {
      key: "allowedChars",
      validate: (pw) => (pw.length > 0 ? /^[A-Za-z0-9]*$/.test(pw) : undefined),
      text: translations.passwordRules.allowedChars,
    },
    {
      key: "length",
      validate: (pw) => pw.length === 8,
      text: translations.passwordRules.length,
    },
  ];

  return (
    <>
      <p>{translations.enterPassword}</p>
      <Input
        name="password"
        type="password"
        placeholder={translations.password}
        value={password}
        onChange={handleChange}
        maxLength={8}
      />
      <div className={styles.rules}>
        <p>{translations.passwordRules.title}</p>
        <ul>
          {rules.map((rule) => {
            const valid = rule.validate(password);
            return (
              <li
                key={rule.key}
                className={valid ? styles.valid : styles.invalid}
              >
                {rule.text}
              </li>
            );
          })}
        </ul>
      </div>
    </>
  );
}

export default Password;
