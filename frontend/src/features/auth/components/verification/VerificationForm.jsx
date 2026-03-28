"use client";

import React, { useState } from "react";
import Email from "./Email";
import Password from "./Password";
import Otp from "@/shared/components/otp/Otp";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  PASSWORD: "PASSWORD",
  OTP: "OTP",
};

function VerificationForm({ translations }) {
  const [step, setStep] = useState(VERIFICATION_STEP.EMAIL);

  return (
    <>
      {step === VERIFICATION_STEP.EMAIL && (
        <Email translations={translations} />
      )}
      {/* {step === VERIFICATION_STEP.PASSWORD && (
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
      )} */}
    </>
  );
}

export default VerificationForm;
