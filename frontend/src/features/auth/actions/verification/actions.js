"use server";

import { cookies } from "next/headers";
import {
  generateVerificationTokenAsync,
  verifyUserAsync,
} from "../../services/verificationService";
import {
  validateAllSteps,
  validateEmailStep,
  validatePasswordStep,
} from "../../utils/verification/validations";
import { sendOtpForVerificationAsync } from "@/shared/services/otpService";
import { getCookie, setCookie } from "@/shared/services/cookieService";

async function emailStepAction(email) {
  try {
    const response = await generateVerificationTokenAsync(email);

    await setCookie("verification_token", response.token, {
      expires: new Date(response.expiresAt),
      path: "/",
      secure: true,
      sameSite: "strict",
    });

    return {
      inputs: {
        email: { value: email, error: "" },
      },
      status: 200,
      step: 2,
      timestamp: Date.now(),
    };
  } catch (error) {
    console.error(error);

    return {
      inputs: {
        email: { value: email, error: "" },
      },
      status: error?.status || 500,
      step: 1,
      timestamp: Date.now(),
    };
  }
}

async function passwordStepAction(prevState, formData) {
  const email = formData.get("email")?.trim() || "";
  const password = formData.get("password")?.trim() || "";

  const { errors, isValid } = await validateAllSteps(
    { email: email, password: password },
    ["email", "password"],
  );

  if (errors.token) {
    return {
      inputs: {
        errors: errors,
        values: { password: password },
      },
      status: 401,
      timestamp: Date.now(),
    };
  }

  if (!isValid) {
    return {
      inputs: {
        errors: errors,
        values: { password: password },
      },
      status: 400,
      timestamp: Date.now(),
    };
  }

  try {
    await sendOtpForVerificationAsync(email);

    return {
      inputs: {
        errors: {},
        values: { email: email, password: password },
      },
      status: 200,
      timestamp: Date.now(),
    };
  } catch (error) {
    console.error(error);

    return {
      inputs: {
        errors: {},
        values: { password: password },
      },
      status: error.status,
      timestamp: Date.now(),
    };
  }
}

export async function verificationAction(prevState, formData) {
  let email = "";

  if (prevState.step === 1) {
    email = formData.get("email")?.trim() || "";
  } else {
    email = prevState.inputs.email.value;
  }

  const { errors: emailErrors, isValid: emailIsValid } =
    await validateEmailStep(email);

  if (!emailIsValid) {
    return {
      ...prevState,
      inputs: {
        ...prevState.inputs,
        email: { value: email, error: emailErrors.email },
      },
      status: 400,
      step: 1,
      timestamp: Date.now(),
    };
  }

  if (prevState.step === 1) {
    const emailResult = await emailStepAction(email);

    return {
      ...prevState,
      inputs: {
        ...prevState.inputs,
        email: {
          value: email,
          error: "",
        },
      },
      status: emailResult.status,
      step: emailResult.status === 200 ? 2 : 1,
      timestamp: emailResult.timestamp,
    };
  }

  let password = "";

  if (prevState.step === 2) {
    password = formData.get("password")?.trim() || "";
  } else {
    password = prevState.inputs.password.value;
  }

  if (prevState.step === 2) {
    const verificationToken = getCookie("verification_token");

    if (!verificationToken) {
      return {
        ...prevState,
        inputs: {
          ...prevState.inputs,
          password: { value: password, error: "" },
        },
        status: 401,
        step: 2,
        timestamp: Date.now(),
      };
    }

    const { errors: passwordErrors, isValid: passwordIsValid } =
      await validatePasswordStep(password);

    if (!passwordIsValid) {
      return {
        ...prevState,
        inputs: {
          ...prevState.inputs,
          password: { value: password, error: passwordErrors.password },
        },
        status: 400,
        step: 2,
        timestamp: Date.now(),
      };
    }
  }
}

export async function otpStepAction(prevState, formData) {
  const email = formData.get("email")?.trim() || "";
  const password = formData.get("password")?.trim() || "";
  const otpCode = formData.get("otpCode")?.trim() || "";

  const { errors, isValid } = await validateAllSteps(
    { email: email, password: password, otpCode: otpCode },
    ["email", "password", "otpCode"],
  );

  if (errors.token) {
    return {
      inputs: {
        errors: errors,
        values: { otpCode: otpCode },
      },
      status: 401,
      timestamp: Date.now(),
    };
  }

  if (!isValid) {
    return {
      inputs: {
        errors: errors,
        values: { otpCode: otpCode },
      },
      status: 422,
      timestamp: Date.now(),
    };
  }

  console.log(234234);
  try {
    await verifyUserAsync({
      email: email,
      password: password,
      otpCode: otpCode,
    });

    const cookieStore = await cookies();

    cookieStore.delete("verification_token");
  } catch (error) {
    console.error(error);

    return {
      inputs: {
        errors: {},
        values: { otp: otpCode },
      },
      status: error.status,
      timestamp: Date.now(),
    };
  }
}
