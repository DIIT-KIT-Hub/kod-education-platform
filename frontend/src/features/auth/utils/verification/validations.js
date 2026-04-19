// Imports
import {
  validateDigit,
  validateEmail,
  validateLength,
  validatePasswordAllowedChars,
  validateUppercase,
} from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";

/**
 * Validates email input for the email step.
 *
 * Rules:
 * - Field is required
 * - Must match valid email format
 *
 * @param {string} email - Email input value
 * @returns {Promise<{errors: Object, isValid: boolean}>}
 * Validation result containing errors and validity flag
 */
export async function validateEmailStep(email) {
  const t = await getTranslations("Inputs.email.validations");

  const errors = {};

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

/**
 * Validates password input for the password step.
 *
 * Rules:
 * - Required field
 * - Must contain at least one uppercase letter
 * - Must contain at least one digit
 * - Must contain only allowed characters (A–Z, a–z, 0–9)
 * - Must be between 8 and 32 characters
 *
 * @param {string} password - Password input value
 * @returns {Promise<{errors: Object, isValid: boolean}>}
 * Validation result containing errors and validity flag
 */
export async function validatePasswordStep(password) {
  const t = await getTranslations("Inputs.password.validations");

  const errors = {};

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

/**
 * Validates OTP code input for verification step.
 *
 * Rules:
 * - Required field
 * - Must be exactly 6 characters long
 *
 * @param {string} otpCode - OTP code input value
 * @returns {Promise<{errors: Object, isValid: boolean}>}
 * Validation result containing errors and validity flag
 */
export async function validateOtpStep(otpCode) {
  const t = await getTranslations("Inputs.otp.validations");
  const errors = {};

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
