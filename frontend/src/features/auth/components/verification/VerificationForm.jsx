"use client";

import React, { useState } from "react";
import Email from "./Email";
import Password from "./Password";
import Otp from "@/features/auth/components/verification/Otp";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  PASSWORD: "PASSWORD",
  OTP: "OTP",
};

function VerificationForm({ translations }) {
  const [step, setStep] = useState(VERIFICATION_STEP.EMAIL);
  const [formData, setFormData] = useState({});

  const handleNextStep = (nextStep, data = {}) => {
    setFormData((prev) => ({ ...prev, ...data }));
    setStep(nextStep);
  };

  return (
    <>
      {step === VERIFICATION_STEP.EMAIL && (
        <Email
          translations={translations}
          onNext={(data) => handleNextStep(VERIFICATION_STEP.PASSWORD, data)}
        />
      )}
      {step === VERIFICATION_STEP.PASSWORD && (
        <Password
          translations={translations}
          email={formData.email}
          onNext={(data) => handleNextStep(VERIFICATION_STEP.OTP, data)}
        />
      )}
      {step === VERIFICATION_STEP.OTP && (
        <Otp
          translations={translations}
          email={formData.email}
          password={formData.password}
        />
      )}
    </>
  );
}

export default VerificationForm;
