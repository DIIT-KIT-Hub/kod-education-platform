"use client";

import React, { useState } from "react";
import styles from "./Form.module.css";
import { useTranslations } from "next-intl";

import Login from "./Login";
import Verification from "./Verification";

const AUTH_STEP = {
  LOGIN: "LOGIN",
  VERIFICATION: "VERIFICATION",
};

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  OTP: "OTP",
  PASSWORD: "PASSWORD",
};

function Form() {
  const tlogin = useTranslations("Authentication.Login");
  const tverification = useTranslations("Authentication.Verification");
  const tglobal = useTranslations("Global");

  const [verificationStep, setVerificationStep] = useState(null);

  const [step, setStep] = useState(AUTH_STEP.LOGIN);

  const onSubmit = (data) => {
    console.log("submit", data);
  };

  const handleStep = () => {
    setStep((prev) =>
      prev === AUTH_STEP.LOGIN ? AUTH_STEP.VERIFICATION : AUTH_STEP.LOGIN,
    );
  };

  const getButtonText = () => {
    if (step === AUTH_STEP.LOGIN) {
      return tlogin("login");
    }

    if (
      step === AUTH_STEP.VERIFICATION &&
      verificationStep === VERIFICATION_STEP.EMAIL
    ) {
      return tverification("requestOtp");
    }
  };

  return (
    <div className={styles.wrapper}>
      <h1>
        {step === AUTH_STEP.LOGIN ? tlogin("title") : tverification("title")}
      </h1>

      <form className={styles.form}>
        {step === AUTH_STEP.LOGIN && <Login />}
        {step === AUTH_STEP.VERIFICATION && (
          <Verification setVerificationStep={setVerificationStep} />
        )}
        <button type="submit">{getButtonText()}</button>
      </form>

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
