"use client";

import React, { useState } from "react";
import styles from "./Form.module.css";
import { useTranslations } from "next-intl";
import LoginForm from "./LoginForm";
import VerificationForm from "./VerificationForm";

const AUTH_STEP = {
  LOGIN: "LOGIN",
  VERIFICATION: "VERIFICATION",
};

function Form() {
  const tlogin = useTranslations("Authentication.Login");
  const tverification = useTranslations("Authentication.Verification");
  const tglobal = useTranslations("Global");

  const [step, setStep] = useState(AUTH_STEP.LOGIN);

  const handleStep = () => {
    setStep((prev) =>
      prev === AUTH_STEP.LOGIN ? AUTH_STEP.VERIFICATION : AUTH_STEP.LOGIN,
    );
  };

  return (
    <div className={styles.wrapper}>
      <h1>
        {step === AUTH_STEP.LOGIN ? tlogin("title") : tverification("title")}
      </h1>

      {step === AUTH_STEP.LOGIN && <LoginForm />}
      {step === AUTH_STEP.VERIFICATION && <VerificationForm />}

      <div className={styles.additional}>
        <p>{tlogin("forgotPassword")}</p>
        <p>|</p>
        <p onClick={handleStep}>
          {step === AUTH_STEP.LOGIN
            ? tverification("notVerified")
            : tverification("verified")}
        </p>
      </div>

      <div className={styles.copyright}>
        <p>{tglobal("copyright")}</p>
      </div>
    </div>
  );
}

export default Form;
