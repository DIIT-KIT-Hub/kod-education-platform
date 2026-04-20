// Imports
import { getTranslations } from "next-intl/server";
import VerificationForm from "@/features/auth/components/verification/VerificationForm";

/**
 * Verification page (Server Component).
 *
 * This page initializes the full multi-step verification flow UI.
 *
 * Responsibilities:
 * - Loads all required translations for verification flow and inputs
 * - Structures translation object for nested authentication steps
 * - Renders VerificationForm with localized content
 * - Serves as entry point for email → password → OTP verification flow
 *
 * This is a server-rendered Next.js page using next-intl for i18n.
 *
 * @returns {JSX.Element} Verification page with localized form
 */
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
        errors: {
          userNotFound: tVerification("email.errors.userNotFound"),
          userAlreadyVerified: tVerification(
            "email.errors.userAlreadyVerified",
          ),
          tokenGenerationError: tVerification(
            "email.errors.tokenGenerationError",
          ),
        },
      },
      password: {
        title: tVerification("password.title"),
        button: tVerification("password.button"),
        verifying: tVerification("password.verifying"),
        success: tVerification("password.success"),
        errors: {
          otpNotSent: tVerification("password.errors.otpNotSent"),
        },
      },
      otp: {
        title: tVerification("otp.title"),
        buttonVerify: tVerification("otp.buttonVerify"),
        buttonResend: tVerification("otp.buttonResend"),
        verifying: tVerification("otp.verifying"),
        success: tVerification("otp.success"),
        notReceived: tVerification("otp.notReceived"),
        otpCodeExpired: tVerification("otp.otpCodeExpired"),
        resending: tVerification("otp.resending"),
        resend: tVerification("otp.resend"),
        errors: {
          otpNotResent: tVerification("otp.errors.otpNotResent"),
          otpCodeInvalid: tVerification("otp.errors.otpCodeInvalid"),
        },
      },
      errors: {
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

// Page export
export default page;
