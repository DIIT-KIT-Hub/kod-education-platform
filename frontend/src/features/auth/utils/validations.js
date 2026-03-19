import { validateEmail } from "@/shared/utils/validations";

export const createValidateLogin = (validations) => (data) => {
  let errors = {};

  console.log(validations);

  const email = data.email.trim();
  if (!email) {
    errors.email = validations.emailRequired;
  } else if (!validateEmail(email)) {
    errors.email = validations.invalidEmail;
  }

  const password = data.password.trim();
  if (!password) {
    errors.password = validations.passwordRequired;
  }

  return errors;
};

export const createValidateEmailVerification = (validations) => (data) => {
  let errors = {};
  const email = data.email.trim();
  if (!email) {
    errors.email = validations.emailRequired;
  } else if (!validateEmail(email)) {
    errors.email = validations.invalidEmail;
  }
  return errors;
};
