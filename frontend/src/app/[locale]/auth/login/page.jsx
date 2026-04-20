// Imports
import { getTranslations } from "next-intl/server";
import LoginForm from "@/features/auth/components/login/LoginForm";

/**
 * Login page (Server Component).
 *
 * This page initializes the authentication login flow UI.
 *
 * Responsibilities:
 * - Loads localized translations for login page and input fields
 * - Structures translation object for LoginForm component
 * - Renders login form with proper i18n content
 * - Serves as entry point for user authentication (email/password login)
 *
 * This is a server-rendered Next.js page using next-intl for localization.
 *
 * @returns {JSX.Element} Login page with localized authentication form
 */
async function page() {
  const tLogin = await getTranslations("Login");
  const tInputs = await getTranslations("Inputs");

  const translations = {
    login: {
      title: tLogin("title"),
      signIn: tLogin("signIn"),
      verifying: tLogin("verifying"),
      success: tLogin("success"),
      errors: {
        userNotFound: tLogin("errors.userNotFound"),
        invalidCredentials: tLogin("errors.invalidCredentials"),
        notVerified: tLogin("errors.notVerified"),
        loginFailed: tLogin("errors.loginFailed"),
      },
    },
    inputs: {
      email: {
        placeholder: tInputs("email.placeholder"),
      },
      password: {
        placeholder: tInputs("password.placeholder"),
      },
    },
  };

  return (
    <>
      <h1>{tLogin("title")}</h1>
      <LoginForm t={translations} />
    </>
  );
}

// Page export
export default page;
