"use client";

import React from "react";

import { useForm } from "@/shared/hooks/forms/useForm";

import {
  checkUserExistenceByEmailAsync,
  checkUserVerificationByEmailAsync,
} from "@/shared/services/authService";
import { createValidateEmailVerification } from "../../utils/validations";

import { useToast } from "@/shared/hooks/toast/useToast";
import Email from "./Email";
import Password from "./Password";
import { useFormStep } from "@/shared/hooks/forms/useFormStep";
import Otp from "@/shared/components/otp/Otp";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  PASSWORD: "PASSWORD",
  OTP: "OTP",
};

function VerificationForm({ translations }) {
  const validateEmailStep = createValidateEmailVerification(
    translations.validations,
  );
  const { step, setStep } = useFormStep(VERIFICATION_STEP.EMAIL);

  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    (data) => {
      if (step === VERIFICATION_STEP.EMAIL) {
        return validateEmailStep(data);
      }

      return {};
    },
  );

  const { success, error, warning, serverError } = useToast();

  const onEmailSubmit = async (email) => {
    try {
      // let exists = await checkUserExistenceByEmailAsync(email);

      // if (!exists) {
      //   error("user_not_found");
      //   return;
      // }

      // let isVerified = await checkUserVerificationByEmailAsync(email);

      // if (isVerified) {
      //   warning("user_already_verified");

      //   return;
      // }

      setStep(VERIFICATION_STEP.OTP);
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

  const getSubmitButtonText = () => {
    switch (step) {
      case VERIFICATION_STEP.EMAIL: {
        return translations.verification.startVerification;
        break;
      }
      case VERIFICATION_STEP.PASSWORD: {
        return translations.verification.proceedOtp;
        break;
      }
      case VERIFICATION_STEP.OTP: {
        return translations.verification.otp.verify;
        break;
      }
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {step === VERIFICATION_STEP.EMAIL && (
        <Email
          translations={{
            enterEmail: translations.verification.enterEmail,
            email: translations.inputs.email,
          }}
          email={formData.email}
          handleChange={handleChange}
          error={errors.email}
        />
      )}
      {step === VERIFICATION_STEP.PASSWORD && (
        <Password
          translations={{
            enterPassword: translations.verification.enterPassword,
            passwordRules: translations.verification.passwordRules,
            password: translations.inputs.password,
          }}
          password={formData.password}
          handleChange={handleChange}
        />
      )}
      {step === VERIFICATION_STEP.OTP && (
        <Otp translations={translations.verification.otp} />
      )}
      <button type="submit">{getSubmitButtonText()}</button>
    </form>
  );
}

export default VerificationForm;
