"use server";

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
import {
  getCookie,
  removeCookie,
  setCookie,
} from "@/shared/services/cookieService";

function getFormValues(prevState, formData) {
  return {
    email:
      prevState.step === 1
        ? formData.get("email")?.trim() || ""
        : prevState.inputs.email.value,

    password:
      prevState.step === 2
        ? formData.get("password")?.trim() || ""
        : prevState.inputs.password.value,

    otpCode:
      prevState.step === 3
        ? formData.get("otpCode")?.trim() || ""
        : prevState.inputs.otpCode.value,
  };
}

function buildState(prevState, { inputs, status, step }) {
  return {
    ...prevState,
    inputs: {
      ...prevState.inputs,
      ...inputs,
    },
    status,
    step,
    timestamp: Date.now(),
  };
}

async function handleEmailStep(prevState, email) {
  const { errors, isValid } = await validateEmailStep(email);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { email: { value: email, error: errors.email } },
      status: 400,
      step: 1,
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
      step: 2,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { email: { value: email, error: "" } },
      status: e?.status || 500,
      step: 1,
    });
  }
}

async function handlePasswordStep(prevState, email, password) {
  const token = getCookie("verification_token");

  if (!token) {
    return buildState(prevState, {
      inputs: { password: { value: password, error: "" } },
      status: 401,
      step: 2,
    });
  }

  const { errors, isValid } = await validatePasswordStep(password);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { password: { value: password, error: errors.password } },
      status: 400,
      step: 2,
    });
  }

  try {
    await sendOtpForVerificationAsync(email);

    return buildState(prevState, {
      inputs: { password: { value: password, error: "" } },
      status: 200,
      step: 3,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { password: { value: password, error: "" } },
      status: e?.status || 500,
      step: 2,
    });
  }
}

async function handleOtpStep(prevState, email, password, otpCode) {
  const token = getCookie("verification_token");

  if (!token) {
    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: "" } },
      status: 401,
      step: 3,
    });
  }

  const { errors, isValid } = await validateOtpStep(otpCode);

  if (!isValid) {
    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: errors.otpCode } },
      status: 400,
      step: 3,
    });
  }

  try {
    await verifyUserAsync({ email, password, otpCode });
    await removeCookie("verification_token");

    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: "" } },
      status: 200,
      step: 0,
    });
  } catch (e) {
    return buildState(prevState, {
      inputs: { otpCode: { value: otpCode, error: "" } },
      status: e?.status || 500,
      step: 3,
    });
  }
}

export async function verificationAction(prevState, formData) {
  const { email, password, otpCode } = getFormValues(prevState, formData);

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
