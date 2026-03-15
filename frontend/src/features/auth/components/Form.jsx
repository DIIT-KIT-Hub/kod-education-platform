"use client";

import React from "react";
import styles from "./Form.module.css";
import { useTranslations } from "next-intl";
import { validateLogin } from "../utils/validations";
import { useForm } from "@/shared/hooks/forms/useForm";
import Input from "@/shared/components/input/Input";

function Form() {
  const t = useTranslations("Login");

  const { formData, errors, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    validateLogin,
  );

  const onSubmit = (data) => {
    console.log("submit", data);
  };

  return (
    <div className={styles.wrapper}>
      <h1>{t("title")}</h1>

      <form onSubmit={handleSubmit(onSubmit)} className={styles.form}>
        <Input
          name="email"
          type="text"
          placeholder={t("email")}
          value={formData.email}
          onChange={handleChange}
          error={errors.email}
          maxLength={32}
          t={t}
        />

        <Input
          name="password"
          type="password"
          placeholder={t("password")}
          value={formData.password}
          onChange={handleChange}
          error={errors.password}
          maxLength={8}
          t={t}
        />

        <button type="submit">{t("login")}</button>
      </form>

      <div className={styles.forgotPassword}>
        <p>{t("forgotPassword")}</p>
        <p>|</p>
        <p>{t("verification")}</p>
      </div>

      <div className={styles.copyright}>
        <p>{t("copyright")}</p>
      </div>
    </div>
  );
}

export default Form;
