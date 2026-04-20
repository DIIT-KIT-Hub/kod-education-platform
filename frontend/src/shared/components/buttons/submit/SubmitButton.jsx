// Imports
import { useEffect, useRef } from "react";
import { useToast } from "@/shared/hooks/toast/useToast";

/**
 * Submit button component with built-in loading toast handling.
 *
 * Displays a loading toast while a form is in pending state and
 * automatically dismisses it when the state changes.
 *
 * @param {Object} props - Component props
 * @param {string} props.text - Button label text
 * @param {string} props.loadingText - Text shown in loading toast
 * @param {boolean} props.isPending - Indicates if form submission is in progress
 * @param {boolean} [props.disabled=false] - Manually disables the button
 *
 * @returns {JSX.Element} Rendered submit button
 */
function SubmitButton({ text, loadingText, isPending, disabled = false }) {
  const { loading, dismiss } = useToast();

  const toastIdRef = useRef(null);

  useEffect(() => {
    if (isPending) {
      toastIdRef.current = loading(loadingText);
    } else {
      if (toastIdRef.current) {
        dismiss(toastIdRef.current);
        toastIdRef.current = null;
      }
    }

    return () => {
      if (toastIdRef.current) {
        dismiss(toastIdRef.current);
      }
    };
  }, [isPending]);

  return (
    <button type="submit" disabled={isPending || disabled}>
      {text}
    </button>
  );
}

// Component export
export default SubmitButton;
