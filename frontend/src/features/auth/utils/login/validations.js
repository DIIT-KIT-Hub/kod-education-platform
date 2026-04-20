// Imports
import { validateEmail } from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";

/**
 * Validates login form data.
 *
 * Validation rules:
 * - Email is required and must be in valid format
 * - Password is required
 *
 * Uses i18n translations for error messages.
 *
 * @param {Object} data - Login form data
 * @param {string} data.email - User email address
 * @param {string} data.password - User password
 *
 * @returns {Promise<{errors: Object, isValid: boolean}>}
 * Validation result containing field errors and overall validity flag
 */
export async function validateLogin(data) {
  const tInputs = await getTranslations("Inputs");

  const errors = {};

  const email = data.email;

  if (!email) {
    errors.email = tInputs("email.validations.required");
  } else if (!validateEmail(email)) {
    errors.email = tInputs("email.validations.invalidEmail");
  }

  const password = data.password;
  if (!password) {
    errors.password = tInputs("password.validations.required");
  }

  return {
    errors,
    isValid: Object.keys(errors).length === 0,
  };
}
