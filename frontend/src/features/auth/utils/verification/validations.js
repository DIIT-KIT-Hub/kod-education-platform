import {
  validateDigit,
  validateEmail,
  validateLength,
  validatePasswordAllowedChars,
  validateUppercase,
} from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";
import { cookies } from "next/headers";

async function validateEmailStep(email) {
  const t = await getTranslations("Inputs.data.email.validations");

  let errors = {};

  if (!email) {
    errors.email = t("emailRequired");
  } else if (!validateEmail(email)) {
    errors.email = t("invalidEmail");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

async function validatePasswordStep(password) {
  const t = await getTranslations("Inputs.data.password.validations");

  let errors = {};

  if (!password) {
    errors.password = t("passwordRequired");
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

async function validateOtpStep(otpCode) {
  const t = await getTranslations("Inputs.data.otp.validations");
  let errors = {};

  if (!otpCode) {
    errors.otpCode = t("otpRequired");
  } else if (otpCode.length !== 6) {
    errors.otpCode = t("otpLengthInvalid");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

async function validateVerificationToken() {
  const cookieStore = await cookies();

  const verificationToken = cookieStore.get("verification_token")?.value;

  let errors = {};

  const t = await getTranslations("Verification.data.errors");
  if (!verificationToken) {
    errors.token = t("incorrectVerificationToken");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}

export async function validateAllSteps(data, steps) {
  if (steps.includes("email")) {
    const validationResult = await validateEmailStep(data.email);

    if (!validationResult.isValid) {
      return validationResult;
    }
  }

  if (steps.includes("password") || steps.includes("otpCode")) {
    const validationResult = await validateVerificationToken();

    if (!validationResult.isValid) {
      return validationResult;
    }
  }

  if (steps.includes("password")) {
    const validationResult = await validatePasswordStep(data.password);

    if (!validationResult.isValid) {
      return validationResult;
    }
  }

  if (steps.includes("otpCode")) {
    const validationResult = await validateOtpStep(data.otpCode);

    if (!validationResult.isValid) {
      return validationResult;
    }
  }

  return { errors: {}, isValid: true };
}
