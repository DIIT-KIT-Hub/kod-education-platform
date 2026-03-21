"use client";

import React from "react";
import Input from "@/shared/components/input/Input";
import { createValidateLogin } from "../../utils/validations";
import { useForm } from "@/shared/hooks/forms/useForm";

function LoginForm({ translations }) {
  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    createValidateLogin({
      ...translations.inputs.email.validations,
      ...translations.inputs.password.validations,
    }),
  );

  const onSubmit = async (data) => {};

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <Input
        name="email"
        type="text"
        placeholder={translations.inputs.email.placeholder}
        value={formData.email}
        onChange={handleChange}
        error={errors.email}
        maxLength={32}
      />
      <Input
        name="password"
        type="password"
        placeholder={translations.inputs.password.placeholder}
        value={formData.password}
        onChange={handleChange}
        error={errors.password}
        maxLength={8}
      />
      <button type="submit">{translations.login.signIn}</button>
    </form>
  );
}

export default LoginForm;
