import { validateEmail } from "@/shared/utils/validations";

export const validateLogin = (data) => {
  let errors = {};

  let email = data.email.trim();

  if (email.length === 0) {
    errors.email = "email_required";
  } else if (!validateEmail(email)) {
    errors.email = "invalid_email";
  }

  let password = data.password.trim();

  if (password.length === 0) {
    errors.password = "password_required";
  }

  return errors;
};

export const validateEmailVerification = (data) => {
  let errors = {};

  let email = data.email.trim();

  if (email.length === 0) {
    errors.email = "email_required";
  } else if (!validateEmail(email)) {
    errors.email = "invalid_email";
  }

  return errors;
};
