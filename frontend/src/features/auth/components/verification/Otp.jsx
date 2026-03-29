"use client";
import React, { useActionState, useEffect, useRef, useState } from "react";
import styles from "./Otp.module.css";
import Error from "../../../../shared/components/error/Error";
import SubmitButton from "../../../../shared/components/buttons/submit/SubmitButton";
import { useToast } from "@/shared/hooks/toast/useToast";

import { otpStepAction } from "../../actions/verification/actions";
import { useRouter } from "next/navigation";

const initialState = {
  inputs: {
    errors: {},
    values: {},
  },
  status: 0,
  timestamp: 0,
};

function Otp({ translations, email, password }) {
  const [otp, setOtp] = useState(new Array(6).fill(""));

  const [timer, setTimer] = useState(300);
  const [canResend, setCanResend] = useState(false);
  const inputsRef = useRef([]);
  const router = useRouter();
  const [state, formAction] = useActionState(otpStepAction, initialState);

  const { success, error } = useToast();

  useEffect(() => {
    let interval;
    if (timer > 0) {
      interval = setInterval(() => setTimer((prev) => prev - 1), 1000);
    } else {
      setCanResend(true);
      clearInterval(interval);
    }
    return () => clearInterval(interval);
  }, [timer]);

  const formatTime = () => {
    const minutes = Math.floor(timer / 60);
    const seconds = timer % 60;
    return `${minutes}:${seconds < 10 ? `0${seconds}` : seconds}`;
  };

  const handleChange = (element, index) => {
    if (isNaN(element.value)) return false;

    const newOtp = [...otp];
    newOtp[index] = element.value.substring(element.value.length - 1);
    setOtp(newOtp);

    if (element.value && index < 5) inputsRef.current[index + 1].focus();
  };

  const handleKeyDown = (e, index) => {
    if (e.key === "Backspace" && !otp[index] && index > 0) {
      inputsRef.current[index - 1].focus();
    }
  };

  const handleResend = () => {
    if (canResend) {
      setTimer(300);
      setCanResend(false);
      setOtp(new Array(6).fill(""));
    }
  };

  useEffect(() => {
    if (state.status === 0) {
      return;
    }

    switch (state.status) {
      case 200: {
        success(translations.verification.otp.verified);

        router.push("/");
        break;
      }
      case 400: {
        error(translations.verification.errors.otpCodeInvalid);
        break;
      }
      case 401: {
        error(translations.verification.errors.incorrectVerificationToken);

        setTimeout(() => {
          window.location.reload();
        }, 500);
        break;
      }
      case 410: {
        error(translations.verification.errors.otpCodeExpired);
        break;
      }
      case 422: {
        break;
      }
      default: {
        error(translations.verification.errors.verificationFailed);
        break;
      }
    }
  }, [state.timestamp]);

  return (
    <form action={formAction}>
      <p>{translations.verification.otp.title}</p>
      <input type="hidden" name="email" value={email} />
      <input type="hidden" name="password" value={password} />
      <input type="hidden" name="otpCode" value={otp.join("")} />
      <div className={styles.otpInputs}>
        {otp.map((data, index) => (
          <input
            key={index}
            type="text"
            maxLength="1"
            value={data}
            ref={(el) => (inputsRef.current[index] = el)}
            onChange={(e) => handleChange(e.target, index)}
            onKeyDown={(e) => handleKeyDown(e, index)}
            className={styles.input}
          />
        ))}
      </div>
      {state?.inputs?.errors?.otpCode && (
        <Error error={state?.inputs?.errors?.otpCode} />
      )}
      <div className={styles.timerSection}>
        {timer > 0 ? (
          <p className={styles.notReceived}>
            {translations.verification.otp.notReceived}{" "}
            <strong>{formatTime()}</strong>
          </p>
        ) : (
          <p className={styles.resend} onClick={handleResend}>
            {translations.verification.otp.resend}
          </p>
        )}
      </div>
      <SubmitButton
        text={translations.verification.button.verify}
        loadingText={translations.verification.otp.verifying}
      />
    </form>
  );
}

export default Otp;
