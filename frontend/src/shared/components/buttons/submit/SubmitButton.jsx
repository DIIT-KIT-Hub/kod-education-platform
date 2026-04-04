import { useToast } from "@/shared/hooks/toast/useToast";
import React, { useEffect, useRef } from "react";

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

export default SubmitButton;
