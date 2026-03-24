"use client";

import React from "react";
import Input from "@/shared/components/input/Input";
import { useLoginFlow } from "../../hooks/useLoginFlow";

function LoginForm({ translations }) {
  const { formData, errors, isLoading, handleChange, handleSubmit } =
    useLoginFlow(translations);

  return (
    <form onSubmit={handleSubmit}>
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
      <button type="submit" disabled={isLoading}>
        {translations.login.signIn}
      </button>
    </form>
  );
}

export default LoginForm;
