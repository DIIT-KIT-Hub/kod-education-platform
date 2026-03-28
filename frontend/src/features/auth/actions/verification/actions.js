"use server";

import { cookies } from "next/headers";
import { generateVerificationTokenAsync } from "../../services/verificationService";
import { validateEmailStep } from "../../utils/verification/validations";

export async function emailStepAction(prevState, formData) {
  const email = formData.get("email")?.trim() || "";

  const { errors, isValid } = await validateEmailStep(email);

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
        values: {},
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
