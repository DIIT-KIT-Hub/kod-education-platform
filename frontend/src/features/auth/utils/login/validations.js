import { validateEmail } from "@/shared/utils/validations";
import { getTranslations } from "next-intl/server";

export async function validateLogin(data) {
  const tInputs = await getTranslations("Inputs");

  let errors = {};

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
