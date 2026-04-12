import {
  validateDigit,
  validateEmail,
  validateLength,
  validatePasswordAllowedChars,
  validateUppercase,
} from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";

export async function validateEmailStep(email) {
  const t = await getTranslations("Inputs.email.validations");

  let errors = {};

  if (!email) {
    errors.email = t("required");
  } else if (!validateEmail(email)) {
    errors.email = t("invalidEmail");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

export async function validatePasswordStep(password) {
  const t = await getTranslations("Inputs.password.validations");

  let errors = {};

  if (!password) {
    errors.password = t("required");
  } else if (!validateUppercase(password)) {
    errors.password = t("uppercase");
  } else if (!validateDigit(password)) {
    errors.password = t("digit");
  } else if (!validatePasswordAllowedChars(password)) {
    errors.password = t("allowedChars");
  } else if (!validateLength(password, 8, 32)) {
    errors.password = t("length");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

export async function validateOtpStep(otpCode) {
  const t = await getTranslations("Inputs.otp.validations");
  let errors = {};

  if (!otpCode) {
    errors.otpCode = t("required");
  } else if (otpCode.length !== 6) {
    errors.otpCode = t("length");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}
