/**
 * Available verification steps in the authentication flow.
 *
 * @readonly
 * @enum {string}
 *
 * @property {string} NONE - No verification step selected.
 * @property {string} EMAIL - Step where the user provides their email.
 * @property {string} PASSWORD - Step where the user enters their password.
 * @property {string} OTP - Step where the user confirms using a one-time password.
 */
export const VERIFICATION_STEP = {
  NONE: "NONE",
  EMAIL: "EMAIL",
  PASSWORD: "PASSWORD",
  OTP: "OTP",
};
