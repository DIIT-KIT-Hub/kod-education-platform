"use server";

import { cookies } from "next/headers";
import {
  generateVerificationTokenAsync,
  verifyUserAsync,
} from "../../services/verificationService";
import { validateAllSteps } from "../../utils/verification/validations";
import { sendOtpForVerificationAsync } from "@/shared/services/otpService";

export async function emailStepAction(prevState, formData) {
  const email = formData.get("email")?.trim() || "";

  const { errors, isValid } = await validateAllSteps({ email: email }, [
    "email",
  ]);

  if (!isValid) {
    return {
      inputs: {
        errors: errors,
        values: { email: email },
      },
      status: 400,
      timestamp: Date.now(),
    };
  }

  try {
    const response = await generateVerificationTokenAsync(email);

    const cookieStore = await cookies();

    cookieStore.set("verification_token", response.token, {
      expires: new Date(response.expiresAt),
      path: "/",
      secure: true,
      sameSite: "strict",
    });

    return {
      inputs: {
        errors: {},
        values: { email: email },
      },
      status: 200,
      timestamp: Date.now(),
    };
  } catch (error) {
    console.error(error);

    return {
      inputs: {
        errors: {},
        values: { email: email },
      },
      status: error.status,
      timestamp: Date.now(),
    };
  }
}

export async function passwordStepAction(prevState, formData) {
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
