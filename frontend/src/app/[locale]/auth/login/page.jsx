import LoginForm from "@/features/auth/components/login/LoginForm";
import { getTranslations } from "next-intl/server";
import React from "react";

async function page() {
  const tLogin = await getTranslations("Login");
  const tInputs = await getTranslations("Inputs");

  return (
    <>
      <h1>{tLogin("data.title")}</h1>
      <LoginForm
        translations={{
          login: tLogin.raw("data"),
          inputs: tInputs.raw("data"),
        }}
      />
    </>
  );
}

export default page;
