"use server";

import { cookies } from "next/headers";
import { loginAsync } from "../../services/authService";
import { validateLogin } from "../../utils/login/validations";
import { redirect } from "@/i18n/routing";
import { getLocale } from "next-intl/server";

export async function loginAction(prevState, formData) {
  const loginData = {
    email: formData.get("email")?.trim() || "",
    password: formData.get("password")?.trim() || "",
  };

  const { errors, isValid } = await validateLogin(loginData);

  if (!isValid) {
    return {
      success: false,
      inputs: {
        errors: errors,
        values: loginData,
      },
      status: 422,
      timestamp: Date.now(),
    };
  }

  let isSuccess = false;

  try {
    const response = await loginAsync(loginData);

    const cookieStore = await cookies();

    cookieStore.set("access_token", response.accessToken, {
      expires: new Date(response.accessTokenExpiresAt),
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "strict",
    });

    cookieStore.set("refresh_token", response.refreshToken, {
      expires: new Date(response.refreshTokenExpiresAt),
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "strict",
    });

    isSuccess = true;
  } catch (error) {
    console.error(error);

    return {
      success: false,
      inputs: {
        errors: {},
        values: loginData,
      },
      status: error.status,
      timestamp: Date.now(),
    };
  }

  if (isSuccess) {
    const locale = await getLocale();

    redirect({ href: "/", locale: locale });
  }
}
