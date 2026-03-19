import { useState } from "react";

export function useFormStep(initialStep) {
  const [step, setStep] = useState(initialStep);

  return { step, setStep };
}
