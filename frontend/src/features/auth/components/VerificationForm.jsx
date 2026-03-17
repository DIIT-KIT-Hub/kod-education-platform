import React, { useState } from "react";

import formStyles from "./Form.module.css";
import Input from "@/shared/components/input/Input";
import { useTranslations } from "use-intl";
import { useForm } from "@/shared/hooks/forms/useForm";

import {
  checkUserExistenceByEmailAsync,
  checkUserVerificationByEmailAsync,
} from "@/shared/services/authService";
import { validateEmailVerification } from "../utils/validations";
import toast from "react-hot-toast";
import { useToast } from "@/shared/hooks/toast/useToast";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  OTP: "OTP",
  PASSWORD: "PASSWORD",
};

function VerificationForm() {
  const [step, setStep] = useState(VERIFICATION_STEP.EMAIL);

  const { formData, errors, setErrors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    (data) => {
      if (step === VERIFICATION_STEP.EMAIL) {
        return validateEmailVerification(data);
      }

      return {};
    },
  );

  const tuser = useTranslations("User");
  const tverification = useTranslations("Authentication.Verification");

  const { success, error, warning, serverError } = useToast();

  const onEmailSubmit = async (email) => {
    try {
      let exists = await checkUserExistenceByEmailAsync(email);

      if (!exists) {
        error("user_not_found");
        return;
      }

      let isVerified = await checkUserVerificationByEmailAsync(email);

      if (isVerified) {
        warning("user_already_verified");

        return;
      }
    } catch (error) {
      console.error("Error checking user existence:", error);

      serverError();
    }
  };

  const onSubmit = async (data) => {
    switch (step) {
      case VERIFICATION_STEP.EMAIL: {
        await onEmailSubmit(data.email);
        break;
      }
    }
  };

  return (
    <form className={formStyles.form} onSubmit={handleSubmit(onSubmit)}>
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
      <button type="submit">{tverification("startVerification")}</button>
    </form>
  );
}

export default VerificationForm;
