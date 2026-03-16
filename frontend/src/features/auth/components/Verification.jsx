import React, { useEffect, useState, useTransition } from "react";
import styles from "./Verification.module.css";
import Input from "@/shared/components/input/Input";
import { useTranslations } from "use-intl";
import { useForm } from "@/shared/hooks/forms/useForm";
import { validateEmail } from "@/shared/utils/validations";
import { useToast } from "@/stores/toast/ToastContext";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  OTP: "OTP",
  PASSWORD: "PASSWORD",
};

function Verification({ setVerificationStep }) {
  const [step, setStep] = useState(VERIFICATION_STEP.EMAIL);

  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    validateEmail,
  );

  const tuser = useTranslations("User");
  const tverification = useTranslations("Authentication.Verification");

  const { addToast } = useToast();
  useEffect(() => {
    setVerificationStep(step);
  }, [step]);

  return (
    <>
      {step === VERIFICATION_STEP.EMAIL && (
        <>
          <p>{tverification("enterEmail")}</p>
          <Input
            name="email"
            type="text"
            placeholder={tuser("email")}
            value={formData.email}
            onChange={handleChange}
            error={errors.email}
            maxLength={32}
          />
        </>
      )}
    </>
  );
}

export default Verification;
