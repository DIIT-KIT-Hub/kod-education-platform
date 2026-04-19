// Imports
import styles from "./Input.module.css";
import Error from "../error/Error";

/**
 * Reusable input field component with built-in error display.
 *
 * @param {Object} props - Component props
 * @param {string} props.name - Input field name attribute
 * @param {string} [props.type="text"] - Input type (e.g. text, password, email)
 * @param {string} [props.placeholder] - Placeholder text
 * @param {string} [props.defaultValue] - Default value of the input
 * @param {(event: React.ChangeEvent<HTMLInputElement>) => void} [props.onChange] - Change event handler
 * @param {string} [props.error] - Error message to display below input
 * @param {number} [props.maxLength] - Maximum allowed input length
 *
 * @returns {JSX.Element} Rendered input component
 */
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

// Component export
export default Input;
