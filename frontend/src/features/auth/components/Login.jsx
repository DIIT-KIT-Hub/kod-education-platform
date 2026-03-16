import Input from "@/shared/components/input/Input";
import React from "react";
import { validateLogin } from "../utils/validations";
import { useForm } from "@/shared/hooks/forms/useForm";
import { useTranslations } from "next-intl";

function Login() {
  const tuser = useTranslations("User");
  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    validateLogin,
  );

  return (
    <>
      <Input
        name="email"
        type="text"
        placeholder={tuser("email")}
        value={formData.email}
        onChange={handleChange}
        error={errors.email}
        maxLength={32}
      />

      <Input
        name="password"
        type="password"
        placeholder={tuser("password")}
        value={formData.password}
        onChange={handleChange}
        error={errors.password}
        maxLength={8}
      />
    </>
  );
}

export default Login;
