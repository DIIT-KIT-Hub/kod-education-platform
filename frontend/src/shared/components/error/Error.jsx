// Imports
import styles from "./Error.module.css";

/**
 * Simple error message display component.
 *
 * @param {Object} props - Component props
 * @param {string} props.error - Error message text to display
 *
 * @returns {JSX.Element} Rendered error message element
 */
function Error({ error }) {
  return <p className={styles.error}>{error}</p>;
}

// Component export
export default Error;
