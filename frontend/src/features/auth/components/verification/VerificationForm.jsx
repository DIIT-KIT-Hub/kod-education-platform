"use client";

import React from "react";
import Email from "./Email";
import Password from "./Password";
import Otp from "@/shared/components/otp/Otp";
import {
  useVerificationFlow,
  VERIFICATION_STEP,
} from "../../hooks/useVerificationFlow";

function VerificationForm({ translations }) {
  const {
    step,
    formData,
    errors,
    isLoading,
    buttonText,
    handleChange,
    handleSubmit,
  } = useVerificationFlow(translations);

  return (
    <form onSubmit={handleSubmit}>
      {step === VERIFICATION_STEP.EMAIL && (
        <Email
          translations={{
            title: translations.verification.email.title,
            email: translations.inputs.email.placeholder,
          }}
          email={formData.email}
          handleChange={handleChange}
          error={errors.email}
        />
      )}
      {step === VERIFICATION_STEP.PASSWORD && (
        <Password
          translations={translations.verification.password}
          password={formData.password}
          handleChange={handleChange}
        />
      )}
      {step === VERIFICATION_STEP.OTP && (
        <Otp
          translations={translations.verification.otp}
          otpValue={formData.otpCode}
          handleOtpChange={(value) =>
            handleChange({ target: { name: "otpCode", value } })
          }
          error={errors.otpCode}
        />
      )}
      <button type="submit" disabled={isLoading}>
        {buttonText}
      </button>
    </form>
  );
}

export default VerificationForm;
