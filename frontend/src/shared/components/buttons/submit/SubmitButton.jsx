import { useToast } from "@/shared/hooks/toast/useToast";
import React, { useEffect, useRef } from "react";
import { useFormStatus } from "react-dom";

function SubmitButton({ text, loadingText, disabled = false }) {
  const { pending } = useFormStatus();
  const { loading, dismiss } = useToast();

  const toastIdRef = useRef(null);

  useEffect(() => {
    if (pending) {
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
  }, [pending]);

  return (
    <button type="submit" disabled={pending || disabled}>
      {text}
    </button>
  );
}

export default SubmitButton;
