import VerificationForm from "@/features/auth/components/verification/VerificationForm";
import { getTranslations } from "next-intl/server";
import React from "react";

async function page() {
  const tVerification = await getTranslations("Verification");
  const tInputs = await getTranslations("Inputs");

  const translations = {
    verification: {
      title: tVerification("title"),
      email: {
        title: tVerification("email.title"),
        button: tVerification("email.button"),
        verifying: tVerification("email.verifying"),
        success: tVerification("email.success"),
      },
      password: {
        title: tVerification("password.title"),
        button: tVerification("password.button"),
        verifying: tVerification("password.verifying"),
        success: tVerification("password.success"),
      },
      otp: {
        title: tVerification("otp.title"),
        button: tVerification("otp.button"),
        verifying: tVerification("otp.verifying"),
        success: tVerification("otp.success"),
        notReceived: tVerification("otp.notReceived"),
        resend: tVerification("otp.resend"),
      },
      errors: {
        userAlreadyVerified: tVerification("errors.userAlreadyVerified"),
        tokenGenerationError: tVerification("errors.tokenGenerationError"),
        otpNotSent: tVerification("errors.otpNotSent"),
        otpCodeInvalid: tVerification("errors.otpCodeInvalid"),
        otpCodeExpired: tVerification("errors.otpCodeExpired"),
        verificationFailed: tVerification("errors.verificationFailed"),
        incorrectVerificationToken: tVerification(
          "errors.incorrectVerificationToken",
        ),
      },
    },
    inputs: {
      email: {
        placeholder: tInputs("email.placeholder"),
      },
      password: {
        placeholder: tInputs("password.placeholder"),
        validations: {
          title: tInputs("password.validations.title"),
          uppercase: tInputs("password.validations.uppercase"),
          digit: tInputs("password.validations.digit"),
          allowedChars: tInputs("password.validations.allowedChars"),
          length: tInputs("password.validations.length"),
          required: tInputs("password.validations.required"),
        },
      },
      otp: {
        validations: {
          required: tInputs("otp.validations.required"),
          length: tInputs("otp.validations.length"),
        },
      },
    },
  };

  return (
    <>
      <h1>{tVerification("title")}</h1>
      <VerificationForm t={translations} />
    </>
  );
}

export default page;
