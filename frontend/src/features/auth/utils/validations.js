import { validateEmail } from "@/shared/utils/validations";

export const createValidateLogin = (validations) => (data) => {
  let errors = {};

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
