// Imports
import { useState } from "react";
import styles from "./Password.module.css";
import Input from "@/shared/components/input/Input";

/**
 * Password step component for authentication flow.
 *
 * Displays password input field with validation rules and translations.
 * Keeps local input state while reflecting external form state errors.
 *
 * @param {Object} props - Component props
 * @param {Object} props.state - External form state
 * @param {string} props.state.value - Initial password value
 * @param {string} [props.state.error] - Validation error message for password
 * @param {Object} props.t - Translation object
 * @param {string} props.t.title - Step title text
 * @param {Object} props.t.password - Password-related translations
 * @param {string} props.t.password.placeholder - Input placeholder text
 * @param {Object} props.t.password.validations - Validation messages
 * @param {string} props.t.password.validations.title - Validation rules section title
 * @param {string} props.t.password.validations.uppercase - Uppercase rule text
 * @param {string} props.t.password.validations.digit - Digit rule text
 * @param {string} props.t.password.validations.allowedChars - Allowed characters rule text
 * @param {string} props.t.password.validations.length - Length rule text
 *
 * @returns {JSX.Element} Rendered password step UI
 */
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

// Component export
export default Password;
