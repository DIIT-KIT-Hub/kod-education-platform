// SSR
"use server";

// Imports
import { loginAsync } from "../../services/authService";
import { validateLogin } from "../../utils/login/validations";
import { redirect } from "@/i18n/routing";
import { getLocale } from "next-intl/server";
import { setCookie } from "@/shared/services/cookieService";

/**
 * Server action for user login.
 *
 * Handles full authentication flow:
 * - Extracts email and password from FormData
 * - Validates input data
 * - Calls authentication API
 * - Stores access and refresh tokens in cookies
 * - Redirects user on successful login
 *
 * On validation failure:
 * - Returns field-level errors (422 status)
 *
 * On authentication failure:
 * - Returns API error status and preserves input values
 *
 * On success:
 * - Sets secure HTTP-only cookies
 * - Redirects user to home page based on locale
 *
 * @param {Object} prevState - Previous form state (unused but required by useActionState)
 * @param {FormData} formData - Submitted form data
 * @returns {Promise<Object|void>} Returns validation error state or redirects on success
 */
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

    await setCookie("access_token", response.accessToken, {
      expires: new Date(response.accessTokenExpiresAt),
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "strict",
    });

    await setCookie("refresh_token", response.refreshToken, {
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
