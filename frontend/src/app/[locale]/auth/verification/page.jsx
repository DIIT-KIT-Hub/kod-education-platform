import VerificationForm from "@/features/auth/components/verification/VerificationForm";
import { getTranslations } from "next-intl/server";
import React from "react";

async function page() {
  const tVerification = await getTranslations("Auth.Verification");
  const tInputs = await getTranslations("Inputs");
  const tValidations = await getTranslations("Validations");

  const translations = {
    verification: {
      verified: tVerification("verified"),
      enterEmail: tVerification("enterEmail"),
      startVerification: tVerification("startVerification"),
      enterPassword: tVerification("enterPassword"),
      passwordRules: {
        title: tVerification("PasswordRules.title"),
        uppercase: tVerification("PasswordRules.uppercase"),
        digit: tVerification("PasswordRules.digit"),
        allowedChars: tVerification("PasswordRules.allowedChars"),
        length: tVerification("PasswordRules.length"),
      },
      proceedOtp: tVerification("proceedOtp"),
      otp: {
        title: tVerification("Otp.title"),
        notReceived: tVerification("Otp.notReceived"),
        resend: tVerification("Otp.resend"),
        verify: tVerification("Otp.verify"),
      },
    },
    inputs: {
      email: tInputs("Email"),
      password: tInputs("Password"),
    },
    validations: {
      invalidEmail: tValidations("invalidEmail"),
      emailRequired: tValidations("emailRequired"),
      passwordRequired: tValidations("passwordRequired"),
    },
  };

  return (
    <>
      <h1>{tVerification("title")}</h1>
      <VerificationForm translations={translations} />
    </>
  );
}

export default page;
