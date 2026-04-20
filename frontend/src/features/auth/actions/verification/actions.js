// SSR
"use server";

// Imports
import {
  generateVerificationTokenAsync,
  verifyUserAsync,
} from "../../services/verificationService";
import {
  validateEmailStep,
  validateOtpStep,
  validatePasswordStep,
} from "../../utils/verification/validations";
import { sendOtpForVerificationAsync } from "@/shared/services/otpService";
import { removeCookie, setCookie } from "@/shared/services/cookieService";
import { VERIFICATION_STEP } from "../../utils/constants/constants";

/**
 * Extracts form values based on current verification step.
 *
 * Ensures that only relevant fields are updated per step,
 * while preserving previously entered values from state.
 *
 * @param {Object} prevState - Previous form state
 * @param {Object} formData - Submitted form data
 * @returns {{
 *  email: string,
 *  password: string,
 *  otpCode: string
 * }} Normalized form values
 */
function getFormValues(prevState, formData) {
  return {
    email:
      prevState.step === VERIFICATION_STEP.EMAIL
        ? formData.get("email")?.trim() || ""
        : prevState.inputs.email.value,

    password:
      prevState.step === VERIFICATION_STEP.PASSWORD
        ? formData.get("password")?.trim() || ""
        : prevState.inputs.password.value,

    otpCode:
      prevState.step === VERIFICATION_STEP.OTP
        ? formData.get("otpCode")?.trim() || ""
        : prevState.inputs.otpCode.value,
  };
}

/**
 * Builds a new immutable verification form state.
 *
 * Preserves previous state while updating only changed fields.
 *
 * @param {Object} prevState - Previous state object
 * @param {Object} params - State update payload
 * @param {Object} params.inputs - Updated input values and errors
 * @param {number} params.status - HTTP-like status code
 * @param {number} params.step - Current verification step
 * @param {number|null} [params.expiresAt] - OTP expiration timestamp
 * @param {string|null} [params.intent] - Action intent (e.g. resend, verify)
 *
 * @returns {Object} Updated state object
 */
function buildState(prevState, { inputs, status, step, expiresAt, intent }) {
  return {
    ...prevState,
    inputs: {
      ...prevState.inputs,
      ...inputs,
    },
    status,
    step,
    timestamp: Date.now(),
    expiresAt: expiresAt ?? prevState.expiresAt,
    intent: intent ?? null,
  };
}

/**
 * Handles email step validation and token generation.
 *
 * Flow:
 * - Validates email format
 * - Generates verification token
 * - Stores token in cookies
 * - Advances to password step on success
 *
 * @param {Object} prevState - Previous form state
 * @param {string} email - User email
 * @returns {Promise<Object>} Updated state
 */
async function handleEmailStep(prevState, email) {
  const { errors, isValid } = await validateEmailStep(email);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { email: { value: email, error: errors.email } },
      status: 400,
      step: VERIFICATION_STEP.EMAIL,
    });
  }

  try {
    const res = await generateVerificationTokenAsync(email);

    await setCookie("verification_token", res.token, {
      expires: new Date(res.expiresAt),
      path: "/",
      secure: true,
      sameSite: "strict",
    });

    return buildState(prevState, {
      inputs: { email: { value: email, error: "" } },
      status: 200,
      step: VERIFICATION_STEP.PASSWORD,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { email: { value: email, error: "" } },
      status: e?.status || 500,
      step: VERIFICATION_STEP.OTP,
    });
  }
}

/**
 * Handles password step validation and OTP generation.
 *
 * Flow:
 * - Validates password strength
 * - Sends OTP to user email
 * - Moves to OTP step on success
 *
 * @param {Object} prevState - Previous form state
 * @param {string} email - User email
 * @param {string} password - User password
 * @returns {Promise<Object>} Updated state
 */
async function handlePasswordStep(prevState, email, password) {
  const { errors, isValid } = await validatePasswordStep(password);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { password: { value: password, error: errors.password } },
      status: 400,
      step: VERIFICATION_STEP.PASSWORD,
    });
  }

  try {
    const expiresAt = await sendOtpForVerificationAsync(email);

    return buildState(prevState, {
      inputs: { password: { value: password, error: "" } },
      status: 200,
      step: VERIFICATION_STEP.OTP,
      expiresAt: expiresAt,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { password: { value: password, error: "" } },
      status: e?.status || 500,
      step: VERIFICATION_STEP.PASSWORD,
    });
  }
}

/**
 * Handles OTP verification step.
 *
 * Flow:
 * - Validates OTP format
 * - Verifies user with email + password + OTP
 * - Removes verification token on success
 * - Completes verification flow
 *
 * @param {Object} prevState - Previous form state
 * @param {string} email - User email
 * @param {string} password - User password
 * @param {string} otpCode - OTP code
 * @returns {Promise<Object>} Updated state
 */
async function handleOtpStep(prevState, email, password, otpCode) {
  const { errors, isValid } = await validateOtpStep(otpCode);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: errors.otpCode } },
      status: 422,
      step: VERIFICATION_STEP.OTP,
    });
  }

  try {
    await verifyUserAsync({ email, password, otpCode });
    await removeCookie("verification_token");

    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: "" } },
      status: 200,
      step: VERIFICATION_STEP.NONE,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: "" } },
      status: e?.status || 500,
      step: VERIFICATION_STEP.OTP,
    });
  }
}

/**
 * Server action handling full multi-step verification flow.
 *
 * Supports:
 * - Email step (token generation)
 * - Password step (OTP sending)
 * - OTP step (final verification)
 * - OTP resend flow
 *
 * Flow is controlled by `step` and `intent` fields.
 *
 * @param {Object} prevState - Previous form state
 * @param {FormData} formData - Submitted form data
 * @returns {Promise<Object>} Updated verification state
 */
export async function verificationAction(prevState, formData) {
  const intent = formData.get("intent");
  const { email, password, otpCode } = getFormValues(prevState, formData);

  if (prevState.step === VERIFICATION_STEP.OTP && intent === "resend") {
    try {
      const expiresAt = await sendOtpForVerificationAsync(email);

      return buildState(prevState, {
        status: 200,
        step: VERIFICATION_STEP.OTP,
        expiresAt,
        intent: "resend",
        inputs: {
          otpCode: { value: "", error: "" },
        },
      });
    } catch (e) {
      return buildState(prevState, {
        inputs: { otpCode: { value: otpCode, error: "" } },
        status: e?.status || 500,
        step: VERIFICATION_STEP.OTP,
      });
    }
  }

  switch (prevState.step) {
    case 1:
      return handleEmailStep(prevState, email);

    case 2:
      return handlePasswordStep(prevState, email, password);

    case 3:
      return handleOtpStep(prevState, email, password, otpCode);

    default:
      return prevState;
  }
}
