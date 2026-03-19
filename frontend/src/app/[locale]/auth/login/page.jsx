import LoginForm from "@/features/auth/components/login/LoginForm";
import { getTranslations } from "next-intl/server";
import React from "react";

async function page() {
  const tLogin = await getTranslations("Auth.Login");
  const tInputs = await getTranslations("Inputs");
  const tValidations = await getTranslations("Validations");

  const translations = {
    login: {
      signIn: tLogin("signIn"),
      forgotPassword: tLogin("forgotPassword"),
      notVerified: tLogin("notVerified"),
    },
    inputs: {
      email: tInputs("Email"),
      password: tInputs("Password"),
    },
    validations: {
      invalidEmail: tValidations("invalidEmail"),
      emailRequired: tValidations("emailRequired"),
      passwordRequired: tValidations("passwordRequired"),
    },
  };

  return (
    <>
      <h1>{tLogin("title")}</h1>
      <LoginForm
        translations={{
          email: translations.inputs.email,
          password: translations.inputs.password,
          signIn: translations.login.signIn,
          validations: translations.validations,
        }}
      />
    </>
  );
}

export default page;
