import { validateEmail } from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";

export async function validateEmailStep(email) {
  const tEmailValidations = await getTranslations(
    "Inputs.data.email.validations",
  );

  let errors = {};

  if (!email) {
    errors.email = tEmailValidations("emailRequired");
  } else if (!validateEmail(email)) {
    errors.email = tEmailValidations("invalidEmail");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

export const createValidateOtpVerification = (validations) => (data) => {
  let errors = {};

  const otp = data.otpCode.trim();

  if (!otp) {
    errors.otpCode = validations.otpRequired;
  } else if (otp.length !== 6) {
    errors.otpCode = validations.otpLengthInvalid;
  }

  return errors;
};
