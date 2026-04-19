// Imports
import Input from "@/shared/components/input/Input";

/**
 * Email step component for authentication flow.
 *
 * Displays an email input field with validation error support.
 * This is a controlled step within a multi-step auth process.
 *
 * @param {Object} props - Component props
 * @param {Object} props.state - External form state
 * @param {string} props.state.value - Current email value
 * @param {string} [props.state.error] - Validation error message for email
 * @param {Object} props.t - Translation object
 * @param {string} props.t.title - Step title text
 * @param {string} props.t.placeholder - Email input placeholder text
 *
 * @returns {JSX.Element} Rendered email step UI
 */
function Email({ state, t }) {
  return (
    <>
      <p>{t.title}</p>
      <Input
        name="email"
        type="text"
        placeholder={t.placeholder}
        defaultValue={state.value}
        error={state.error}
        maxLength={32}
      />
    </>
  );
}

// Component export
export default Email;
