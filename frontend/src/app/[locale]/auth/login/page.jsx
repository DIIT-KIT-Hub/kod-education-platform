import LoginForm from "@/features/auth/components/login/LoginForm";
import { getTranslations } from "next-intl/server";
import React from "react";

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

export default page;
