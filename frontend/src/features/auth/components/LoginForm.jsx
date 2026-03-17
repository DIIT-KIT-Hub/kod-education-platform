import Input from "@/shared/components/input/Input";
import React from "react";
import styles from "./Form.module.css";
import { validateLogin } from "../utils/validations";
import { useForm } from "@/shared/hooks/forms/useForm";
import { useTranslations } from "next-intl";

function LoginForm() {
  const tlogin = useTranslations("Authentication.Login");
  const tuser = useTranslations("User");
  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    validateLogin,
  );

  const onSubmit = async (data) => {};

  return (
    <form className={styles.form} onSubmit={handleSubmit(onSubmit)}>
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

      <button type="submit">{tlogin("login")}</button>
    </form>
  );
}

export default LoginForm;
